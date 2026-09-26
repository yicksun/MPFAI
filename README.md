# MPFAI

Microsoft Partner Funding and Attribution Intelligence (MPFAI) is a development-only workspace for customer/engagement records, versioned funding guidelines, deterministic calculations, delivery tasks, attribution verification, evidence review, and claim lifecycle tracking.

## Run locally

Prerequisites: .NET 10 SDK, Node.js 22, and npm. The API uses local in-memory adapters; no Azure credentials or external services are required.

In PowerShell, start the API:

```powershell
dotnet restore MPFAI.sln
dotnet run --project src\MPFAI.Api
```

The launch profile uses `Development` and serves `http://localhost:5000`. Confirm `GET http://localhost:5000/health`; OpenAPI is at `/openapi/v1.json`. The API refuses to start outside Development until production authentication and adapters are implemented.

In a second PowerShell window:

```powershell
Push-Location apps\web
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:3000`. The seeded organization ID appears in `.env.example`. To populate a fresh API process with a sample customer, engagement, fictional guideline, and task, run `.\scripts\seed-local.ps1` from the repository root. The guideline in `sample-data\funding-guideline.txt` is explicitly fictional.

Every API operation requires an `organizationId` query parameter in this local profile to exercise tenant-scoped repository behavior. This is **not authentication**: callers can supply any organization ID. Never expose the local profile or its query-parameter identity model to a network or production environment.

## Verify changes

```powershell
dotnet test MPFAI.sln
dotnet format MPFAI.sln --verify-no-changes --no-restore
Push-Location apps\web
npm run lint
npm run build
npx tsc --noEmit
Pop-Location
```

The GitHub Actions workflow runs the same backend, frontend, and Bicep checks on pull requests and pushes to `master`.

## Local capabilities and limits

- Guideline registration accepts UTF-8 `.txt` source only, retains its bytes/checksum in process memory, versions metadata, detects overlapping conflicting parameters, and requires approval by someone other than the uploader. PDF, DOCX, XLSX, malware scanning, and durable Blob Storage are not implemented.
- Funding uses deterministic `percentage-with-cap-v1` arithmetic with decimal rounding, effective-date/currency checks, an ordered calculation trace, and citations. Every approval is internal; this never asserts Microsoft eligibility, approval, or payment.
- SOW review is a local, deterministic text-citation job marked `local-text-review-only`; it does not use AI, determine eligibility, or calculate funding.
- Attribution verification requires a setup report, independent verifier, method, and evidence reference. This metadata is not proof that Microsoft PAL is configured.
- Evidence and claim workflows track metadata/state only; no file bytes, external claim submission, deadline calculation, Partner Center import, or payment integration is provided.
- `GET /api/v1/integrations` reports unavailable integrations (`not_configured`) explicitly. The API does not return success for malware scans, Partner Center, or Azure AI. The Bicep file is an infrastructure foundation, not a production deployment.

See [the detailed PRD requirement coverage and gaps](docs/MPFAI-MVP-Gaps.md), [product specification](docs/MPFAI-Product-Specification.md), and [technical specification](docs/MPFAI-Technical-Specification.md). Production requires, at minimum, Entra authentication/authorization, durable SQL and Blob adapters, malware scanning, private network controls, observability, and validated operational and security controls.
