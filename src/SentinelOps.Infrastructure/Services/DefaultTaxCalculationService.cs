using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Infrastructure.Services;

public class DefaultTaxCalculationService : ITaxCalculationService
{
    public (decimal TaxRate, decimal TaxAmount) CalculateTax(decimal subtotal, string? country, string? postalCode, bool isExempt)
    {
        if (isExempt || subtotal <= 0)
        {
            return (0m, 0m);
        }

        // Standard demonstration tax logic (mockable / extensible for Avalara, TaxJar, Stripe Tax)
        decimal rate = country?.ToUpperInvariant() switch
        {
            "US" => 0.08m, // Average US state sales tax baseline
            "GB" => 0.20m, // UK VAT
            "DE" or "FR" => 0.20m, // EU Standard VAT
            "TR" => 0.20m, // TR KDV
            _ => 0.0m
        };

        var amount = Math.Round(subtotal * rate, 2, MidpointRounding.AwayFromZero);
        return (rate, amount);
    }
}
