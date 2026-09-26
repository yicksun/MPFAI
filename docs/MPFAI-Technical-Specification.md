# MPFAI Technical Specification

**Product:** Microsoft Partner Funding and Attribution Intelligence (MPFAI)  
**Document type:** Technical Specification  
**Status:** Draft for engineering review  
**Version:** 0.1  
**Date:** 26 September 2026  
**Source:** MPFAI Product Specification v0.1

## 1. Purpose

This document defines the recommended technical foundation for implementing MPFAI. It translates the product specification into an application architecture, component model, data strategy, processing workflows, security model, engineering standards, and delivery plan.

The design prioritizes:

- explainable SOW-to-funding analysis;
- deterministic and reproducible funding calculations;
- secure processing of confidential customer documents;
- strong organization isolation and auditability;
- reliable asynchronous workflows;
- human approval of program rules, recommendations, and calculated amounts; and
- an Azure-native architecture that remains manageable for an MVP team.

## 2. Architecture decision summary

| Area | Decision |
|---|---|
| Application style | Modular monolith for the MVP |
| Frontend | Next.js with React and TypeScript |
| Component library | Fluent UI React |
| Backend | ASP.NET Core on .NET 10 LTS |
| Background processing | Azure Functions, .NET isolated worker |
| Primary database | Azure SQL Database |
| Data access | Entity Framework Core |
| Document storage | Azure Blob Storage |
| Messaging | Azure Service Bus |
| Document extraction | Azure AI Document Intelligence |
| AI analysis | Azure AI Foundry model deployment |
| Guideline retrieval | Azure AI Search |
| Funding calculation | Versioned C# rules and calculation engine |
| Identity | Microsoft Entra ID with MSAL |
| Secrets | Azure Key Vault and managed identities |
| Web hosting | Azure App Service |
| Observability | OpenTelemetry, Application Insights, Azure Monitor |
| Infrastructure as code | Bicep |
| CI/CD | GitHub Actions |
| Backend testing | xUnit |
| Browser testing | Playwright |

## 3. Architectural approach

### 3.1 Modular monolith

The MVP should be delivered as one deployable ASP.NET Core API containing independently organized business modules. This avoids the operational and transactional complexity of microservices while preserving clear boundaries that can be extracted later if needed.

Recommended modules:

1. Identity and Organizations
2. Customers and Engagements
3. Document Library
4. SOW Processing
5. Program Guidelines
6. Funding Analysis
7. Funding Calculation
8. Delivery Workflow
9. Attribution
10. Evidence and Claims
11. Notifications
12. Reporting
13. Audit

Each module owns its application services, domain rules, database mappings, API endpoints, and tests. Modules may share infrastructure abstractions but must not directly manipulate another module's internal entities.

### 3.2 Deployment units

The MVP has three primary deployable units:

- **Web application:** Next.js user interface.
- **Application API:** ASP.NET Core API containing the modular business application.
- **Worker application:** Azure Functions for document processing, AI analysis, imports, notifications, and scheduled checks.

The web application and API can initially share one App Service Plan but should be deployed as separate applications. Background work must remain separate from web request processing.

### 3.3 Core design principles

- The database is authoritative for workflow and business state.
- Blob Storage is authoritative for immutable source documents and generated files.
- AI Search is a retrieval index, not a system of record.
- AI-generated content is advisory until reviewed.
- Funding arithmetic is performed by deterministic application code.
- Every derived result records all input and rule versions.
- Long-running work is asynchronous, idempotent, observable, and retryable.
- A connector failure produces an explicit failed, stale, or unknown state.
- Organization authorization is enforced in the API and data-access layer, never only in the UI.

## 4. Technology stack

### 4.1 Frontend

| Technology | Use |
|---|---|
| Next.js | Application framework, routing, build, and server-side capabilities |
| React | User interface components |
| TypeScript | Type-safe frontend development |
| Fluent UI React | Accessible Microsoft-aligned components |
| TanStack Query | Server-state loading, caching, and mutation |
| React Hook Form | Form state and validation integration |
| Zod | Client-side schema validation |
| MSAL React | Microsoft Entra ID authentication |
| Playwright | End-to-end and accessibility-oriented browser tests |
| Vitest and Testing Library | Component and frontend unit tests |

