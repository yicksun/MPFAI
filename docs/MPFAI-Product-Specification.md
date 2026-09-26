# MPFAI Product Specification

**Product:** Microsoft Partner Funding and Attribution Intelligence (MPFAI)  
**Document type:** Product Requirements Document (PRD)  
**Status:** Draft for stakeholder review  
**Version:** 0.1  
**Date:** 26 September 2026  
**Primary platform:** Microsoft Azure

## 1. Product summary

MPFAI is a secure web application for Microsoft partners that connects three operational areas:

1. **SOW-to-funding analysis** identifies potential Microsoft funding opportunities from a statement of work, explains eligibility conditions, and creates the work required to prepare a nomination or claim.
2. **Azure partner-attribution monitoring** tracks whether delivery identities, Azure scopes, and the intended partner association are aligned and verifiable.
3. **Deal-to-delivery workflow** manages a deal from qualification through delivery, evidence collection, claim review, approval, payment, and closeout.

The product uses a shared customer and engagement record so information discovered during SOW analysis becomes actionable work rather than an isolated report.

### Product promise

> Turn every qualified Azure deal into an explainable, attributable, evidence-ready engagement.

### End-to-end lifecycle

`SOW intake -> funding assessment -> human approval -> deal approval -> delivery -> evidence -> claim -> payment`

Azure attribution is monitored from delivery readiness through engagement closeout.

## 2. Problem statement

Microsoft partners commonly manage funding eligibility, partner attribution, delivery milestones, evidence, and claims in separate documents and spreadsheets. This causes:

- missed funding opportunities and deadlines;
- inconsistent interpretation of program rules;
- SOWs that do not define measurable or evidence-ready outcomes;
- incomplete or unverified Partner Admin Link (PAL) setup;
- weak handoffs between sales, funding operations, delivery, and finance;
- missing proof of execution and customer attestations;
- limited visibility into requested, approved, and paid funding; and
- poor auditability when program rules or engagement scope change.

MPFAI addresses these problems without representing itself as Microsoft or making funding decisions on Microsoft's behalf.

## 3. Goals and success measures

### 3.1 Product goals

- Create one authoritative engagement workspace across sales, funding, delivery, attribution, evidence, and claims.
- Reduce the time required to assess a SOW against a curated funding-program library.
- Make every AI recommendation explainable with source text, rule version, assumptions, and confidence.
- Detect attribution gaps early enough for an accountable owner to remediate them.
- Improve claim readiness by mapping required evidence to delivery milestones.
- Provide management with reliable pipeline and operational-risk reporting.

### 3.2 MVP success measures

| Measure | Target within 90 days of production launch |
|---|---:|
| Median time from SOW upload to review-ready assessment | <= 10 minutes |
| SOW analyses with traceable source passages and rule versions | 100% |
| Potential matches reviewed by an authorized human before promotion | 100% |
| Active delivery engagements with an attribution state and owner | >= 95% |
| Required claim evidence mapped before delivery completion | >= 90% |
| Critical funding or claim deadlines with alerts configured | 100% |
| Reduction in manual status-tracking effort | >= 40% |
| High-severity cross-tenant data-access incidents | 0 |

Targets must be baselined during the pilot and can be revised with recorded stakeholder approval.

## 4. Scope

### 4.1 MVP scope

- Microsoft Entra ID authentication and role-based access.
- Partner organization and location profiles.
- Customer, tenant, subscription, and engagement records.
- PDF and DOCX SOW upload, extraction, correction, and versioning.
- Curated, version-controlled funding-program library.
- Explainable SOW-to-program matching and gap analysis.
- Human review and approval of analysis results.
- Engagement lifecycle, tasks, milestones, dependencies, and alerts.
- Guided PAL setup and evidence-based verification workflow.
- Partner Center report import for reconciliation where authorized.
- Evidence requirements, uploads, approvals, and claim-readiness checks.
- External claim status, action-required, dispute, approval, and payment tracking.
- Operational dashboards, exports, audit logs, and notifications.

### 4.2 Out of scope for MVP

- Automatic submission of nominations or claims to Microsoft.
- Guaranteed eligibility, funding, attribution, or payment determinations.
- Universal coverage of every Microsoft program, geography, and fiscal year.
- Unverified claims of real-time Partner Center or PAL telemetry.
- Customer Azure access acquisition or modification by MPFAI.
- CRM replacement, project accounting, payroll, or full resource scheduling.
- Automated SOW modification without explicit human approval.
- Retaining customer access solely to preserve partner attribution.

### 4.3 Post-MVP candidates

- Dynamics 365 and Salesforce synchronization.
- Customer portal for consent, acceptance, and attestations.
- Electronic signature integration.
- Automated rule ingestion with administrator approval.
- Program-change impact analysis across the active portfolio.
- Specialization-readiness and workforce-certification planning.
- Finance-system reconciliation and revenue forecasting.
- Supported Partner Center API integrations proven during discovery.

## 5. Users and permissions

### 5.1 Primary personas

