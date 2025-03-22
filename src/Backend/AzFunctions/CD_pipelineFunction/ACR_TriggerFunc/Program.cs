using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
