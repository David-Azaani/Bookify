using Asp.Versioning;
using Asp.Versioning.Builder;
using Bookify.Application.Bookings.GetBooking;
using Bookify.Application.Bookings.ReserveBooking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.Api.Controllers.Bookings
{

    public static class BookingsEndPoints

    {


        // to make Minimal Api

        // The Base Center
        public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder builder)

        {

            #region Old
            //we move this to program.cs because is not really scalable a better way in the program cs

            //ApiVersionSet apiVersionSet = builder.NewApiVersionSet()
            //    .HasApiVersion(new ApiVersion(1))
            //    .ReportApiVersions()
            //    .Build();



            //builder.MapGet("api/v{version:apiVersion}/{id}",GetBooking);
            // to add authorization
            //   builder.MapGet("api/v{version:apiVersion}/{id}",GetBooking).RequireAuthorization("bookings:read");
            // or we can write extension method for that like withAuthorization 



            //builder.MapGet("api/v{version:apiVersion}/bookings/{id}", GetBooking)
            //    .WithName(nameof(GetBooking))
            //    .WithApiVersionSet(apiVersionSet);

            //builder.MapPost("api/v{version:apiVersion}/bookings", ReserveBooking)
            //    .RequireAuthorization()
            //    .WithApiVersionSet(apiVersionSet); 
            #endregion
            builder.MapGet("bookings/{id}", GetBooking)
                .WithName(nameof(GetBooking))
               ;


            builder.MapPost("bookings", ReserveBooking)
                .RequireAuthorization()
                ;




            return builder;
        }


        public static async Task<IResult> GetBooking(Guid id, ISender _sender, CancellationToken cancellationToken)
        {
            var query = new GetBookingQuery(id);

            var result = await _sender.Send(query, cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        }

        public static async Task<IResult> ReserveBooking(
    ReserveBookingRequest request, ISender _sender,
    CancellationToken cancellationToken)
        {
            var command = new ReserveBookingCommand(
                request.ApartmentId,
                request.UserId,
                request.StartDate,
                request.EndDate
                );

            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }
            // this will be in header
            return Results.CreatedAtRoute(nameof(GetBooking), new { id = result.Value }, result.Value);
        }



    }
}