| Persona | Primary needs |
|---|---|
| Sales / Account Manager | Qualify deals, upload SOWs, understand potential funding, and complete handoff. |
| Funding Operations Analyst | Maintain program rules, validate eligibility, prepare claims, and manage deadlines. |
| Delivery Manager | Plan milestones, assign evidence, manage scope changes, and track delivery risks. |
| Consultant / Engineer | Complete assigned delivery, evidence, and attribution tasks with minimum friction. |
| Finance Analyst | Track requested, approved, paid, rejected, and disputed amounts. |
| Executive / Practice Leader | View opportunity, delivery, attribution, claim, and risk summaries. |
| Organization Administrator | Configure identity, access, integrations, retention, and partner profile data. |
| Auditor / Read-only Reviewer | Review records, decisions, source documents, evidence, and immutable history. |

### 5.2 MVP roles

| Role | Core permissions |
|---|---|
| Administrator | Tenant configuration, users, roles, integrations, retention, and all records. |
| Program Manager | Program library administration, assessments, claims, and reporting. |
| Engagement Manager | Customer and engagement management, tasks, milestones, evidence, and attribution. |
| Contributor | Work on assigned records, upload evidence, and update assigned tasks. |
| Finance | Funding values, claims, payments, and finance reporting. |
| Executive Viewer | Read-only dashboards and approved summaries. |
| Auditor | Read-only records, versions, evidence metadata, and audit history. |

Permissions must support least privilege, organization isolation, record-level authorization where required, and separation of program-rule authoring from approval.

## 6. Core concepts

- **Organization:** A Microsoft partner using MPFAI.
- **Partner location:** An enrolled or operating location with its own identifiers and eligibility.
- **Customer:** The customer organization associated with one or more engagements.
- **Engagement:** The central record connecting a deal, SOW, funding opportunities, delivery, Azure scopes, attribution, evidence, and claims.
- **SOW version:** An immutable uploaded document plus its extraction and analysis results.
- **Program definition:** A versioned representation of qualification rules, rates, evidence, dates, and official references.
- **Assessment:** An analysis of one SOW version against one or more program versions.
- **Funding opportunity:** A reviewed potential match associated with an engagement.
- **Attribution record:** The intended partner, identity, customer tenant, Azure scope, verification method, and current state.
- **Evidence requirement:** A program-specific item that must be demonstrated or supplied.
- **Evidence item:** A document, attestation, acceptance, invoice, output, or other artifact mapped to one or more requirements.
- **Claim:** MPFAI's record of an externally submitted claim and its lifecycle.

## 7. Key user journeys

### 7.1 SOW to reviewed funding opportunity

1. Sales creates or selects a customer and engagement.
2. Sales uploads a PDF or DOCX SOW.
3. MPFAI extracts customer, scope, Azure workloads, deliverables, dates, fees, and expected outcomes.
4. The user corrects extraction errors and confirms the analysis inputs.
5. MPFAI evaluates the SOW against active, applicable program versions.
6. Results show potential matches, outstanding conditions, non-matches, and insufficient-information cases.
7. A funding analyst reviews the rule evidence, SOW passages, assumptions, and estimate.
8. Approved opportunities create funding records, required tasks, deadlines, and evidence requirements.

### 7.2 Deal to delivery readiness

1. An approved opportunity enters pre-delivery review.
2. MPFAI checks customer consent or pre-approval requirements, SOW approval, delivery ownership, evidence plan, and attribution plan.
3. Mandatory blockers prevent delivery-ready status unless an authorized user records a permitted exception.
4. The delivery manager approves the baseline plan.
5. The engagement enters delivery and begins milestone tracking.

### 7.3 Attribution setup and monitoring

1. The delivery manager records customer tenant, Azure scopes, delivery identities, and intended Partner ID.
2. MPFAI assigns PAL or other association setup tasks with Microsoft guidance.
3. A consultant reports completion and supplies verification evidence.
4. An authorized verifier confirms the association using a supported method.
5. MPFAI monitors freshness and imports available reports for reconciliation.
6. Missing, stale, changed, or unknown states generate alerts and remediation tasks.
7. Closeout includes customer-access removal or handover tasks.

### 7.4 Delivery to claim and payment

1. Milestones create evidence collection tasks.
2. Users upload artifacts and map them to specific requirements.
3. Reviewers approve, reject, or request replacement evidence.
4. Scope changes trigger reassessment of eligibility, estimates, evidence, and deadlines.
5. When requirements are complete, MPFAI generates a claim-readiness checklist and preparation pack.
6. A funding analyst submits the claim externally and records its ID and submission date.
7. MPFAI tracks review, action-required, resubmission, dispute, approval, rejection, and payment.
8. Finance records or imports payment and closes the financial lifecycle.

## 8. Functional requirements

Priority uses **Must**, **Should**, and **Could**.

### 8.1 Identity, organization, and access

| ID | Requirement | Priority |
|---|---|---|
| IAM-001 | Authenticate users through Microsoft Entra ID using organization-approved tenant policies. | Must |
| IAM-002 | Enforce organization isolation and role-based authorization on every API and background operation. | Must |
| IAM-003 | Support configurable user roles and security groups without embedding permissions in UI-only logic. | Must |
| IAM-004 | Record privileged administrative and approval actions in an immutable audit history. | Must |
| IAM-005 | Support optional single-tenant deployment initially and a multi-tenant SaaS model without changing the domain model. | Should |
| IAM-006 | Support user deactivation without deleting historical attribution, approval, or audit records. | Must |

