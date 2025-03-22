var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// And in the Configure method:
var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var init = new[]
{
    "Cloud ninja initalized.", "Create your pipelines", "write your infrastructure"
};

var summaries = new[]
{
    "Cloud ninja initalized.", "Create your docker compose ", "we are at the end of this infrastructure", "help me", "dead inside", "help me D:", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("api/init", () =>
{
    var load = summaries[new Random().Next(summaries.Length)];
    if (load == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(load);
});

app.Run();

