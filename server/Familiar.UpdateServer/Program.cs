using Familiar.UpdateServer.Endpoints;
using Familiar.UpdateServer.Options;
using Familiar.UpdateServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<UpdateServerOptions>(
    builder.Configuration.GetSection(UpdateServerOptions.SectionName));

// Services
builder.Services.AddSingleton<ISigningService, SigningService>();
builder.Services.AddSingleton<IReleaseService, ReleaseService>();

var app = builder.Build();

// Initialize signing keys if they don't exist
var signingService = app.Services.GetRequiredService<ISigningService>();
try
{
    signingService.GetPublicKey();
}
catch (InvalidOperationException)
{
    app.Logger.LogInformation("No signing keys found, generating new key pair...");
    signingService.GenerateKeyPair();
}

// Endpoints
app.MapUpdateEndpoints();
app.MapAdminEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