### 8.2 Customer and partner profiles

| ID | Requirement | Priority |
|---|---|---|
| CRM-001 | Maintain partner organization, Partner IDs, locations, enrollments, designations, specializations, and effective dates. | Must |
| CRM-002 | Maintain customer name, domain, tenant identifiers, geography, contacts, and applicable business identifiers. | Must |
| CRM-003 | Prevent accidental duplicate customers while allowing authorized merges with full history. | Should |
| CRM-004 | Treat customer identifiers and contacts as sensitive organization data. | Must |
| CRM-005 | Track the source, owner, last verification date, and confidence of profile data. | Must |

### 8.3 SOW intake and document processing

| ID | Requirement | Priority |
|---|---|---|
| SOW-001 | Accept PDF and DOCX uploads and reject unsupported, malicious, encrypted, or policy-exceeding files with a clear error. | Must |
| SOW-002 | Scan uploaded files for malware before extraction or user download. | Must |
| SOW-003 | Extract text and structure, including OCR for supported scanned documents. | Must |
| SOW-004 | Extract customer, dates, scope, workloads, deliverables, fees, assumptions, dependencies, outcomes, and consumption estimates. | Must |
| SOW-005 | Display each extracted value with its source page or passage and extraction confidence. | Must |
| SOW-006 | Allow authorized users to correct extracted values without altering the original document. | Must |
| SOW-007 | Store each upload and analysis as an immutable version linked to the engagement. | Must |
| SOW-008 | Compare SOW versions and identify changes affecting funding, delivery, attribution, evidence, or deadlines. | Should |
| SOW-009 | Limit model inputs to the minimum document content required for the selected analysis. | Must |

### 8.4 Program library

| ID | Requirement | Priority |
|---|---|---|
| PRG-001 | Store versioned program name, program owner, solution area, geography, partner criteria, customer criteria, activities, exclusions, rates, caps, dates, evidence, deadlines, and official sources. | Must |
| PRG-002 | Preserve historical versions so an assessment can always be reproduced using its original rules. | Must |
| PRG-003 | Require effective dates, last-reviewed date, source references, author, and approver before a program version becomes active. | Must |
| PRG-004 | Support draft, under-review, active, superseded, and retired program-version states. | Must |
| PRG-005 | Prevent the same user from approving their own material rule change unless explicitly permitted and audited. | Should |
| PRG-006 | Notify program managers before rules or source documents become stale or expire. | Should |
| PRG-007 | Allow rule coverage to begin with a curated subset rather than imply universal Microsoft program coverage. | Must |
| PRG-008 | Provide an administrator-managed system document library for Microsoft Partner funding guideline files, including PDF, DOCX, and XLSX where supported. | Must |
| PRG-009 | Malware-scan, extract, classify, and version every uploaded guideline while preserving the immutable original file, uploader, upload date, effective period, geography, program, fiscal year, and document checksum. | Must |
| PRG-010 | Require an authorized program manager to review and approve extracted funding rules and calculation parameters before a guideline version can be used for funding analysis. | Must |
| PRG-011 | Detect replacement, duplicate, conflicting, expired, or superseded guideline documents and block ambiguous rules from authoritative calculations until resolved. | Must |

### 8.5 SOW-to-funding analysis

| ID | Requirement | Priority |
|---|---|---|
| ANA-001 | Read the applicable Microsoft Partner funding guideline documents uploaded by an administrator to the system document library and use only approved, effective guideline versions for analysis. | Must |
| ANA-002 | Classify each result as potential match, conditions outstanding, not eligible, or insufficient information. | Must |
| ANA-003 | Retrieve the relevant eligibility rules, rates, caps, thresholds, exclusions, dates, eligible activities, evidence requirements, and calculation examples from the approved guideline documents. | Must |
| ANA-004 | Keep model confidence separate from program eligibility and reviewer disposition. | Must |
| ANA-005 | Evaluate a confirmed SOW extraction and partner/customer inputs against the rules extracted from the applicable approved guideline versions. | Must |
| ANA-006 | Calculate indicative funding using deterministic formulas and parameters derived from the approved guideline documents; the AI model may extract or explain rules but must not perform the authoritative arithmetic. | Must |
| ANA-007 | Validate all required calculation inputs and return insufficient information rather than an amount when mandatory inputs, applicable rules, or calculation parameters are missing, conflicting, expired, or unapproved. | Must |
| ANA-008 | Display the complete calculation breakdown, including input values, formula, rate, cap, threshold, currency, assumptions, exclusions, intermediate values, and final indicative amount. | Must |
| ANA-009 | Cite every eligibility conclusion and calculation parameter to the exact guideline document version, page, section, table, or cell and preserve the relevant source excerpt. | Must |
| ANA-010 | Show every result's relevant rule, source citation, supporting or conflicting SOW passage, assumptions, missing information, and model confidence. | Must |
| ANA-011 | Separate estimated, requested, approved, disputed, and paid amounts. | Must |
| ANA-012 | Suggest SOW improvements as tracked proposals requiring explicit human accept, edit, or reject decisions. | Must |
| ANA-013 | Never invent customer commitments, delivery facts, qualifications, calculation parameters, guideline content, or evidence. | Must |
| ANA-014 | Require authorized human approval before a potential match or calculated amount becomes an active funding opportunity. | Must |
| ANA-015 | Generate an executive summary, detailed assessment, calculation worksheet, gap checklist, and reviewer decision record. | Must |
| ANA-016 | Re-run an assessment when the SOW, relevant partner/customer inputs, or applicable approved guideline version changes, while preserving prior results and calculations. | Should |

