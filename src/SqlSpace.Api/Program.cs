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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

