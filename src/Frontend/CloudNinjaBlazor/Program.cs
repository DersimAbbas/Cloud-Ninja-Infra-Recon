using CloudNinjaBlazor.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient(
    "CloudNinjaBlazor.ServerAPI",
    client =>
    {
        client.BaseAddress = new Uri("Http://backend:8080/");
    }
)
.ConfigureHttpClient(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
