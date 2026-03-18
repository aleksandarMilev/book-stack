using BookStack.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;

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
    .AddDatabase(
        builder.Configuration,
        builder.Environment)
    .AddCustomCorsPolicy(
        builder.Configuration,
        builder.Environment)
    .AddJwtAuthentication(
        builder.Configuration,
        builder.Environment);

builder
    .Host
    .AddLogging(builder.Environment);

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

app.UseSerilogRequestLogging();

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

    await app.UseDevDb(cancellationToken);

}

app.Run();