The frontend must consume the API through generated TypeScript contracts produced from the API's OpenAPI document. Client-side validation improves usability but never replaces server-side validation.

### 4.2 Backend

| Technology | Use |
|---|---|
| .NET 10 LTS | Runtime |
| ASP.NET Core | REST API and application hosting |
| C# | Domain, workflow, and calculation implementation |
| Entity Framework Core | Relational persistence |
| FluentValidation | Request and command validation |
| OpenAPI | API contract and client generation |
| OpenTelemetry | Tracing, metrics, and log correlation |
| xUnit | Unit and integration tests |
| Testcontainers for .NET | Local integration tests against SQL-compatible dependencies where suitable |

Use ASP.NET Core problem details for consistent errors. Use strongly typed identifiers or value objects for important domain IDs, money, currencies, date ranges, rule versions, and organization context.

### 4.3 Azure services

| Service | Responsibility |
|---|---|
| Azure App Service | Host web and API applications |
| Azure Functions | Run asynchronous and scheduled workers |
| Azure SQL Database | Store operational, workflow, configuration, and audit data |
| Azure Blob Storage | Store source documents, evidence, exports, and generated packs |
| Azure Service Bus | Queue reliable processing commands and integration events |
| Azure AI Document Intelligence | Extract text, layout, tables, and page locations |
| Azure AI Search | Index approved guideline content for retrieval |
| Azure AI Foundry | Host approved models for structured extraction and analysis |
| Microsoft Entra ID | Authenticate workforce users |
| Azure Key Vault | Store secrets and certificates that cannot use managed identity |
| Application Insights | Application performance monitoring and distributed traces |
| Azure Monitor | Metrics, alerts, dashboards, and operational logs |
| Microsoft Defender for Storage | Malware scanning for Blob Storage uploads |
| Azure Front Door and WAF | Optional production edge protection and routing |

## 5. Solution structure

Recommended repository layout:

```text
/
|-- apps/
|   |-- web/                         # Next.js application
|   |-- api/                         # ASP.NET Core API host
|   `-- workers/                     # Azure Functions host
|-- src/
|   |-- BuildingBlocks/
|   |   |-- Application/
|   |   |-- Domain/
|   |   |-- Infrastructure/
|   |   `-- Observability/
|   |-- Identity/
|   |-- Customers/
|   |-- Engagements/
|   |-- Documents/
|   |-- Programs/
|   |-- Analysis/
|   |-- Calculations/
|   |-- Delivery/
|   |-- Attribution/
|   |-- Claims/
|   |-- Notifications/
|   |-- Reporting/
|   `-- Audit/
|-- tests/
|   |-- Unit/
|   |-- Integration/
|   |-- Architecture/
|   |-- Contract/
|   `-- EndToEnd/
|-- infra/
|   |-- modules/
|   |-- environments/
|   `-- main.bicep
|-- docs/
|   |-- decisions/
|   |-- runbooks/
|   `-- api/
|-- Directory.Build.props
|-- Directory.Packages.props
`-- MPFAI.sln
```

The repository should use centralized .NET package versions, strict nullable reference types, warnings as errors in CI, consistent analyzers, and locked JavaScript dependencies.

## 6. Domain and module boundaries

### 6.1 Identity and Organizations

Responsibilities:

- map Entra users and groups to MPFAI organizations;
- resolve organization context for every request;
- maintain role assignments;
- enforce organization and record-level access;
- deactivate users while retaining historical references.

This module does not store passwords or implement an independent identity provider.

### 6.2 Customers and Engagements

Responsibilities:

- customer profiles, domains, contacts, and tenant identifiers;
- engagement creation and lifecycle;
- engagement ownership;
- Azure scopes associated with an engagement;
- independent sales, funding, delivery, attribution, claim, and finance states.

The engagement is the aggregate reference shared across downstream modules.

### 6.3 Document Library

Responsibilities:

- secure uploads;
- malware-scan status;
- immutable Blob Storage object references;
- metadata, classification, versioning, checksums, and retention;
- access-controlled downloads;
- derivative files and generated outputs.

Documents include SOWs, Microsoft funding guidelines, evidence, report imports, and generated claim packs. Each type has a separate retention and authorization policy.

### 6.4 Program Guidelines

Responsibilities:

- administrator uploads of Microsoft Partner funding guideline files;
- document extraction and classification;
- rule and calculation-parameter proposals;
- program-manager review and approval;
- effective dates, fiscal year, geography, program, and source tracking;
- conflict, duplicate, expiration, and supersession detection;
- immutable approved guideline versions.

Only approved and effective guideline versions are eligible for production analysis.

### 6.5 Funding Analysis

Responsibilities:

- analyze a confirmed SOW extraction;
- identify applicable approved guideline versions;
- retrieve relevant guideline passages;
- generate structured candidate findings;
- apply deterministic eligibility checks;
- expose citations, missing inputs, assumptions, and confidence;
- manage analyst review and promotion to a funding opportunity.

### 6.6 Funding Calculation

Responsibilities:

- execute approved versioned formulas;
- validate required inputs;
- apply rates, thresholds, caps, exclusions, and currency;
- produce a complete calculation trace;
- reject ambiguous or incomplete calculation configurations;
- support reproducible recalculation.

This module must not call an LLM to perform authoritative arithmetic.

### 6.7 Delivery Workflow

Responsibilities:

- tasks, dependencies, milestones, readiness gates, and exceptions;
- scope changes and impact reviews;
- customer acceptance and delivery completion;
- ownership, reminders, and escalation.

### 6.8 Attribution

Responsibilities:

- customer tenant, Azure scope, delivery identity, association type, and intended Partner ID;
- guided PAL setup;
- evidence-based verification;
- report imports and reconciliation;
- stale, missing, mismatched, and unknown states;
- closeout and access-removal tasks.

### 6.9 Evidence and Claims

Responsibilities:

- evidence requirements derived from approved program versions;
- evidence uploads, mappings, review, and replacement;
- claim readiness;
- external claim identifiers and lifecycle;
- action-required, resubmission, dispute, approval, rejection, and expiry;
- payment reconciliation.

### 6.10 Audit

Responsibilities:

- append-only material business events;
- actor, organization, timestamp, action, record, correlation ID, and before/after values;
- decision provenance;
- document, rule, formula, model, and prompt versions.

Audit writes must participate in the same database transaction as the material state change where possible.

## 7. Data architecture

### 7.1 Azure SQL

Use a single Azure SQL database for the MVP with schema-per-module organization:

```text
identity.*
customers.*
engagements.*
documents.*
programs.*
analysis.*
calculations.*
delivery.*
attribution.*
claims.*
notifications.*
reporting.*
audit.*
```

Important conventions:

- Every organization-owned table includes `organization_id`.
- Every mutable entity includes a concurrency token.
- Store timestamps in UTC using `datetimeoffset`.
- Use ISO 4217 currency codes and decimal types suitable for financial calculations.
- Avoid floating-point types for rates or monetary values.
- Use soft deletion only where the product requires archive and restore.
- Approved rules, formulas, assessments, and evidence versions are immutable.
- Use temporal tables selectively for high-value mutable records; continue to maintain explicit audit events.

### 7.2 Principal tables

Representative tables:

- `identity.organizations`
- `identity.users`
- `identity.role_assignments`
- `customers.customers`
- `customers.contacts`
- `customers.customer_tenants`
- `engagements.engagements`
- `engagements.engagement_states`
- `engagements.azure_scopes`
- `documents.documents`
- `documents.document_versions`
- `documents.processing_jobs`
- `programs.programs`
- `programs.guideline_versions`
- `programs.guideline_sources`
- `programs.rules`
- `programs.formulas`
- `programs.formula_parameters`
- `analysis.assessments`
- `analysis.findings`
- `analysis.citations`
- `analysis.reviewer_decisions`
- `calculations.calculation_runs`
- `calculations.calculation_inputs`
- `calculations.calculation_steps`
- `calculations.calculation_outputs`
- `delivery.tasks`
- `delivery.task_dependencies`
- `delivery.milestones`
- `delivery.scope_changes`
- `attribution.records`
- `attribution.verifications`
- `attribution.report_imports`
- `claims.evidence_requirements`
- `claims.evidence_items`
- `claims.evidence_mappings`
- `claims.claims`
- `claims.claim_events`
- `claims.payments`
- `audit.events`

### 7.3 Blob Storage

Recommended containers:

- `upload-quarantine`
- `sow-originals`
- `guideline-originals`
- `evidence-originals`
- `report-imports`
- `generated-documents`

Blob names should use non-guessable IDs rather than customer names. Business filenames remain metadata. Enable versioning, soft delete, encryption, lifecycle policies, and malware scanning. Downloads should flow through an authorized API or short-lived user-delegation SAS after authorization.

### 7.4 Azure AI Search

Create separate indexes for approved guideline content and, if required later, organization-scoped engagement documents.

Guideline chunks should include:

- guideline version ID;
- program ID;
- title and document type;
- fiscal year;
- effective date range;
- geography;
- solution area;
- page, section, table, row, column, or cell reference;
- source text;
- approval state;
- checksum;
- embedding vector; and
- searchable keywords.

Only approved guideline versions are promoted to the active index or filtered as active. Search results must always be constrained by guideline state and effective context.

## 8. API design

### 8.1 Style

Use versioned JSON REST APIs under `/api/v1`. Generate and publish an OpenAPI document. Use resource-oriented endpoints for queries and commands for explicit state transitions.

Examples:

```text
POST   /api/v1/customers
GET    /api/v1/customers/{customerId}
POST   /api/v1/engagements
GET    /api/v1/engagements/{engagementId}
POST   /api/v1/engagements/{engagementId}/sows
POST   /api/v1/sow-versions/{versionId}/confirm-extraction
POST   /api/v1/sow-versions/{versionId}/analyses
GET    /api/v1/analyses/{analysisId}
POST   /api/v1/analyses/{analysisId}/review-decisions
POST   /api/v1/guidelines
POST   /api/v1/guideline-versions/{versionId}/approve
POST   /api/v1/funding-opportunities/{id}/calculate
POST   /api/v1/engagements/{id}/scope-changes
POST   /api/v1/attribution-records/{id}/verifications
POST   /api/v1/claims/{id}/events
GET    /api/v1/reports/funding-pipeline
```

### 8.2 API rules

- Validate organization ownership before returning whether a resource exists.
- Require idempotency keys for commands that may be retried.
- Use ETags or concurrency tokens on mutable records.
- Return RFC 9457 problem details with a correlation ID.
- Use cursor pagination for large lists.
- Apply explicit request-size limits.
- Never return Blob Storage credentials or permanent URLs.
- Keep state transitions explicit; do not allow arbitrary status patching.
- Record material mutations in the audit log.

### 8.3 Authorization

Use policy-based ASP.NET Core authorization:

```text
OrganizationMember
ProgramLibraryRead
ProgramLibraryManage
ProgramVersionApprove
AssessmentReview
EngagementManage
AttributionVerify
EvidenceReview
ClaimManage
FinanceManage
AuditRead
```

Policies combine the Entra identity, MPFAI role assignment, organization context, and resource ownership. Administrative roles do not bypass organization boundaries.

## 9. Asynchronous processing

### 9.1 Service Bus queues and topics

Recommended queues:

- `document-scan`
- `document-extract`
- `guideline-index`
- `sow-analyze`
- `report-import`
- `notification-send`
- `document-generate`

Use a topic for business events consumed by multiple handlers:

- `engagement-events`

Representative events:

- `SowVersionConfirmed`
- `ProgramVersionApproved`
- `FundingOpportunityApproved`
- `EngagementScopeChanged`
- `AttributionBecameStale`
- `EvidenceRequirementCreated`
- `ClaimActionRequired`
- `PaymentRecorded`

### 9.2 Reliability requirements

- Messages carry message ID, correlation ID, organization ID, job ID, schema version, and creation time.
- Consumers implement idempotency using a processed-message record or unique operation key.
- Use exponential retry for transient failures.
- Move poison messages to a dead-letter queue with diagnostics.
- Never acknowledge a message before durable state is committed.
- Use an outbox pattern for business events emitted from database transactions.
- Scheduled recovery detects abandoned jobs and safely retries them.
- User-visible job records show queued, running, succeeded, failed, cancelled, and retrying states.

## 10. Guideline ingestion and approval

### 10.1 Ingestion workflow

1. An authorized administrator uploads a PDF, DOCX, or supported XLSX file.
2. The API stores the file in the quarantine container and creates a pending document version.
3. Defender for Storage scans the object.
4. A worker rejects malware or releases the file for extraction.
5. Document Intelligence extracts text, layout, tables, and source locations.
6. A model proposes document classification, metadata, rules, rates, caps, thresholds, exclusions, dates, and calculation examples as structured JSON.
7. Validation checks schema, data types, currency, date ranges, references, and formula safety.
8. Conflict detection compares the proposal with active guideline versions.
9. A program manager reviews every material rule and calculation parameter against the source.
10. Approval creates an immutable guideline version, deterministic formula definition, and active search index entries.
11. Superseded versions remain available for historical reproduction.

### 10.2 Formula representation

Do not execute arbitrary code, spreadsheet macros, or model-generated expressions. Use a constrained formula schema interpreted by C#.

Supported operations should begin with:

- fixed amount;
- multiplication by approved rate;
- percentage;
- minimum and maximum;
- tiered thresholds;
- conditional branch based on approved enumerated facts;
- quantity bands;
- currency rounding; and
- program cap.

Example conceptual definition:

```json
{
  "formulaType": "percentage_with_cap",
  "baseInput": "eligibleEngagementValue",
  "rate": "0.15",
  "maximumAmount": "50000.00",
  "currency": "USD",
  "rounding": "away_from_zero_2dp",
  "sourceCitationId": "citation-id"
}
```

Formula definitions are schema-validated, source-cited, versioned, reviewed, and approved. New operation types require code changes and automated tests.

### 10.3 Conflict handling

Block approval or calculation when:

- two active guideline versions claim overlapping applicability with different values;
- a required rate, cap, threshold, currency, or date is missing;
- the source citation cannot be resolved;
- the file is expired, unapproved, corrupt, or superseded;
- extracted text and manually entered values disagree without a reviewer resolution; or
- the formula uses an unsupported operation.

## 11. SOW analysis and funding calculation

### 11.1 Analysis workflow

1. User uploads and confirms a SOW version.
2. The application derives the engagement context: customer, geography, workloads, dates, partner location, and requested analysis scope.
3. The program module selects applicable approved guideline versions.
4. AI Search retrieves relevant guideline chunks using metadata filters plus hybrid search.
5. The model returns a strict structured result containing candidate rules, cited passages, supporting SOW passages, missing facts, and confidence.
6. The application validates every citation and rejects unsupported model output.
7. Deterministic eligibility rules classify the candidate.
8. If calculable, the C# calculation engine validates inputs and executes the approved formula.
9. Results and the complete calculation trace are stored.
10. A funding analyst reviews and approves, edits, rejects, or requests information.

### 11.2 Structured model output

The model response must conform to a versioned schema with:

- candidate program and guideline version IDs;
- SOW source references;
- guideline citation IDs;
- extracted factual inputs;
- missing inputs;
- proposed eligibility conditions;
- explanation;
- confidence; and
- warnings.

The model must not supply an authoritative monetary result. Unknown IDs, unresolved citations, invalid values, and schema violations fail the analysis step rather than being silently ignored.

### 11.3 Calculation result

Each calculation run stores:

- calculation run ID;
- formula and guideline version;
- SOW and assessment version;
- input names, values, units, source, and verification state;
- ordered calculation steps;
- intermediate values;
- rate, thresholds, cap, currency, and rounding rule;
- exclusions and assumptions;
- final indicative amount;
- execution timestamp;
- engine version; and
- reviewer decision.

Recalculation creates a new immutable run and never overwrites the prior result.

## 12. Authentication and security

### 12.1 Authentication flow

- Use Entra ID authorization code flow with PKCE.
- The browser obtains an access token for the MPFAI API through MSAL.
- The API validates issuer, audience, tenant, signature, lifetime, and required scopes.
- The API maps the Entra subject to an active MPFAI organization user.
- Background workers authenticate to Azure services through managed identity.

### 12.2 Organization isolation

Use defense in depth:

- organization ID resolved from authenticated membership, not client input alone;
- organization query filters in EF Core;
- explicit authorization checks for resource commands;
- organization ID on queues and storage metadata;
- per-organization search filters;
- integration and end-to-end tests for cross-organization access;
- no shared cache entry without organization in its key.

Azure SQL row-level security may be evaluated as an additional safeguard, but it does not replace application authorization.

### 12.3 Upload security

- Upload to quarantine using short-lived scoped access.
- Validate declared and detected file type.
- Enforce file and decompression limits.
- Reject encrypted files unless a separately approved secure flow exists.
- Scan before extraction or download.
- Do not execute macros or embedded content.
- Sanitize generated filenames and response headers.
- Apply retention and deletion only through auditable workflows.

### 12.4 AI security

- Treat SOW and guideline text as untrusted content.
- Prevent retrieved documents from changing model instructions or tool behavior.
- Send only required content to approved Azure model deployments.
- Restrict retrieval by organization, approval state, program, geography, and effective date.
- Log model and prompt versions but not confidential prompt content in standard telemetry.
- Validate structured output and citation ownership before use.
- Apply content filtering and request limits.

## 13. Observability

### 13.1 Telemetry standards

Every request and background job includes:

- trace ID;
- correlation ID;
- organization ID in protected structured form;
- user or workload actor ID;
- operation name;
- module;
- outcome;
- duration; and
- error code.

Do not log SOW text, guideline contents, evidence contents, tokens, secrets, or customer contact details.

### 13.2 Key metrics

- API latency and error rate;
- authentication and authorization failures;
- upload and malware-scan outcomes;
- Service Bus queue depth, age, retries, and dead letters;
- extraction latency and confidence;
- search latency and zero-result rate;
- AI latency, token usage, schema-failure rate, and citation-failure rate;
- calculation success, blocked, and invalid-configuration rates;
- stale guideline and attribution counts;
- notification failures;
- deadline job health; and
- per-environment Azure cost.

### 13.3 Alerts

Create actionable alerts for:

- sustained API failure or latency;
- dead-letter messages;
- failed or stalled document jobs;
- malware detections;
- repeated cross-organization authorization denials;
- unavailable AI, search, SQL, storage, or messaging dependencies;
- stale report imports;
- calculation configuration errors;
- failed deadline notifications;
- Key Vault or certificate expiry; and
- unusual cost growth.

## 14. Testing strategy

### 14.1 Test layers

| Layer | Coverage |
|---|---|
| Unit | Domain rules, formula operations, date logic, state transitions, and validation |
| Property-based | Monetary rounding, caps, thresholds, tiers, and invariant checks |
| Integration | API, Azure SQL, Blob abstractions, Service Bus handlers, and search adapters |
| Contract | OpenAPI compatibility and external adapter contracts |
| Architecture | Module dependency boundaries and organization-filter requirements |
| AI evaluation | Extraction, retrieval, citation accuracy, unsupported claims, and adversarial documents |
| End-to-end | Principal user journeys in Playwright |
| Security | Authorization matrix, cross-tenant isolation, upload abuse, and OWASP controls |
| Performance | API, dashboard, upload, queues, document volume, and calculation throughput |
| Recovery | Retry, dead-letter, backup restore, worker restart, and idempotency |

### 14.2 Funding calculation tests

Every approved formula version needs:

- examples transcribed from the source guideline;
- lower, exact, and upper threshold cases;
- cap and no-cap cases;
- zero, negative, missing, and invalid inputs;
- currency and rounding cases;
- conflicting or expired guideline cases;
- deterministic repeatability; and
- backward reproduction of historical runs.

No formula can become active until all required tests pass and a program manager approves the source mapping.

### 14.3 AI evaluation

Maintain a versioned evaluation set of redacted or synthetic:

- SOWs;
- guideline documents;
- expected extracted fields;
- applicable and non-applicable program cases;
- calculation inputs;
- missing-information cases;
- conflicting rules;
- prompt-injection attempts; and
- expected citations.

Deploy model or prompt changes only after meeting agreed quality thresholds and completing human review.

## 15. Infrastructure and environments

### 15.1 Environments

Use separate Azure resources for:

- development;
- test;
- staging; and
- production.

Production must not share databases, storage accounts, search indexes, model endpoints, queues, secrets, or identities with non-production.

### 15.2 Bicep

The Bicep deployment should provision:

- resource group-level resources;
- App Service Plan and applications;
- Function App;
- Azure SQL server and database;
- Storage Account and containers;
- Service Bus namespace, queues, topic, subscriptions, and dead-letter monitoring;
- Document Intelligence;
- Azure AI Search;
- Azure AI Foundry resources or approved model endpoint references;
- Key Vault;
- managed identities and RBAC;
- Application Insights and Log Analytics;
- private endpoints, DNS, and network controls where selected;
- alert rules, action groups, and dashboards; and
- resource locks and backup configuration for production.

Use parameter files per environment. Secrets must not appear in parameter files or deployment output.

### 15.3 Local development

Recommended local dependencies:

- .NET SDK 10;
- current supported Node.js LTS;
- SQL Server container or approved local SQL environment;
- Azurite for storage behavior where sufficient;
- Service Bus and AI services represented by test adapters for normal unit work;
- optional Azure development resources for integration testing that emulators cannot cover.

Use developer identities and `DefaultAzureCredential`; do not distribute shared connection strings.

## 16. CI/CD

### 16.1 Pull request pipeline

1. Restore dependencies from lock files.
2. Check formatting and analyzers.
3. Build frontend and backend with warnings treated as errors.
4. Run unit, architecture, and component tests.
5. Run integration tests.
6. Generate OpenAPI and verify the frontend client is current.
7. Scan dependencies, source, secrets, containers if used, and Bicep.
8. Build immutable deployment artifacts.

### 16.2 Deployment pipeline

1. Deploy Bicep changes using what-if review.
2. Apply backward-compatible database migration.
3. Deploy workers, API, and web application.
4. Run smoke and contract tests.
5. Validate health endpoints and queue processing.
6. Promote through staging approval to production.
7. Use deployment slots for App Service and controlled swap.
8. Retain an application rollback path; database changes must follow expand-and-contract migration.

Production deployment requires an approval gate and records the artifact, database migration, infrastructure revision, and approver.

## 17. Performance and resilience

### 17.1 Performance approach

- Use asynchronous upload processing.
- Return compact list projections rather than full aggregates.
- Index organization, status, owner, due date, engagement, customer, and external claim fields.
- Precompute selected dashboard summaries where query cost requires it.
- Stream files rather than loading them fully into API memory.
- Cache stable approved guideline metadata with version-aware keys.
- Apply bounded concurrency to AI and document-processing workers.

### 17.2 Resilience

- Use SDK retry policies only for transient errors and with bounded attempts.
- Apply circuit breaking around unstable external dependencies.
- Distinguish retryable, user-correctable, configuration, and permanent failures.
- Persist job checkpoints where one operation has multiple expensive stages.
- Use dead-letter queues and operational replay tooling.
- Expose dependency health separately from process liveness.
- Do not mark a process successful until all authoritative writes complete.

## 18. Engineering conventions

- Use UTC internally and render dates in the user's selected time zone.
- Use `decimal`, never binary floating point, for rates and money.
- Store currency with every monetary amount.
- Use explicit state-transition methods rather than public status setters.
- Avoid generic repositories; use module-specific repositories or EF Core directly within module boundaries.
- Use database transactions for state, outbox, and audit consistency.
- Use cancellation tokens for all asynchronous I/O.
- Treat nullable warnings and analyzer warnings as build failures.
- Version APIs, messages, prompts, formula schemas, and stored AI outputs.
- Generate migrations in source control and review SQL impact.
- Do not hide errors behind empty results or successful fallback responses.

## 19. Technical acceptance criteria

The MVP technical foundation is ready for production when:

- the web, API, and worker applications deploy reproducibly through Bicep and GitHub Actions;
- Entra authentication and the complete authorization matrix pass automated tests;
- cross-organization resource access tests fail securely for every principal module;
- PDF, DOCX, and supported XLSX guideline uploads complete scan, extraction, review, approval, versioning, and indexing;
- a SOW can be processed asynchronously with visible status and traceable citations;
- an approved guideline produces a deterministic calculation with a reproducible step-by-step trace;
- missing, conflicting, expired, superseded, or unapproved rules block calculation;
- recalculation creates a new immutable run without changing historical output;
- queue retries, idempotency, dead-letter handling, and job recovery are verified;
- audit events capture material changes and cannot be altered through application APIs;
- backup restore meets the documented recovery objectives;
- load tests satisfy product performance targets at agreed MVP volume;
- Application Insights provides end-to-end traces across web, API, queue, worker, AI, search, SQL, and storage;
- security, accessibility, AI evaluation, and production-readiness reviews pass; and
- operations runbooks cover failed jobs, dead letters, dependency outages, security incidents, backup restore, and rollback.

## 20. Implementation sequence

### Iteration 1: Platform foundation

- Repository, solution boundaries, CI, Bicep, environments, identity, organization isolation, audit, and observability.

### Iteration 2: Customers, engagements, and documents

- Customer and engagement records, secure upload pipeline, storage, malware scanning, tasks, and activity history.

### Iteration 3: Guideline library

- Guideline ingestion, extraction, review, approval, versioning, conflict detection, and search indexing.

### Iteration 4: SOW analysis and calculation

- SOW extraction review, retrieval, structured AI analysis, citation validation, deterministic calculation, and analyst approval.

### Iteration 5: Delivery, evidence, and claims

- Readiness, milestones, scope impact, evidence requirements, claim readiness, claim events, and payments.

### Iteration 6: Attribution

- Attribution planning, guided PAL tasks, verification, report imports, reconciliation, stale-state detection, and closeout.

### Iteration 7: Hardening and pilot

- Dashboards, exports, performance, resilience, security, accessibility, AI evaluation, recovery testing, and pilot telemetry.

## 21. Open technical decisions

Resolve these before detailed implementation:

1. Single-tenant private deployment or multi-tenant SaaS at launch.
2. Next.js deployment as a Node App Service or a static/client application backed entirely by the API.
3. Azure Functions versus App Service WebJobs for scheduled and queue workers.
4. Required languages, document-size limits, and supported XLSX structures.
5. Selected Azure AI Foundry model and deployment region.
6. AI Search indexing strategy for multi-tenant deployment.
7. Exact Partner Center import and integration methods available to the organization.
8. Production network topology and private endpoint requirements.
9. Data residency, retention, legal hold, and customer-managed-key requirements.
10. Initial formula operation catalogue and approval workflow.
11. Reporting and export volume requirements.
12. CRM and finance integrations included in the MVP.

## 22. References

- `MPFAI-Product-Specification.md`
- [Microsoft: Link a partner ID to an account used to manage customers](https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/link-partner-id)
- [Microsoft: MCI engagements overview and eligibility](https://learn.microsoft.com/en-us/partner-center/incentives/mci-engagements)
- [Microsoft: Submit an MCI engagement claim](https://learn.microsoft.com/en-us/partner-center/incentives/mci-engagements-workshop)

Microsoft program rules, Azure service capabilities, SDK support, model availability, and Partner Center integration options must be revalidated during technical discovery and before production deployment.
