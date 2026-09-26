# MPFAI

Microsoft Partner Funding and Attribution Intelligence (MPFAI) is an explainable workspace for partner funding analysis, delivery, attribution, evidence, and claims.

## Local development

Prerequisites: .NET 10 SDK.

```powershell
dotnet restore MPFAI.sln
dotnet test MPFAI.sln
dotnet run --project src/MPFAI.Api
```

The API listens on the ASP.NET Core launch URL (typically `http://localhost:5000`). Check `GET /health`.

The current local adapter stores data in process memory. This is suitable for development and tests only. Endpoints accept an `organizationId` query parameter to exercise organization isolation locally; production must resolve organization identity from authenticated Microsoft Entra membership, never trust a client-supplied tenant identifier.

Funding calculations use the deterministic `percentage-with-cap-v1` formula and require an approved, effective, non-conflicting guideline. Configure local guideline fixtures with the `Guidelines` configuration section; each calculation returns its version, full trace, and source citations. A missing guideline intentionally blocks calculation.

This is an MVP local mode, not a production deployment. Azure storage, malware scanning, Microsoft Entra authentication, external partner-report access, and Azure AI integrations are not enabled or represented as successful. See `docs/MPFAI-Product-Specification.md` and `docs/MPFAI-Technical-Specification.md` for intended scope and production requirements.
