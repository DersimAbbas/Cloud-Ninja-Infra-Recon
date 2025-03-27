using CloudNinjaBlazor.Components;
using CloudNinjaBlazor.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseAddress = config["ApiBaseUrl"]; // Read from appsettings.json
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
}

builder
    .Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = true);
builder .Services.AddScoped<NinjaAPI>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
