using System.Linq;
using Microsoft.Extensions.Hosting;
using RapiAgent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRapiAgentServices();

if (args.Contains("--service"))
    builder.Host.UseWindowsService();

var app = builder.Build();

app.UseRapiAgentRpc();
app.MapControllers();

await app.RunAsync();
