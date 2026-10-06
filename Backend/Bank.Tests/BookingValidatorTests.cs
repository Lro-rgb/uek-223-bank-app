using Bank.Core.Helper;
using Bank.Core.Models;

namespace Bank.Tests;

// Reine Unit-Tests ohne Datenbank.
public class BookingValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validate_AmountNotPositive_ReturnsInvalidAmount(decimal amount)
    {
        Assert.Equal(BookingResult.InvalidAmount, BookingValidator.Validate(1, 2, amount));
    }

    [Fact]
    public void Validate_SameLedger_ReturnsSameLedger()
    {
        Assert.Equal(BookingResult.SameLedger, BookingValidator.Validate(1, 1, 10));
    }

    [Fact]
    public void Validate_ValidInput_ReturnsSuccess()
    {
        Assert.Equal(BookingResult.Success, BookingValidator.Validate(1, 2, 0.01m));
    }
}
