using System.ComponentModel.DataAnnotations;
using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;
using MPFAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<IEngagementRepository, InMemoryEngagementRepository>();
builder.Services.AddSingleton<IFundingGuidelineRepository>(_ =>
    new InMemoryFundingGuidelineRepository(builder.Configuration.GetSection("Guidelines").Get<FundingGuideline[]>()));
builder.Services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
builder.Services.AddSingleton<FundingCalculator>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", mode = "local-in-memory" }));
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
app.MapGet("/api/v1/guidelines", async (IFundingGuidelineRepository repository, CancellationToken token) =>
    Results.Ok(await repository.ListAsync(token)));
app.MapPost("/api/v1/funding/calculate", async (FundingCalculationRequest request, FundingCalculator calculator, CancellationToken token) =>
{
    try
    {
        return Results.Ok(await calculator.CalculateAsync(request, token));
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

app.Run();

public sealed record CreateCustomer([Required] string Name, string? Domain);
public sealed record CreateEngagement([Required] Guid CustomerId, [Required] string Name);

public partial class Program;
