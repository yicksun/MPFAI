# MVP implementation and requirement gaps

This repository provides a runnable, local-only foundation, not the production MVP described by the PRD. All local records live in process memory and are lost when the API stops. `GET /health` explicitly reports `local-in-memory`; the UI and sample data are illustrative.

## Implemented local slices

| PRD IDs | Implemented behavior |
|---|---|
| CRM-002 (partial) | Organization-scoped customer name/domain and engagement CRUD; no tenant/contact/profile provenance fields. |
| PRG-008–011 (partial) | UTF-8 text-only guideline registration, immutable checksum/content in memory, version metadata, independent uploader/approver, effective dates, duplicate/conflict rejection, approval and supersession. |
| ANA-006–009 (partial) | Deterministic percentage-with-cap v1 calculation, currency/date/input checks, trace steps, and source citations. Unapproved, superseded, missing, expired, conflicting, or cross-organization guideline access is blocked. |
| ENG-003 (partial) | Owner, due date, task states, guarded transitions, and audit events. No dependencies, reminders, comments, templates, or notification delivery. |
| ATT-001/004/005 (partial) | Association metadata; setup request, report-complete, and independent evidence-referenced verification are distinct states. |
| CLM-006/007/011 (partial) | External claim ID and guarded claim lifecycle with audit entries. No external submission, deadlines, evidence mapping, payment reconciliation, or durable event history. |
| ADM-002/003 (local only) | Material actions append audit entries in memory. They are not durable or tamper-evident. |
| SOW-004/005 (limited) | Local text-only review job returns source lines and explicit missing inputs; it does not infer eligibility or funding. |

## Required before production

| PRD IDs | Gap |
|---|---|
| IAM-001–006 | Microsoft Entra authentication, MFA/Conditional Access integration, organization membership and role authorization, user lifecycle, and durable append-only audit are not implemented. The API refuses non-Development startup to prevent accidental unauthenticated deployment. |
| CRM-001, CRM-003–005 | Partner locations/qualifications, customer tenant/contact records, duplicate resolution, sensitive-field policy, and profile data provenance are absent. |
| SOW-001–009 | PDF/DOCX upload, malware scanning, extraction/OCR, immutable external document storage, correction/version workflows, source page extraction, and model data minimization are absent. The local analyzer accepts text only and is not AI. |
| PRG-001–007, PRG-009–011 | Complete program-rule schema, PDF/DOCX/XLSX extraction, malware scanning, reviewer rule-by-rule workflow, external immutable storage, fiscal/geographic coverage, and robust conflict resolution are absent. Text guideline upload is a local development fixture only. |
| ANA-001–005, ANA-010–016 | No approved-document retrieval, AI analysis, grounded eligibility findings, SOW matching, proposal decisions, opportunity promotion, or reproducible stored assessments. Only the standalone indicative calculator is implemented. |
| ENG-001–009 | Independent lifecycle state machines, readiness gates/exceptions, milestones/dependencies, customer acceptance, scope-change impact review, archive/restore, and escalations remain unimplemented. |
| ATT-002/003, ATT-006–011 | No identity/RBAC verification connector, Microsoft instructions, Partner Center report import, stale monitoring, alerts, attributed-revenue reconciliation, or closeout access workflow. Manual local evidence does not prove PAL or attribution. |
| CLM-001–005, CLM-008–012 | No program-derived evidence requirements, file storage, mapping/review history, deadline basis, claim pack, external claim-status synchronization, or payment reconciliation. Local evidence records are metadata only. |
| RPT-001–008, ADM-001/004/005 | No role-specific reporting, exports, notification delivery, organization configuration, retention/deletion, or live dependency-health probes. |
| NFR-001+, technical acceptance criteria | No production identity/network controls, SQL/Blob/Service Bus/Azure AI adapters, durable workers, backup/recovery, load/accessibility/security/AI evaluations, OpenTelemetry, or production deployment workflow. |

The Bicep and CI files are a starting point for infrastructure validation and continuous integration only; they do not make this application production-ready. See the two specifications for the full acceptance criteria and decisions that still require partner-specific discovery.