### 8.6 Engagement and delivery workflow

| ID | Requirement | Priority |
|---|---|---|
| ENG-001 | Provide one engagement workspace linking all product modules. | Must |
| ENG-002 | Track sales, funding, delivery, attribution, claim, and financial states independently. | Must |
| ENG-003 | Support owners, followers, tasks, dependencies, due dates, reminders, escalation, comments, and attachments. | Must |
| ENG-004 | Support configurable milestone templates based on engagement or funding type. | Should |
| ENG-005 | Evaluate delivery-readiness gates and identify blockers separately from warnings. | Must |
| ENG-006 | Permit exceptions only to authorized roles and require reason, approver, date, and affected requirement. | Must |
| ENG-007 | Record customer acceptance and delivery completion without conflating either with claim approval. | Must |
| ENG-008 | Trigger impact review when scope, dates, price, workload, customer, Azure scope, or delivery identity changes. | Must |
| ENG-009 | Support soft archive and restore while preserving child records and audit history. | Must |

### 8.7 Azure attribution

| ID | Requirement | Priority |
|---|---|---|
| ATT-001 | Map an engagement to customer tenant, subscription or resource scope, delivery identity, intended Partner ID, and association type. | Must |
| ATT-002 | Support consultant identities, service principals, and Azure Lighthouse scenarios as distinct patterns. | Must |
| ATT-003 | Provide guided setup tasks based on current official Microsoft instructions. | Must |
| ATT-004 | Represent setup requested, consultant reported complete, verification pending, verified, missing, mismatched, stale, and unknown as distinct states. | Must |
| ATT-005 | Store verification method, verifier, verification date, evidence, scope, and result. | Must |
| ATT-006 | Never treat message delivery, link opening, or consultant self-report as verified attribution. | Must |
| ATT-007 | Where authorized and supported, check identity status and relevant Azure RBAC scope without changing customer access. | Should |
| ATT-008 | Import authorized Partner Center reports and retain import period, source file, processing status, and reconciliation results. | Must |
| ATT-009 | Keep estimated consumption separate from Microsoft-reported attributed revenue. | Must |
| ATT-010 | Alert on stale verification, disabled identity, changed scope, mismatched Partner ID, missing report coverage, or connector failure. | Must |
| ATT-011 | Include access-removal or handover tasks during engagement closeout. | Must |

### 8.8 Evidence and claims

| ID | Requirement | Priority |
|---|---|---|
| CLM-001 | Generate evidence requirements from approved funding opportunities and program versions. | Must |
| CLM-002 | Map each evidence item to one or more requirements, milestones, and claims. | Must |
| CLM-003 | Track evidence owner, source, date, applicable period, sensitivity, review state, and reviewer comments. | Must |
| CLM-004 | Support draft, submitted-for-review, accepted, rejected, replaced, and expired evidence states. | Must |
| CLM-005 | Build a claim-readiness checklist including applicable invoices, proof of execution, customer attestation, partner attestation, and required approvals. | Must |
| CLM-006 | Record external claim ID, portal, engagement type, trigger dates, deadlines, and authoritative external status. | Must |
| CLM-007 | Track draft, consent states, submitted, under review, action required, approved, rejected, disputed, expired, and paid states where applicable. | Must |
| CLM-008 | Calculate deadlines from the approved program rule version and recorded trigger events; display the calculation basis. | Must |
| CLM-009 | Alert before submission, action-required, resubmission, and dispute deadlines. | Must |
| CLM-010 | Generate a downloadable preparation pack without claiming that MPFAI submitted it to Microsoft. | Must |
| CLM-011 | Preserve claim history, review comments, resubmissions, disputes, and final decisions. | Must |
| CLM-012 | Allow finance to reconcile approved amounts and payments without changing the funding review decision. | Must |

### 8.9 Dashboards, notifications, and exports

| ID | Requirement | Priority |
|---|---|---|
| RPT-001 | Provide role-specific dashboards for sales, funding, delivery, attribution, finance, and executives. | Must |
| RPT-002 | Show funding amounts by estimate, request, approval, rejection, dispute, and payment state. | Must |
| RPT-003 | Show upcoming deadlines, overdue tasks, stalled engagements, evidence gaps, attribution risks, and action-required claims. | Must |
| RPT-004 | Make the data period, last refresh time, filters, and data source visible on reports. | Must |
| RPT-005 | Exclude speculative funding from committed or paid revenue metrics. | Must |
| RPT-006 | Support filtered CSV/XLSX exports and PDF executive summaries with access checks. | Should |
| RPT-007 | Send in-app and email notifications with configurable frequency and escalation. | Must |
| RPT-008 | Deduplicate repeated alerts and record notification delivery failures. | Should |

