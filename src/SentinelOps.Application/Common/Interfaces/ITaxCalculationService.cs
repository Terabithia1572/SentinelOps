namespace SentinelOps.Application.Common.Interfaces;

public interface ITaxCalculationService
{
    (decimal TaxRate, decimal TaxAmount) CalculateTax(decimal subtotal, string? country, string? postalCode, bool isExempt);
}
