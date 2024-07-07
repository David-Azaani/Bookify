

using System.Data;
using Bookify.Application.Abstractions.Authentication;
using Bookify.Application.Abstractions.Data;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Bookings;
using Dapper;

namespace Bookify.Application.Bookings.GetBooking;

internal sealed class GetBookingQueryHandler : IQueryHandler<GetBookingQuery, BookingResponse>
{


    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IUserContext _userContext;

    public GetBookingQueryHandler(ISqlConnectionFactory sqlConnectionFactory ,  IUserContext userContext)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _userContext = userContext;
    }


    // we had a security flow and issue, which any authenticated user could see all booking without checking the owner identity of the booking
    // we dos ome changes in IUserContext te get the UserId and use it here to solve the issue
    public async Task<Result<BookingResponse>> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        // we use using to handle dispose after finishing
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = """
       SELECT
           id AS Id,
           apartment_id AS ApartmentId,
           user_id AS UserId,
           status AS Status,
           price_for_period_amount AS PriceAmount,
           price_for_period_currency AS PriceCurrency,
           cleaning_fee_amount AS CleaningFeeAmount,
           cleaning_fee_currency AS CleaningFeeCurrency,
           amenities_up_charge_amount AS AmenitiesUpChargeAmount,
           amenities_up_charge_currency AS AmenitiesUpChargeCurrency,
           total_price_amount AS TotalPriceAmount,
           total_price_currency AS TotalPriceCurrency,
           duration_start AS DurationStart,
           duration_end AS DurationEnd,
           created_on_utc AS CreatedOnUtc
       FROM bookings
       WHERE id = @BookingId
       """;

        var booking = await connection.QueryFirstOrDefaultAsync<BookingResponse>(
     sql,
     new
     {
         request.BookingId
     });


        // the Solution for security Issue
        // this called RESOURCE BASE AUTHORIZATION
        if (booking is null || booking.UserId != _userContext.UserId)
            return Result.Failure<BookingResponse>(BookingErrors.NotFound);


        return booking;



     
    }
}