### 8.10 Administration and audit

| ID | Requirement | Priority |
|---|---|---|
| ADM-001 | Configure organization profile, defaults, fiscal periods, currencies, retention, integrations, and notification policies. | Must |
| ADM-002 | Record actor, timestamp, action, previous value, new value, record, and correlation ID for material changes. | Must |
| ADM-003 | Make audit records append-only to normal application users and administrators. | Must |
| ADM-004 | Support legal retention and deletion workflows without silently removing referenced evidence or decisions. | Should |
| ADM-005 | Provide integration-health status and surface failures without success-shaped fallbacks. | Must |

## 9. Lifecycle models

### 9.1 Assessment

`Draft -> Extracting -> User confirmation required -> Analyzing -> Analyst review -> Approved / Rejected / Superseded`

Failed extraction or analysis is a visible recoverable state with diagnostic detail and retry controls.

### 9.2 Funding opportunity

`Potential -> Conditions outstanding -> Qualified internally -> Pre-approval pending -> Approved externally / Not approved / Withdrawn / Expired`

The exact states used must be configurable by program. Internal qualification must never be labeled Microsoft approval.

### 9.3 Delivery

`Proposed -> Pre-delivery review -> Ready -> In progress -> At risk -> Delivered -> Closed / Cancelled`

### 9.4 Attribution

`Not planned -> Setup requested -> Reported complete -> Verification pending -> Verified`

Any active record can transition to `Missing`, `Mismatched`, `Stale`, or `Unknown` when evidence or data becomes insufficient.

### 9.5 Claim

`Not started -> Draft -> Consent pending -> Ready to submit -> Submitted -> Under review -> Action required -> Resubmitted -> Approved -> Payment pending -> Paid`

Alternative outcomes include `Rejected`, `Disputed`, `Rejected final`, `Cancelled`, and `Expired`. Applicability depends on the program version.

## 10. Business rules

1. A potential program match cannot become an active opportunity without human approval.
2. Every assessment result must reference the exact SOW version and program version used.
3. Replacing a program rule never rewrites historical assessments or claims.
4. Funding estimates must use deterministic formulas and parameters traceable to approved, effective Microsoft Partner funding guideline documents in the system document library; an LLM must not perform the authoritative financial calculation.
5. An engagement may have multiple opportunities and claims, but each amount must retain its own currency and lifecycle.
6. Delivery completion, evidence completion, claim approval, and payment are separate events.
7. A PAL invitation or self-reported completion does not equal verification.
8. Connector or import failure results in `Unknown` or `Stale`, never `Verified`.
9. A scope change affecting eligibility, evidence, amount, dates, Azure scope, or identity creates a mandatory impact-review task.
10. Required blockers cannot be bypassed unless the program permits an exception and an authorized approver records it.
11. All deadlines must identify their source rule and trigger event.
12. Archived records remain excluded from active metrics but retain full history and remain recoverable subject to retention policy.

## 11. Information architecture

### 11.1 Primary navigation

- **Dashboard**
- **Customers**
- **Engagements**
- **SOW Analysis**
- **Funding**
- **Attribution**
- **Evidence & Claims**
- **Reports**
- **Administration**

### 11.2 Engagement workspace tabs

- Overview
- SOWs & Assessments
- Funding
- Delivery Plan
- Attribution
- Evidence
- Claims & Payments
- Activity & Audit

### 11.3 Dashboard principles

- Present exceptions and next actions before aggregate vanity metrics.
- Show owner, severity, due date, source, and recommended action for each risk.
- Distinguish verified facts, imported facts, estimates, and AI suggestions visually.
- Preserve filters in shareable URLs where authorization permits.

## 12. Conceptual data model

### 12.1 Principal entities

- `Organization`
- `OrganizationUser`
- `RoleAssignment`
- `PartnerLocation`
- `PartnerQualification`
- `Customer`
- `CustomerContact`
- `CustomerTenant`
- `AzureScope`
- `Engagement`
- `EngagementOwner`
- `SowDocument`
- `SowVersion`
- `ExtractionResult`
- `Program`
- `ProgramVersion`
- `ProgramRule`
- `ProgramSource`
- `Assessment`
- `AssessmentFinding`
- `ReviewerDecision`
- `FundingOpportunity`
- `FundingAmount`
- `Task`
- `TaskDependency`
- `Milestone`
- `ScopeChange`
- `AttributionRecord`
- `DeliveryIdentity`
- `AttributionVerification`
- `EvidenceRequirement`
- `EvidenceItem`
- `EvidenceReview`
- `Claim`
- `ClaimEvent`
- `Payment`
- `Notification`
- `ImportJob`
- `AuditEvent`

### 12.2 Relationship constraints

- Every tenant-owned entity includes an immutable `organization_id`.
- An engagement belongs to one customer but can reference multiple SOWs, Azure scopes, opportunities, and claims.
- An assessment references exactly one SOW version and one or more immutable program versions.
- An assessment finding references at least one rule and zero or more source passages; missing passages require a recorded reason.
- A funding opportunity references the reviewer decision that promoted it.
- Evidence items can satisfy multiple requirements, but each mapping has an explicit reviewer state.
- Every state transition produces a timestamped event and audit entry.
- Monetary values store amount, currency, amount type, effective date, source, and calculation details.

