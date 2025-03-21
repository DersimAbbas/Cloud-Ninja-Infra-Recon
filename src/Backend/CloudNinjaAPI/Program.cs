var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
    "Cloud ninja initalized.", "Create your docker compose ", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
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

