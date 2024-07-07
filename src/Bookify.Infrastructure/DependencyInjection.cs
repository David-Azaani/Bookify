using Asp.Versioning;
using Bookify.Application.Abstractions.Authentication;
using Bookify.Application.Abstractions.Caching;
using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Abstractions.Data;
using Bookify.Application.Abstractions.Email;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Apartments;
using Bookify.Domain.Bookings;
using Bookify.Domain.Reviews;
using Bookify.Domain.Users;
using Bookify.Infrastructure.Authentication;
using Bookify.Infrastructure.Authorization;
using Bookify.Infrastructure.Caching;
using Bookify.Infrastructure.Clock;
using Bookify.Infrastructure.Data;
using Bookify.Infrastructure.Email;
using Bookify.Infrastructure.Outbox;
using Bookify.Infrastructure.Repositories;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using AuthenticationOptions = Bookify.Infrastructure.Authentication.AuthenticationOptions;
using AuthenticationService = Bookify.Infrastructure.Authentication.AuthenticationService;
using IAuthenticationService = Bookify.Application.Abstractions.Authentication.IAuthenticationService;


namespace Bookify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IEmailService, EmailService>();


        AddPersistence(services, configuration);


        AddAuthentication(services, configuration);


        AddAuthorization(services);


        AddCaching(services, configuration);


        AddHealthCheck(services, configuration);

        AddApiVersioning(services);

        AddBackgroundJob(services , configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        //connectionString
        var connectionString = configuration.GetConnectionString("Database") ??
                              throw new ArgumentNullException(nameof(configuration));
        // registering Ef
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        //registering repos & UOW

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());


        // SQl Connection factory and DateOnly

        services.AddSingleton<ISqlConnectionFactory>(_ =>
            new SqlConnectionFactory(connectionString));

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }



    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        //   .AddJwtBearer(o=>o.);
        // get value from app setting and map to the AuthenticationOptions
        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));

        services.ConfigureOptions<JwtBearerOptionsSetup>();

        // config Keycloak
        // mapping app setting information of keycloak to the KeycloakOptions file
        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));

        services.AddTransient<AdminAuthorizationDelegatingHandler>();

        //For register

        services.AddHttpClient<IAuthenticationService, AuthenticationService>((serviceProvider, httpClient) =>
            {
                var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

                httpClient.BaseAddress = new Uri(keycloakOptions.AdminUrl);
            })
            .AddHttpMessageHandler<AdminAuthorizationDelegatingHandler>();

        //ForLogin
        // typed http client implementation : which means we inject httpClient instance inside the jwtService

        services.AddHttpClient<IJwtService, JwtService>((serviceProvider, httpClient) =>
        {
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

            httpClient.BaseAddress = new Uri(keycloakOptions.TokenUrl);
        });


        services.AddHttpContextAccessor();

        services.AddScoped<IUserContext, UserContext>();
    }





    private static void AddAuthorization(IServiceCollection service)
    {
        service.AddScoped<AuthorizationService>();

        service.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

        service.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        service.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    }


    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {

        // get con string from app setting
        var connectionString = configuration.GetConnectionString("Cache") ??
                               throw new ArgumentNullException(nameof(configuration));

        // register redis and pass the con string
        services.AddStackExchangeRedisCache(option => option.Configuration = connectionString);

        // register interface

        services.AddSingleton<ICacheService, CacheService>();

    }



    private static void AddHealthCheck(IServiceCollection service, IConfiguration configuration)
    {
        service.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!)
            .AddRedis(configuration.GetConnectionString("Cache")!)
            .AddUrlGroup(new Uri(configuration["Keycloak:BaseUrl"]!), HttpMethod.Get, "keycloack");
    }


    private static void AddApiVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true; // hover
                options.ApiVersionReader =
                    new UrlSegmentApiVersionReader(); // the kind of api versioning which we want to use
                // options.ApiVersionReader = new HeaderApiVersionReader("X-Version-Id"); //Ex header api versioning 
                // options.ApiVersionReader = new QueryStringApiVersionReader();Ex Query Api Versioning

                // if we want to combine of these ways:

                //options.ApiVersionReader = ApiVersionReader.Combine(

                //    new HeaderApiVersionReader(),
                //    new QueryStringApiVersionReader()
                //    );
            })
            .AddMvc()
            //Adding after installing Asp.Versioning.Mvc.ApiExplorer
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V"; // for [Route("api/v{V}/users")]
                options.SubstituteApiVersionInUrl = true;
            });


    }




    private static void AddBackgroundJob(IServiceCollection services, IConfiguration configuration)

    {
        // Mapping from appSetting to the class
         services.Configure<OutboxOptions>(configuration.GetSection("OutBox"));


         services.AddQuartz();
        // wait to jobs complete when the app is going to shutdown
         services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

         services.ConfigureOptions<ProcessOutboxMessagesJobSetup>();



    }


}