## 13. AI and decision-support requirements

### 13.1 Permitted AI uses

- Document classification, OCR post-processing, and structured extraction.
- Azure workload and delivery-scope identification.
- Retrieval of relevant program rules from the approved library.
- Draft matching explanations and gap analysis.
- Draft SOW enhancement suggestions.
- Summaries, checklists, and reviewer assistance.

### 13.2 Prohibited autonomous decisions

- Final funding eligibility or Microsoft approval.
- Authoritative funding calculation outside approved formulas.
- Customer consent, partner attestation, or reviewer approval.
- Silent alteration of a SOW, program rule, evidence, or claim.
- Fabrication of facts, citations, qualifications, deliverables, or evidence.

### 13.3 Explainability and quality

- Each suggestion must display source passages, applicable program rules, program version, model version, analysis timestamp, assumptions, and confidence.
- Users must be able to accept, edit, reject, or mark a suggestion as not applicable.
- Corrections and reviewer decisions should be captured for quality evaluation but must not be used for model training without approved governance.
- The system must support a deterministic evaluation set with representative SOWs, rules, edge cases, and expected outputs.
- Production model or prompt changes require offline evaluation, approval, versioning, monitoring, and rollback.

### 13.4 Retrieval safeguards

- Retrieval is restricted by organization and user authorization.
- Only approved, effective program versions can support an active recommendation.
- Retrieved text is treated as untrusted data and cannot override system behavior.
- Citations must resolve to the stored source version used in the assessment.

## 14. Azure solution architecture

### 14.1 Recommended MVP architecture

| Capability | Azure service |
|---|---|
| Web UI and API | Azure App Service |
| Background processing | Azure Functions or WebJobs, selected during technical design |
| Operational relational data | Azure SQL Database |
| SOWs, evidence, and generated packs | Azure Blob Storage |
| Document extraction and OCR | Azure AI Document Intelligence |
| AI-assisted analysis | Azure AI Foundry model deployment |
| Program-document retrieval | Azure AI Search |
| Authentication | Microsoft Entra ID |
| Secrets and certificates | Azure Key Vault |
| Messaging and durable work | Azure Service Bus |
| Monitoring and tracing | Azure Monitor and Application Insights |
| Edge security | Azure Front Door with Web Application Firewall when internet-facing scale requires it |

### 14.2 Architectural principles

- Use managed identities instead of embedded credentials.
- Store structured operational relationships in Azure SQL; store original and generated documents in Blob Storage.
- Use private endpoints and network restrictions for production data services where supported by the deployment model.
- Process documents asynchronously through a durable queue.
- Make operations idempotent and attach a correlation ID to every workflow.
- Separate web/API availability from long-running document and AI processing.
- Keep model provider, model version, prompt version, and rule version observable.
- Use infrastructure as code and separate development, test, staging, and production environments.

### 14.3 Processing flow

1. API validates authorization, metadata, file type, and upload limits.
2. File is quarantined and malware-scanned.
3. Accepted file is stored using an immutable object key.
4. A Service Bus message starts extraction.
5. Document Intelligence returns structured content and page references.
6. User confirmation resolves uncertain material fields.
7. AI Search retrieves applicable approved program rules.
8. The model drafts findings and explanations.
9. Deterministic services apply rule checks and calculate indicative amounts.
10. Results are stored with all versions, citations, and telemetry.
11. A reviewer approves, rejects, or requests additional information.

## 15. Security, privacy, and compliance

### 15.1 Security baseline

- Enforce Microsoft Entra ID authentication, MFA and Conditional Access through customer policy.
- Authorize every request server-side and filter all data by organization.
- Use managed identities and Key Vault; no secrets in source code or application configuration.
- Encrypt data in transit and at rest with Azure-supported controls.
- Scan uploads and generated downloads for malware.
- Validate content type, extension, size, decompression limits, and document structure.
- Apply rate limits, anti-automation controls, and secure session handling.
- Protect internet-facing endpoints with standard web controls and WAF where appropriate.
- Log security-sensitive actions and integration failures without logging document contents, tokens, or secrets.
- Conduct threat modeling, dependency scanning, static analysis, penetration testing, and disaster-recovery testing before production.

### 15.2 Data governance

- Classify SOWs, pricing, customer identifiers, evidence, and claims as confidential partner/customer data.
- Configure retention by record type and organization policy.
- Allow legal hold where required.
- Define region and residency during deployment; Azure hosting alone is not a residency guarantee.
- Do not use customer content to train shared models without explicit contractual and technical authorization.
- Keep source documents and generated content separated by organization and environment.
- Record downloads and exports of sensitive data.

### 15.3 Auditability

The system must be able to answer:

- Who changed this value or state?
- What was the previous value?
- Which SOW and rule versions produced this recommendation?
- Which model and prompt versions were used?
- Which person approved or rejected the recommendation?
- What evidence supported the claim?
- What deadline rule and trigger produced this due date?
- What was imported, when, and from which source?

## 16. Non-functional requirements

### 16.1 Availability and recovery

