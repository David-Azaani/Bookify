namespace Bookify.Domain.Shared;

public record Currency
{

    internal static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Eur = new("EUR");

    private Currency(string currencyCode) => Code = currencyCode;
    public string Code { get; init; }


    public static readonly IReadOnlyCollection<Currency> All = new[] { Usd, Eur };



    public static Currency FromCode(string code)
    {

        return All.FirstOrDefault(c => c.Code == code) ?? throw new
            ApplicationException("The Currency Code is Invalid!");
    }


}