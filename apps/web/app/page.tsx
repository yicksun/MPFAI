"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

const api = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
const defaultOrganization = process.env.NEXT_PUBLIC_ORGANIZATION_ID ?? "8182a48a-ad19-41a8-b7e6-dc13d7bb20c7";
const sections = ["Overview", "Customers & deals", "Funding", "Delivery", "Attribution", "Evidence & claims"] as const;
type Section = (typeof sections)[number];
type RecordData = Record<string, unknown> & { id?: string; name?: string; title?: string; state?: string; status?: string };
type DashboardData = Record<string, RecordData[]>;

async function request<T>(organizationId: string, path: string, options?: RequestInit): Promise<T> {
  const separator = path.includes("?") ? "&" : "?";
  const response = await fetch(`${api}${path}${separator}organizationId=${encodeURIComponent(organizationId)}`, {
    ...options,
    headers: { "Content-Type": "application/json", ...options?.headers },
    cache: "no-store",
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
    throw new Error(problem?.detail ?? problem?.title ?? `API request failed (${response.status})`);
  }
  return response.status === 204 ? undefined as T : await response.json() as T;
}

function send(method: string, body: object): RequestInit {
  return { method, body: JSON.stringify(body) };
}

function encodeBase64(value: string): string {
  const bytes = new TextEncoder().encode(value);
  const chunks: string[] = [];
  for (let offset = 0; offset < bytes.length; offset += 0x8000) {
    chunks.push(String.fromCharCode(...bytes.subarray(offset, offset + 0x8000)));
  }
  return btoa(chunks.join(""));
}

export default function Home() {
  const [section, setSection] = useState<Section>("Overview");
  const [organizationId, setOrganizationId] = useState(defaultOrganization);
  const [data, setData] = useState<DashboardData>({});
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [lastResult, setLastResult] = useState("");
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async () => {
    setError("");
    try {
      const paths = ["customers", "engagements", "guidelines", "sow-analysis", "tasks", "attribution", "evidence", "claims"];
      const entries = await Promise.all(paths.map(async (path) => [
        path,
        await request<RecordData[]>(organizationId, `/api/v1/${path}`),
      ] as const));
      setData(Object.fromEntries(entries));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not load workspace data.");
    }
  }, [organizationId]);

  useEffect(() => { void refresh(); }, [refresh]);

  const submit = async (event: FormEvent<HTMLFormElement>, path: string, body: object, success: string) => {
    event.preventDefault();
    const formElement = event.currentTarget;
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const result = await request<unknown>(organizationId, path, send("POST", body));
      setLastResult(JSON.stringify(result, null, 2));
      setMessage(success);
      formElement.reset();
      await refresh();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "The action failed.");
    } finally {
      setBusy(false);
    }
  };

  const values = (key: string) => data[key] ?? [];
  const metric = (key: string) => values(key).length;
  const currentEngagement = values("engagements")[0]?.id as string | undefined;
  const form = (title: string, fields: Array<{ name: string; label: string; type?: string; required?: boolean; defaultValue?: string; multiline?: boolean }>, path: string | ((values: Record<string, string>) => string), build: (values: Record<string, string>) => object, success: string) => (
    <form className="card form-card" onSubmit={(event) => {
      const formData = new FormData(event.currentTarget);
      const entries = Object.fromEntries([...formData.entries()].map(([key, value]) => [key, String(value)]));
      const target = typeof path === "function" ? path(entries) : path;
      void submit(event, target, build(entries), success);
    }}>
      <h3>{title}</h3>
      {fields.map((field) => <label key={field.name}>{field.label}
        {field.multiline
          ? <textarea name={field.name} required={field.required ?? true} defaultValue={field.defaultValue} rows={4} />
          : <input name={field.name} type={field.type ?? "text"} step={field.type === "number" ? "any" : undefined} required={field.required ?? true} defaultValue={field.defaultValue} />}
      </label>)}
      <button disabled={busy} type="submit">{busy ? "Saving…" : "Save"}</button>
    </form>
  );
  const list = (key: string) => (
    <div className="record-list">{values(key).length === 0 ? <p className="muted">Nothing here yet.</p> : values(key).map((item, index) => (
      <article className="record" key={String(item.id ?? index)}>
        <div><strong>{String(item.name ?? item.title ?? item.programId ?? item.externalClaimId ?? item.requirement ?? item.partnerId ?? item.id ?? "Record")}</strong>
          <span className="muted">{String(item.state ?? item.status ?? item.version ?? "")}</span></div>
        <small>{String(item.id ?? "")}</small>
      </article>
    ))}</div>
  );
  const countCard = (label: string, value: number, hint: string) => <article className="metric card"><span className="muted">{label}</span><strong>{value}</strong><small>{hint}</small></article>;

  return (
    <main className="shell">
      <aside className="sidebar">
        <a className="brand" href="#"><span className="brand-mark">M</span><span>MPFAI<small>PARTNER WORKSPACE</small></span></a>
        <nav aria-label="Main navigation">{sections.map((item) => <button key={item} className={section === item ? "nav-item active" : "nav-item"} onClick={() => setSection(item)}>{item}</button>)}</nav>
        <div className="sidebar-foot"><span className="status-dot" /> Local mode<br /><small>External services are not connected</small></div>
      </aside>

      <section className="content">
        <header className="topbar">
          <div><span className="eyebrow">MICROSOFT PARTNER FUNDING & ATTRIBUTION</span><h1>{section}</h1></div>
          <label className="org-field">Organization ID<input aria-label="Organization ID" value={organizationId} onChange={(event) => setOrganizationId(event.target.value)} /></label>
        </header>
        {error && <div className="notice error" role="alert">{error}</div>}
        {message && <div className="notice success" role="status">{message}</div>}
        {lastResult && <details className="result card"><summary>Latest API response</summary><pre>{lastResult}</pre></details>}

        {section === "Overview" && <>
          <div className="welcome card"><div><span className="eyebrow">ENGAGEMENT OPERATIONS</span><h2>Move qualified work forward.</h2><p>Funding stays indicative until an authorized reviewer approves it. Attribution and evidence are tracked independently.</p></div><button className="quiet" onClick={() => void refresh()}>Refresh workspace</button></div>
          <div className="metrics">{countCard("Customers", metric("customers"), "Organization-scoped")}{countCard("Engagements", metric("engagements"), "Across lifecycle stages")}{countCard("Open tasks", values("tasks").filter((item) => item.state !== "done").length, "Owned delivery work")}{countCard("Attribution records", metric("attribution"), "Verification states")}</div>
          <div className="columns"><section className="card"><div className="section-heading"><h3>Active engagements</h3><button className="text-button" onClick={() => setSection("Customers & deals")}>View all</button></div>{list("engagements")}</section><section className="card"><div className="section-heading"><h3>Needs attention</h3><button className="text-button" onClick={() => setSection("Delivery")}>Open delivery</button></div>{values("tasks").filter((item) => item.state !== "done").slice(0, 5).map((item, index) => <p className="attention" key={String(item.id ?? index)}><span className="warning-dot" />{String(item.title)} <small>{String(item.owner)}</small></p>)}{values("tasks").length === 0 && <p className="muted">No delivery tasks recorded.</p>}</section></div>
        </>}

        {section === "Customers & deals" && <div className="columns"><section>{form("Add customer", [{ name: "name", label: "Customer name" }, { name: "domain", label: "Business domain", required: false }], "/api/v1/customers", (v) => ({ name: v.name, domain: v.domain || null }), "Customer created.")}{form("Create engagement", [{ name: "customerId", label: "Customer ID" }, { name: "name", label: "Engagement name" }], "/api/v1/engagements", (v) => ({ customerId: v.customerId, name: v.name }), "Engagement created.")}</section><section className="card"><h3>Customers</h3>{list("customers")}<h3 className="spaced">Engagements</h3>{list("engagements")}</section></div>}

        {section === "Funding" && <>
          <div className="notice">Local mode accepts UTF-8 text guideline files only. Approval requires a different user from the uploader. Funding is an indicative calculation, not a Microsoft decision.</div>
          <div className="columns"><section>
            {form("Register guideline version", [{ name: "fileName", label: "Text filename", defaultValue: "funding-guide.txt" }, { name: "text", label: "Source text", multiline: true }, { name: "programId", label: "Program ID" }, { name: "version", label: "Version" }, { name: "rate", label: "Rate (0–1)", type: "number" }, { name: "cap", label: "Maximum amount", type: "number" }, { name: "currency", label: "Currency", defaultValue: "USD" }, { name: "effectiveFrom", label: "Effective from", type: "date" }, { name: "effectiveTo", label: "Effective to", type: "date" }, { name: "uploader", label: "Uploader" }], "/api/v1/guidelines", (v) => ({ fileName: v.fileName, base64Content: encodeBase64(v.text), programId: v.programId, version: v.version, rate: Number(v.rate), cap: Number(v.cap), currency: v.currency, effectiveFrom: v.effectiveFrom, effectiveTo: v.effectiveTo, uploader: v.uploader }), "Guideline version registered for review.")}
            {form("Approve guideline", [{ name: "guidelineId", label: "Guideline ID" }, { name: "approver", label: "Approver" }], (v) => `/api/v1/guidelines/${v.guidelineId}/approve`, (v) => ({ approver: v.approver }), "Guideline approved.")}
            {form("Calculate indicative funding", [{ name: "guidelineId", label: "Approved guideline ID" }, { name: "eligibleAmount", label: "Eligible amount", type: "number" }, { name: "asOf", label: "Calculation date", type: "date" }, { name: "currency", label: "Currency", defaultValue: "USD" }], "/api/v1/funding/calculate", (v) => ({ guidelineId: v.guidelineId, eligibleAmount: Number(v.eligibleAmount), asOf: v.asOf, currency: v.currency }), "Calculation complete; review trace and citations in the API response.")}
          </section><section className="card"><h3>Guideline library</h3>{list("guidelines")}</section></div>
        </>}

        {section === "Delivery" && <div className="columns"><section>
          {form("Create SOW review job", [{ name: "engagementId", label: "Engagement ID", defaultValue: currentEngagement }, { name: "text", label: "SOW text", multiline: true }], (v) => `/api/v1/engagements/${v.engagementId}/sow-analysis`, (v) => ({ text: v.text }), "Local text review created.")}
          {form("Assign a task", [{ name: "engagementId", label: "Engagement ID", defaultValue: currentEngagement }, { name: "title", label: "Task" }, { name: "owner", label: "Owner" }, { name: "dueDate", label: "Due date", type: "date", required: false }], (v) => `/api/v1/engagements/${v.engagementId}/tasks`, (v) => ({ title: v.title, owner: v.owner, dueDate: v.dueDate || null }), "Task created.")}
          {form("Update task state", [{ name: "taskId", label: "Task ID" }, { name: "state", label: "State (in_progress, blocked, done, open)" }, { name: "actor", label: "Actor" }], (v) => `/api/v1/tasks/${v.taskId}/state`, (v) => ({ state: v.state, actor: v.actor }), "Task state updated.")}
        </section><section className="card"><h3>Delivery tasks</h3>{list("tasks")}<h3 className="spaced">SOW review jobs</h3>{list("sow-analysis")}</section></div>}

        {section === "Attribution" && <div className="columns"><section>
          {form("Record intended association", [{ name: "engagementId", label: "Engagement ID", defaultValue: currentEngagement }, { name: "partnerId", label: "Partner ID" }, { name: "customerTenantId", label: "Customer tenant ID" }, { name: "azureScope", label: "Azure scope" }, { name: "deliveryIdentity", label: "Delivery identity" }, { name: "associationType", label: "Association type", defaultValue: "PAL" }], (v) => `/api/v1/engagements/${v.engagementId}/attribution`, (v) => ({ partnerId: v.partnerId, customerTenantId: v.customerTenantId, azureScope: v.azureScope, deliveryIdentity: v.deliveryIdentity, associationType: v.associationType }), "Attribution setup recorded.")}
          {form("Report setup complete", [{ name: "attributionId", label: "Attribution record ID" }, { name: "reporter", label: "Reporter" }], (v) => `/api/v1/attribution/${v.attributionId}/reported-complete`, (v) => ({ reporter: v.reporter }), "Setup report recorded; verification remains pending.")}
          {form("Independently verify", [{ name: "attributionId", label: "Attribution record ID" }, { name: "verifier", label: "Verifier" }, { name: "method", label: "Verification method" }, { name: "evidenceReference", label: "Evidence reference" }], (v) => `/api/v1/attribution/${v.attributionId}/verify`, (v) => ({ verifier: v.verifier, method: v.method, evidenceReference: v.evidenceReference }), "Attribution verified with evidence.")}
        </section><section className="card"><h3>Attribution state</h3>{list("attribution")}<p className="muted">Self-reported completion is not verification. An independent reviewer and evidence reference are required.</p></section></div>}

        {section === "Evidence & claims" && <div className="columns"><section>
          <div className="notice">Evidence file bytes are not stored in local mode. This screen tracks evidence metadata and review state only.</div>
          {form("Submit evidence metadata", [{ name: "engagementId", label: "Engagement ID", defaultValue: currentEngagement }, { name: "requirement", label: "Requirement" }, { name: "fileName", label: "Filename reference" }, { name: "submittedBy", label: "Submitted by" }], (v) => `/api/v1/engagements/${v.engagementId}/evidence`, (v) => ({ requirement: v.requirement, fileName: v.fileName, submittedBy: v.submittedBy }), "Evidence submitted for review.")}
          {form("Review evidence", [{ name: "evidenceId", label: "Evidence ID" }, { name: "reviewer", label: "Reviewer" }, { name: "accepted", label: "Accept? (true/false)" }, { name: "comment", label: "Review comment" }], (v) => `/api/v1/evidence/${v.evidenceId}/review`, (v) => ({ reviewer: v.reviewer, accepted: v.accepted.toLowerCase() === "true", comment: v.comment }), "Evidence review recorded.")}
          {form("Create claim record", [{ name: "engagementId", label: "Engagement ID", defaultValue: currentEngagement }, { name: "externalClaimId", label: "External claim ID (if submitted)" }, { name: "currency", label: "Currency", defaultValue: "USD" }, { name: "requestedAmount", label: "Requested amount", type: "number" }], (v) => `/api/v1/engagements/${v.engagementId}/claims`, (v) => ({ externalClaimId: v.externalClaimId, currency: v.currency, requestedAmount: Number(v.requestedAmount) }), "Claim record created.")}
          {form("Advance claim state", [{ name: "claimId", label: "Claim ID" }, { name: "state", label: "Next state (for example Submitted, Approved, Paid)" }], (v) => `/api/v1/claims/${v.claimId}/state`, (v) => ({ state: v.state }), "Claim state updated.")}
        </section><section className="card"><h3>Evidence</h3>{list("evidence")}<h3 className="spaced">Claims</h3>{list("claims")}</section></div>}

        <footer>Local in-memory data • Decisions require human review • No external Microsoft services are connected</footer>
      </section>
    </main>
  );
}
