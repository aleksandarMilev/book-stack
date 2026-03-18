using BookStack.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenApi()
    .AddSwagger()
    .AddServices()
    .AddMemoryCache()
    .AddProblemDetails()
    .AddApiControllers()
    .AddCustomHealthChecks()
    .AddHttpContextAccessor()
    .AddCustomRequestTimeouts()
    .AddAppSettings(builder.Configuration)
    .AddIdentity(builder.Environment)
    .AddCustomRateLimiter(builder.Environment)
    .AddCustomHttpLogging(builder.Environment)
    .AddDatabase(
        builder.Configuration,
        builder.Environment)
    .AddCustomCorsPolicy(
        builder.Configuration,
        builder.Environment)
    .AddJwtAuthentication(
        builder.Configuration,
        builder.Environment);

var app = builder.Build();

var envIsDev = app.Environment.IsDevelopment();
var envIsNotTesting = !app.Environment.IsEnvironment("Testing");

if (envIsDev)
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseCustomForwardedHeaders();
    app.UseHttpsRedirection();
}

app.UseHttpLogging();
app.UseStaticFiles();
app.UseRouting();

if (envIsNotTesting)
{
    app.UseAllowedCors();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();
app.UseAppEndpoints();

if (envIsDev)
{
    var cancellationToken = app
        .Lifetime
        .ApplicationStopping;

    await app.UseMigrations(cancellationToken);
    await app.UseBuiltInUser(cancellationToken);
    await app.UseDevBookData(cancellationToken);
    await app.UseDevAdminRole();
}

app.Run();
