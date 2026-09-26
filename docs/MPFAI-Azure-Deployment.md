# Deploying MPFAI to an Azure subscription

**Status: infrastructure scaffold only.** This guide shows how to validate and deploy the Azure resources currently defined in `infra/main.bicep`, and what must change before hosting the application. The current deployment is **not a production MPFAI deployment**.

The checked-in Bicep provisions a Storage account and three private containers, a Key Vault, Log Analytics, Application Insights, an App Service Plan, and a Service Bus namespace with two queues. It does **not** provision a web app, API app, Function App, Azure SQL, AI services, managed identities, role assignments, private endpoints, DNS, alerts, or network restrictions. The API uses in-memory stores, requires a client-supplied `organizationId`, reports external services as unconfigured, and deliberately exits outside the ASP.NET Core `Development` environment. Do not publish that Development API to Azure or expose it to the Internet.

## 1. Decide whether to proceed

- For a **resource-provisioning evaluation**, follow Sections 2–6. This creates only the infrastructure scaffold; it does not deploy or host the MPFAI web or API applications.
- For a **production or partner-data deployment**, stop here until the production gates in Section 7 are implemented, independently reviewed, and accepted. Do not work around the startup guard by setting `ASPNETCORE_ENVIRONMENT=Development` in Azure.

Provisioning Azure resources may incur charges. Review the proposed resources, SKU, region, subscription policy, and expected costs with your Azure subscription owner before deployment. The current template does not configure a budget or cost alert.

## 2. Prerequisites

Have the following ready:

- An Azure subscription approved for this non-production evaluation and permission to create a resource group and deploy the listed resource types. Subscription or resource-group deployment roles must also allow Azure Resource Manager to register the required resource providers.
- PowerShell, Azure CLI (`az`), Git, and the repository checked out at the commit you intend to evaluate.
- A permitted Azure region for the subscription. Check regional availability and organizational policy for Storage, Key Vault, App Service, Service Bus, and Application Insights before selecting one.
- A globally unique, lowercase, alphanumeric 3–12-character prefix, such as `mpfai`. Keep environment names to `dev`, `test`, `staging`, or `prod`; use `dev` for this scaffold evaluation.
- Approval to create public-network-enabled Azure resource endpoints. The current template sets public network access to `Enabled` for Storage, Key Vault, and Service Bus. Storage shared-key access and public blob access are disabled, but the endpoints are not private-networked.

## 3. Sign in and select the subscription

Open PowerShell at the repository root:

```powershell
az version
az login
az account list --output table
```

Select the **approved non-production subscription ID** (not its display name), then verify the selection:

```powershell
$subscriptionId = "<approved-subscription-id>"
az account set --subscription $subscriptionId
az account show --query "{name:name,id:id,tenantId:tenantId,state:state}" --output table
```

If the displayed subscription ID or tenant is not the intended target, stop and select the correct subscription before creating anything.

## 4. Validate the template and register resource providers

Compile the checked-in Bicep template:

```powershell
az bicep version
az bicep build --file infra\main.bicep --outfile "$env:TEMP\mpfai-main.json"
```

If Azure CLI reports that Bicep is missing, install it and retry:

```powershell
az bicep install
az bicep build --file infra\main.bicep --outfile "$env:TEMP\mpfai-main.json"
```

Register the resource providers used by the scaffold. Registration is subscription-scoped; follow your organization's approval process:

```powershell
$providers = @(
  "Microsoft.Storage",
  "Microsoft.KeyVault",
  "Microsoft.OperationalInsights",
  "Microsoft.Insights",
  "Microsoft.Web",
  "Microsoft.ServiceBus"
)
foreach ($provider in $providers) {
  az provider register --namespace $provider
  if ($LASTEXITCODE -ne 0) { throw "Provider registration failed: $provider" }
}
```

Provider registration can take time. Before continuing, verify each provider reports `Registered`:

```powershell
foreach ($provider in $providers) {
  $state = az provider show --namespace $provider --query registrationState --output tsv
  if ($LASTEXITCODE -ne 0 -or $state -ne "Registered") {
    throw "Provider is not registered: $provider ($state)"
  }
}
```

## 5. Create a dedicated resource group

Use a new resource group for this evaluation, in the selected region. Do not target a production or shared resource group:

```powershell
$location = "<approved-region-name>" # For example: eastus; verify regional approval first.
$resourceGroup = "rg-mpfai-dev"

az group create `
  --name $resourceGroup `
  --location $location `
  --tags application=MPFAI environment=dev purpose=scaffold-evaluation
if ($LASTEXITCODE -ne 0) { throw "Resource group creation failed." }
```

Confirm the resource group is in the intended subscription and region:

```powershell
az group show --name $resourceGroup --query "{name:name,location:location,id:id}" --output table
```

## 6. Preview, deploy, and verify the infrastructure scaffold

First review the deployment with Azure's `what-if` operation. Use the same parameters for preview and deployment:

```powershell
$namePrefix = "mpfai" # 3–12 lowercase alphanumeric characters
$environmentName = "dev"

az deployment group validate `
  --resource-group $resourceGroup `
  --template-file infra\main.bicep `
  --parameters namePrefix=$namePrefix environmentName=$environmentName location=$location
if ($LASTEXITCODE -ne 0) { throw "Bicep validation failed." }

az deployment group what-if `
  --resource-group $resourceGroup `
  --template-file infra\main.bicep `
  --parameters namePrefix=$namePrefix environmentName=$environmentName location=$location
