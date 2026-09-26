using System.ComponentModel.DataAnnotations;
using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;
using MPFAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("MPFAI currently supports local Development mode only. Configure Entra authentication and production adapters before deployment.");
}

builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<IEngagementRepository, InMemoryEngagementRepository>();
builder.Services.AddSingleton<IFundingGuidelineRepository>(_ =>
    new InMemoryFundingGuidelineRepository(builder.Configuration.GetSection("Guidelines").Get<FundingGuideline[]>()));
builder.Services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
builder.Services.AddSingleton<FundingCalculator>();
builder.Services.AddSingleton<ISowAnalysisProvider, LocalSafeSowAnalysisProvider>();
builder.Services.AddSingleton<IImmutableDocumentStore, InMemoryDocumentStore>();
builder.Services.AddSingleton<IMalwareScanner, UnconfiguredMalwareScanner>();
builder.Services.AddSingleton<IPartnerCenterReportImporter, UnconfiguredPartnerCenterReportImporter>();
builder.Services.AddSingleton<IIntegrationStatusProvider, LocalIntegrationStatusProvider>();
builder.Services.AddSingleton<WorkflowService>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["WebOrigin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1") &&
        (!Guid.TryParse(context.Request.Query["organizationId"], out var organizationId) || organizationId == Guid.Empty))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { title = "organization_id_required", detail = "A non-empty organizationId is required in local mode." });
        return;
    }

    await next();
});
app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", mode = "local-in-memory" }));
app.MapGet("/api/v1/integrations", (IIntegrationStatusProvider statuses) => Results.Ok(statuses.GetStatuses()));
app.MapGet("/api/v1/customers", async (Guid organizationId, ICustomerRepository repository, CancellationToken token) =>
    Results.Ok(await repository.ListAsync(organizationId, token)));
app.MapPost("/api/v1/customers", async (CreateCustomer request, Guid organizationId, ICustomerRepository repository, IAuditRepository audit, CancellationToken token) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Customer name is required."] });
    }

    var customer = new Customer(Guid.NewGuid(), organizationId, request.Name.Trim(), request.Domain?.Trim(), DateTimeOffset.UtcNow);
    await repository.CreateAsync(customer, token);
    await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, "local-user", "customer.created", "customer", customer.Id.ToString(), DateTimeOffset.UtcNow), token);
    return Results.Created($"/api/v1/customers/{customer.Id}", customer);
});
app.MapGet("/api/v1/customers/{customerId:guid}", async (Guid organizationId, Guid customerId, ICustomerRepository repository, CancellationToken token) =>
{
    var customer = await repository.GetAsync(organizationId, customerId, token);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
});
app.MapGet("/api/v1/engagements", async (Guid organizationId, IEngagementRepository repository, CancellationToken token) =>
    Results.Ok(await repository.ListAsync(organizationId, token)));
