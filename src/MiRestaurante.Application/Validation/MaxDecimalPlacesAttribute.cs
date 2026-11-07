using System.ComponentModel.DataAnnotations;

namespace MiRestaurante.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxDecimalPlacesAttribute(int decimals) : ValidationAttribute
{
    // Decimal equality ignores scale, so 1000.550 is valid and 1000.555 is not.
    public override bool IsValid(object? value) =>
        value is not decimal number || decimal.Round(number, decimals) == number;
}