if ($LASTEXITCODE -ne 0) { throw "Deployment preview failed." }
```

Check the preview against the Bicep inventory at the top of this guide. Stop if the subscription, region, resource names, network exposure, resource types, or changes are unexpected. The scaffold deliberately has public endpoints and does not attach app or workload identities. `prod` selects a larger App Service Plan SKU; that does not make a production application or security configuration.

Only after reviewing and approving the preview, deploy:

```powershell
$deploymentName = "mpfai-scaffold-$(Get-Date -Format 'yyyyMMddHHmmss')"

az deployment group create `
  --name $deploymentName `
  --resource-group $resourceGroup `
  --template-file infra\main.bicep `
  --parameters namePrefix=$namePrefix environmentName=$environmentName location=$location `
  --mode Incremental
if ($LASTEXITCODE -ne 0) { throw "Infrastructure deployment failed; inspect the deployment operations before retrying." }
```

Verify the deployment completed and inspect its outputs:

```powershell
az deployment group show `
  --name $deploymentName `
  --resource-group $resourceGroup `
  --query "{state:properties.provisioningState,outputs:properties.outputs}" `
  --output json

az resource list --resource-group $resourceGroup --query "[].{name:name,type:type,location:location}" --output table
```

The expected resource types are a Storage account (with the `upload-quarantine`, `guideline-originals`, and `evidence-originals` containers), Key Vault, Log Analytics workspace, Application Insights component, App Service Plan, Service Bus namespace, and the `sow-analyze` and `report-import` queues. Confirm actual names, provisioning state, subscription, region, and resource tags in the deployment output and Azure portal. An App Service Plan is only compute capacity; it does not create an App Service site.

## 7. Production release gates — required before deploying application code

The repository does not currently have a supported production app deployment. Complete and verify all applicable items before designing or running an application rollout:

1. **Identity and authorization:** Integrate Microsoft Entra authentication and server-side organization membership/role authorization. Derive organization context from the authenticated principal, remove trust in query-string `organizationId`, add authorization and cross-organization API tests, and keep the non-Development startup protection until those controls are implemented.
2. **Durable data and documents:** Implement and test production repositories and schema migrations backed by Azure SQL; immutable document/evidence storage backed by Blob Storage; and durable, append-only audit history. The current in-memory data is lost when the process stops.
3. **Secure document processing:** Add quarantine and upload limits, real malware scanning, supported PDF/DOCX/XLSX validation and extraction, access-controlled downloads, retention controls, and failure/retry handling. Never label an unperformed scan successful.
4. **Async work and integrations:** Implement idempotent workers, queues/outbox/dead-letter handling, and real dependency health reporting. Until a supported connector is configured and validated, Partner Center/PAL and Azure AI must remain explicitly unavailable; never represent a local status record as external verification.
5. **Azure security and operations:** Add workload managed identities and least-privilege role assignments; decide and implement private endpoints, DNS, ingress restrictions, TLS, Key Vault access, backups/recovery, alerts, monitoring, and incident procedures. Review the current public-network-enabled endpoints with security and network owners; they are not a production private network design.
6. **Deployable application resources:** Extend Bicep to provision and configure actual web/API/worker hosting, environment settings, identity, network and data resources, and health probes. There is currently an App Service Plan but no web app or worker app. Choose a supported hosting and runtime deployment strategy.
7. **Release pipeline:** Add reviewed environment parameter files without secrets, least-privilege federated GitHub deployment identity, artifact build/publish steps, staging approval, smoke tests, and a rollback plan. The current GitHub Actions workflow runs validation only; it does not authenticate to Azure or deploy.
8. **Acceptance and approval:** Complete the relevant security, privacy, accessibility, dependency, load, recovery, and data-governance reviews, test in a separate non-production environment using synthetic or approved redacted data, and obtain documented service-owner approval before production.

Track these gates against [the MVP requirement gaps](MPFAI-MVP-Gaps.md) and the [product](MPFAI-Product-Specification.md) and [technical](MPFAI-Technical-Specification.md) specifications. Do not promote the current process-memory local adapter or its query-string tenant identifier to a public Azure environment.

## 8. Local verification remains local

The supported application run and test path is local development:

```powershell
dotnet test MPFAI.sln --configuration Release
Push-Location apps\web
npm ci
npm run lint
npm audit --audit-level=high
npm run build
npx tsc --noEmit
Pop-Location
```

These commands validate the code; they do **not** deploy an application to Azure. A local `Development` API and its sample records are not a safe Azure deployment target.

## 9. Remove the evaluation resources

If the scaffold is no longer needed, verify the selected subscription and resource-group name before removal. Deleting the group permanently deletes its resources and data:

```powershell
az account show --query "{name:name,id:id}" --output table
az group show --name $resourceGroup --query "{name:name,location:location,id:id}" --output table
```

After independently confirming that the group is dedicated to this disposable evaluation and contains nothing to retain:

```powershell
az group delete --name $resourceGroup --yes --no-wait
```

Check the resource group until Azure reports it deleted. Provider registrations are subscription-level and are not undone by deleting the resource group.
