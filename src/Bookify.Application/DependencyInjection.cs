

using Bookify.Application.Abstractions.Behaviors;
using Bookify.Domain.Bookings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bookify.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection service)
        {

            //AddMediatR
            service.AddMediatR(configuration =>


            {
                configuration
                    .RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);

                //Register Behaviors in mediatR
                configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));

                //Register Validation Behavior in MediaR

                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
                //Register QueryCaching Behavior in MediaR

                configuration.AddOpenBehavior(typeof(QueryCachingBehavior<,>));


            });



            //register validator | need to instal FluentValidation Dependency version from nuget

            service.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);



            //register Domain Service

            service.AddTransient<PricingService>();




            return service;
        }
    }
}
