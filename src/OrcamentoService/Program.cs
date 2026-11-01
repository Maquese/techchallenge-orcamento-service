using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using IOC;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddAppServices(builder.Configuration);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();


// app.UseMiddleware<CorrelationMiddleware>();
// app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});


// Configure the HTTP request pipeline.

    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Minha API v1");
    });


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
