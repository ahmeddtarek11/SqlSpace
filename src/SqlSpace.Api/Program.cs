using Scalar.AspNetCore;
using SqlSpace.Api;
using SqlSpace.Application;
using SqlSpace.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// builder.Host.UseSerilog((context, loggerConfig) =>
// {
//     loggerConfig.ReadFrom.Configuration(context.Configuration);
// });

builder.Services.AddApplication().AddInfrastructure(builder.Configuration).AddApi();
// 1. Add this before builder.Build()
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174") // Add your frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
   app.MapScalarApiReference(options =>
   {
       options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
   });
}

app.UseHttpsRedirection();
// CORS should run before auth and before endpoints are mapped.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