| Requirement | MVP target |
|---|---:|
| Monthly service availability | 99.9%, excluding announced maintenance |
| Recovery point objective | <= 15 minutes for operational data |
| Recovery time objective | <= 4 hours |
| Backup restore test | At least quarterly |

### 16.2 Performance

| Operation | Target |
|---|---:|
| Standard authenticated page API, p95 | <= 2 seconds |
| Dashboard query, p95 | <= 3 seconds |
| Upload acknowledgement | <= 5 seconds after transfer completes |
| SOW processing | Progress visible within 10 seconds; review-ready median <= 10 minutes |
| Search query, p95 | <= 3 seconds |

Long-running processing must not hold an HTTP request open and must expose status, progress, failure reason, and retry state.

### 16.3 Scale assumptions for MVP

- Up to 50 partner organizations or one enterprise deployment.
- Up to 1,000 active users.
- Up to 10,000 engagements.
- Up to 50,000 documents.
- Up to 500 concurrent background jobs at peak through controlled queueing.

These are sizing assumptions, not contractual limits, and must be validated during load testing.

### 16.4 Accessibility and usability

- Meet WCAG 2.2 AA for core workflows.
- Support keyboard navigation, visible focus, semantic landmarks, screen readers, and sufficient contrast.
- Do not use color as the only indicator of state.
- Use plain-language errors with remediation guidance and correlation IDs.
- Preserve unsaved user work where feasible and warn before destructive navigation.

### 16.5 Observability

- Distributed tracing across API, queue, worker, document extraction, AI, search, and storage.
- Metrics for latency, failures, queue depth, retries, model token usage, extraction quality, and connector freshness.
- Alerts for failed processing, dead-letter messages, authorization anomalies, stale imports, excessive cost, and deadline-notification failures.
- Business telemetry must avoid document content and personally identifiable information unless explicitly approved.

## 17. Reporting definitions

To prevent misleading dashboards, use the following amount categories:

- **Potential:** Model- or analyst-identified opportunity not yet internally qualified.
- **Estimated:** Internally reviewed indicative amount based on current assumptions.
- **Requested:** Amount included in an external nomination or claim.
- **Approved:** Amount approved by the external authority.
- **Paid:** Amount confirmed received by finance or an authoritative payment source.
- **Rejected:** Amount associated with a rejected or final-rejected claim.
- **At risk:** Amount with an unresolved blocker, missed milestone, attribution issue, evidence gap, or approaching deadline.

Dashboards must never combine these categories into a single revenue number without showing the composition.

## 18. MVP acceptance criteria

### 18.1 SOW analysis

- A permitted user can upload a valid PDF or DOCX and see extraction progress.
- A malicious, unsupported, oversized, encrypted, or corrupt file is safely rejected with a meaningful error.
- Extracted material fields show source locations and can be corrected without changing the original.
- Analysis reads only applicable, approved, effective Microsoft Partner funding guideline versions from the administrator-managed system document library and records their immutable IDs and checksums.
- Every result displays classification, explanation, source rules, SOW evidence, assumptions, and confidence.
- Every eligibility rule and calculation parameter cites the exact guideline file version and page, section, table, or cell from which it was derived.
- Indicative funding is calculated by a deterministic formula derived from an approved guideline and displays inputs, intermediate values, rate, cap, threshold, currency, exclusions, and final amount.
- Missing, conflicting, expired, or unapproved guideline content prevents calculation and produces an explicit insufficient-information or configuration-error result.
- No opportunity is promoted without an authorized reviewer decision.
- Accepted SOW suggestions produce a new derived version; the original remains unchanged.

### 18.2 Engagement workflow

- An approved opportunity creates the configured tasks, evidence requirements, and deadlines.
- Sales, funding, delivery, attribution, claim, and financial states can change independently.
- Mandatory blockers prevent delivery-ready status unless an authorized, permitted exception is recorded.
- A material scope change creates an impact-review task and identifies affected records.
- Every material state change appears in activity and audit history.

### 18.3 Attribution

- An engagement can record tenant, Azure scope, delivery identity, intended Partner ID, and association type.
- Setup requested, reported complete, and verified are visibly distinct.
- Verification records method, evidence, verifier, date, and scope.
- A stale or failed data source changes the state to stale or unknown and alerts the owner.
- Imported reports retain source, reporting period, processing outcome, and reconciliation history.
- Estimated consumption cannot appear as Microsoft-reported attributed revenue.

### 18.4 Evidence, claims, and payments

- Evidence requirements are generated from the exact approved program version.
- A reviewer can accept, reject, or request replacement evidence.
- Claim readiness reports every missing mandatory item.
- Claim dates and deadlines identify their rule and trigger event.
- Action-required and dispute states create tasks and alerts.
- Approved and paid are separate states with separate dates and amounts.
- The generated claim pack is clearly labeled as preparation material, not proof of submission.

### 18.5 Security and reliability

- Automated authorization tests demonstrate that users cannot access another organization by changing identifiers.
- Secrets are absent from source and deployment output.
- Backup restore, queue retry, dead-letter handling, and worker idempotency are tested.
- Audit records cannot be edited through normal application interfaces or APIs.
- Accessibility tests cover the principal journeys.
- Production readiness review confirms monitoring, alerts, runbooks, and incident contacts.

## 19. Delivery roadmap

