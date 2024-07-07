using Asp.Versioning;
using Asp.Versioning.Builder;
using Bookify.Api.Controllers.Bookings;
using Bookify.Api.Extensions;
using Bookify.Api.OpenApi;
using Bookify.Application;
using Bookify.Application.Abstractions.Data;
using Bookify.Infrastructure;
using Dapper;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// Serilog
builder.Host.UseSerilog((context, configuration) =>
{

    configuration.ReadFrom.Configuration(context.Configuration);
});


//--------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//--------------------------------------------

// Add dependencies from Infra and Application layer.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

//--------------------------------------------

// AddHealth Check wth Custom Health Check
//builder.Services.AddHealthChecks().AddCheck<CustomSqlHealthCheck>("custom-sql");

//--------------------------------------------

// for Solving Swagger multi version Api

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();






var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // app.UseSwaggerUI(); Update for solving swagger issue for multi Api Version
    app.UseSwaggerUI(options =>
 {
     var descriptions = app.DescribeApiVersions();

     foreach (var description in descriptions)
     {
         var url = $"/swagger/{description.GroupName}/swagger.json";
         var name = description.GroupName.ToUpperInvariant();
         options.SwaggerEndpoint(url, name);
     }
 });

    //for developing purpose
    app.ApplyMigrations();

    // run once to populate database then comment it out
    //app.SeedData();
}
//----------------------

app.UseHttpsRedirection();

// Serilog Middleware

app.UseRequestContextLogging(); // our custom middleware for serilog

app.UseSerilogRequestLogging();

//----------------------


//using exception middleware
app.UseCustomExceptionHandler();
//----------------------


//Add auth middlewares
app.UseAuthentication();
app.UseAuthorization();
//----------------------



app.MapControllers();
//----------------------

//MinimalApi ApiVersioning Better way

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var routeGroupBuilder= app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(apiVersionSet);// this is a prefix for all routes
routeGroupBuilder.MapBookingEndpoints();



//app.MapBookingEndpoints(); if we set the versioning in the bookingEndpoint

//----------------------
//AddHealth Check
//app.MapHealthChecks("health"); before AspNetCore.HealthChecks.UI.Client
app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
//----------------------



app.Run();

// implementing simple health check for sql database
//public class CustomSqlHealthCheck(
//    ISqlConnectionFactory sqlConnectionFactory // allow us to query

//    ):IHealthCheck
//{
//    // Custom Health Check | not recommended to write custom health check
//    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
//        CancellationToken cancellationToken =default)
//    {
//        try
//        {
//            using var connection = sqlConnectionFactory.CreateConnection();

//            // ex query

//            await connection.ExecuteScalarAsync("SELECT 1");

//            return HealthCheckResult.Healthy();
//        }
//        catch (Exception e)
//        {
//           return HealthCheckResult.Unhealthy(exception:e);
//        }



//    }
//}