app.MapPost("/api/v1/engagements", async (CreateEngagement request, Guid organizationId, ICustomerRepository customers, IEngagementRepository engagements, IAuditRepository audit, CancellationToken token) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || await customers.GetAsync(organizationId, request.CustomerId, token) is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["engagement"] = ["A name and customer in this organization are required."] });
    }

    var engagement = new Engagement(Guid.NewGuid(), organizationId, request.CustomerId, request.Name.Trim(), "proposed", DateTimeOffset.UtcNow);
    await engagements.CreateAsync(engagement, token);
    await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, "local-user", "engagement.created", "engagement", engagement.Id.ToString(), DateTimeOffset.UtcNow), token);
    return Results.Created($"/api/v1/engagements/{engagement.Id}", engagement);
});
app.MapGet("/api/v1/engagements/{engagementId:guid}", async (Guid organizationId, Guid engagementId, IEngagementRepository repository, CancellationToken token) =>
{
    var engagement = await repository.GetAsync(organizationId, engagementId, token);
    return engagement is null ? Results.NotFound() : Results.Ok(engagement);
});
app.MapGet("/api/v1/guidelines", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListGuidelines(organizationId)));
app.MapPost("/api/v1/guidelines", async (Guid organizationId, UploadGuidelineRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        var item = await workflows.UploadGuidelineAsync(organizationId, request.FileName,
            Convert.FromBase64String(request.Base64Content), request.ProgramId, request.Version, request.Rate,
            request.Cap, request.Currency, request.EffectiveFrom, request.EffectiveTo, request.Uploader, token);
        return Results.Created($"/api/v1/guidelines/{item.Id}", item);
    }
    catch (FormatException)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["base64Content"] = ["Guideline content must be valid base64."] });
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/guidelines/{guidelineId:guid}/approve", async (Guid organizationId, Guid guidelineId, ApproveGuidelineRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.ApproveGuidelineAsync(organizationId, guidelineId, request.Approver, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/funding/calculate", async (Guid organizationId, FundingCalculationRequest request, FundingCalculator calculator, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await calculator.CalculateAsync(request with { OrganizationId = organizationId }, token));
    }
    catch (FundingCalculationException exception)
    {
        return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: exception.Code, detail: exception.Message);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["eligibleAmount"] = [exception.Message] });
    }
});
app.MapPost("/api/v1/engagements/{engagementId:guid}/sow-analysis", async (Guid organizationId, Guid engagementId, SowAnalysisRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Accepted(value: await workflows.StartSowJobAsync(organizationId, engagementId, request.Text, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/sow-analysis", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListJobs(organizationId)));
app.MapPost("/api/v1/engagements/{engagementId:guid}/tasks", async (Guid organizationId, Guid engagementId, CreateTaskRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        var item = await workflows.CreateTaskAsync(organizationId, engagementId, request.Title, request.Owner, request.DueDate, token);
        return Results.Created($"/api/v1/tasks/{item.Id}", item);
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/tasks", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListTasks(organizationId)));
app.MapPost("/api/v1/tasks/{taskId:guid}/state", async (Guid organizationId, Guid taskId, ChangeTaskStateRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.ChangeTaskStateAsync(organizationId, taskId, request.State, request.Actor, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/engagements/{engagementId:guid}/attribution", async (Guid organizationId, Guid engagementId, CreateAttributionRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        var item = await workflows.CreateAttributionAsync(new AttributionRecord(Guid.Empty, organizationId, engagementId,
            request.PartnerId, request.CustomerTenantId, request.AzureScope, request.DeliveryIdentity, request.AssociationType,
            AttributionState.SetupRequested), token);
        return Results.Created($"/api/v1/attribution/{item.Id}", item);
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/attribution", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListAttribution(organizationId)));
app.MapPost("/api/v1/attribution/{attributionId:guid}/reported-complete", async (Guid organizationId, Guid attributionId, ReportAttributionRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.ReportAttributionAsync(organizationId, attributionId, request.Reporter, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/attribution/{attributionId:guid}/verify", async (Guid organizationId, Guid attributionId, VerifyAttributionRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.VerifyAttributionAsync(organizationId, attributionId, request.Verifier, request.Method, request.EvidenceReference, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/engagements/{engagementId:guid}/evidence", async (Guid organizationId, Guid engagementId, SubmitEvidenceRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        var item = await workflows.SubmitEvidenceAsync(organizationId, engagementId, request.Requirement, request.FileName, request.SubmittedBy, token);
        return Results.Created($"/api/v1/evidence/{item.Id}", item);
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/evidence", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListEvidence(organizationId)));
app.MapPost("/api/v1/evidence/{evidenceId:guid}/review", async (Guid organizationId, Guid evidenceId, ReviewEvidenceRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.ReviewEvidenceAsync(organizationId, evidenceId, request.Reviewer, request.Accepted, request.Comment, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapPost("/api/v1/engagements/{engagementId:guid}/claims", async (Guid organizationId, Guid engagementId, CreateClaimRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        var item = await workflows.CreateClaimAsync(organizationId, engagementId, request.ExternalClaimId, request.Currency, request.RequestedAmount, token);
        return Results.Created($"/api/v1/claims/{item.Id}", item);
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/claims", (Guid organizationId, WorkflowService workflows) =>
    Results.Ok(workflows.ListClaims(organizationId)));
app.MapPost("/api/v1/claims/{claimId:guid}/state", async (Guid organizationId, Guid claimId, ChangeClaimStateRequest request, WorkflowService workflows, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await workflows.ChangeClaimStateAsync(organizationId, claimId, request.State, token));
    }
    catch (WorkflowException exception)
    {
        return WorkflowProblem(exception);
    }
});
app.MapGet("/api/v1/audit", async (Guid organizationId, IAuditRepository audit, CancellationToken token) =>
    Results.Ok(await audit.ListAsync(organizationId, token)));

app.Run();

static IResult WorkflowProblem(WorkflowException exception)
{
    var status = exception.Code.EndsWith("_not_found", StringComparison.Ordinal) ? StatusCodes.Status404NotFound : StatusCodes.Status422UnprocessableEntity;
    return Results.Problem(statusCode: status, title: exception.Code, detail: exception.Message);
}

public sealed record CreateCustomer([Required] string Name, string? Domain);
public sealed record CreateEngagement([Required] Guid CustomerId, [Required] string Name);
public sealed record UploadGuidelineRequest(string FileName, string Base64Content, string ProgramId, string Version, decimal Rate, decimal Cap, string Currency, DateOnly EffectiveFrom, DateOnly EffectiveTo, string Uploader);
public sealed record ApproveGuidelineRequest(string Approver);
public sealed record SowAnalysisRequest(string Text);
public sealed record CreateTaskRequest(string Title, string Owner, DateOnly? DueDate);
public sealed record ChangeTaskStateRequest(string State, string Actor);
public sealed record CreateAttributionRequest(string PartnerId, string CustomerTenantId, string AzureScope, string DeliveryIdentity, string AssociationType);
public sealed record ReportAttributionRequest(string Reporter);
public sealed record VerifyAttributionRequest(string Verifier, string Method, string EvidenceReference);
public sealed record SubmitEvidenceRequest(string Requirement, string FileName, string SubmittedBy);
public sealed record ReviewEvidenceRequest(string Reviewer, bool Accepted, string Comment);
public sealed record CreateClaimRequest(string ExternalClaimId, string Currency, decimal RequestedAmount);
public sealed record ChangeClaimStateRequest(ClaimState State);

public partial class Program;