### Phase 0: Discovery and validation

- Confirm target deployment model: single partner, private enterprise, or multi-tenant SaaS.
- Select the initial program and geography subset.
- Obtain representative redacted SOWs, rules, claims, reports, and evidence.
- Validate supported Partner Center data access and PAL verification methods.
- Baseline current process time, missed deadlines, claim quality, and attribution coverage.
- Complete threat model, data classification, and integration feasibility review.

### Phase 1: Foundation

- Entra ID, roles, organization boundaries, customer and engagement records.
- Document storage, upload security, audit history, tasks, and notifications.
- Program library authoring, approval, and version control.

### Phase 2: SOW intelligence

- Extraction, user correction, retrieval, explainable matching, deterministic estimates, and human review.
- Assessment exports and approved opportunity creation.

### Phase 3: Delivery and evidence

- Readiness gates, milestones, scope changes, evidence mapping, reviews, and claim preparation.

### Phase 4: Attribution and reconciliation

- Guided PAL workflows, verification records, report imports, health dashboard, alerts, and closeout.

### Phase 5: Claims, payments, and pilot hardening

- Claim lifecycle, deadlines, disputes, payment reconciliation, executive reporting, load testing, security testing, and pilot rollout.

## 20. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Program rules change frequently or are not fully public | Curated library, versioning, effective dates, official sources, freshness alerts, and human approval. |
| AI overstates eligibility or invents supporting facts | Retrieval grounding, explicit uncertainty, deterministic rules, prohibited autonomous decisions, evaluations, and reviewer approval. |
| Partner Center or PAL data is not available through a supported API | Begin with guided verification and authorized imports; label unknown states; validate integrations before promising automation. |
| Confidential SOW or customer data crosses organization boundaries | Server-side tenant isolation, authorization tests, managed identities, private networking, export controls, and security reviews. |
| Users confuse internal qualification with Microsoft approval | Separate terminology, states, labels, dashboards, and audit events. |
| Deadline calculations become incorrect | Versioned rule formulas, explicit trigger events, test fixtures, review controls, and visible calculation basis. |
| Workflow becomes too complex for consultants | Role-specific views, assigned-task inbox, progressive disclosure, templates, and pilot usability testing. |
| Reports inflate revenue with speculative funding | Strict amount categories and no default aggregation of potential, estimated, approved, and paid values. |

## 21. Dependencies

- Microsoft Entra tenant and application registration.
- Azure subscription, networking, governance, logging, and budget controls.
- Approved Azure AI model availability and regional capacity.
- Azure AI Document Intelligence support for selected documents and regions.
- Current official program materials and subject-matter experts.
- Representative redacted SOWs and claim evidence for evaluation.
- Confirmed data-access method for Partner Center reporting.
- Security, legal, privacy, and records-management approval.

## 22. Decisions required before implementation

1. Deployment model: private single-partner system or multi-tenant SaaS.
2. First geography, fiscal year, solution areas, and funding programs.
3. Source of authoritative partner qualification and certification data.
4. Supported PAL verification methods and approved customer-access model.
5. CRM, project-management, finance, and notification integrations for MVP.
6. Data-retention periods and required Azure deployment region.
7. Maximum document size, supported languages, and OCR requirements.
8. Whether customers need portal access during MVP.
9. Pilot organizations, success baseline, and acceptance authority.
10. Commercial model, service-level objectives, and support ownership.

## 23. Reference sources

- [AI Cloud Partners product reference](https://www.aicloudpartners.com/)
- [Microsoft: Link a partner ID to an account used to manage customers](https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/link-partner-id)
- [Microsoft: MCI engagements overview and eligibility](https://learn.microsoft.com/en-us/partner-center/incentives/mci-engagements)
- [Microsoft: Submit an MCI engagement claim](https://learn.microsoft.com/en-us/partner-center/incentives/mci-engagements-workshop)

Program rules, rates, deadlines, evidence requirements, and integration capabilities must be revalidated against current authoritative Microsoft materials before implementation and on every rule update.

---

## Appendix A: Suggested MVP screens

1. Sign in and organization selection
2. Role-specific dashboard
3. Customer list and customer profile
4. Engagement list and engagement workspace
5. SOW upload and extraction review
6. Analysis progress and results
7. Finding detail and SOW suggestion review
8. Program library and version approval
9. Funding opportunity detail
10. Delivery plan and task board
11. Attribution setup and health dashboard
12. Evidence matrix and evidence review
13. Claim detail, deadline timeline, and preparation pack
14. Payments and finance reconciliation
15. Reports and exports
16. Users, roles, integrations, retention, and audit administration

## Appendix B: Initial notification catalogue

- SOW extraction or analysis failed.
- Assessment requires reviewer action.
- Program source or version is approaching expiry.
- Funding pre-approval or customer consent is overdue.
- Delivery milestone or evidence item is overdue.
- Material scope change requires impact review.
- Attribution setup is incomplete.
- Attribution verification is stale, mismatched, or unknown.
- Report import failed or is stale.
- Claim submission deadline is approaching.
- Claim status is action required.
- Resubmission or dispute deadline is approaching.
- Approved payment is overdue for reconciliation.
- Integration, notification delivery, or background processing failed.
