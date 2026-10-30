using CSharpExercises.Checksums;
using CSharpExercises.Finance;

namespace CSharpExercises.Tests;

public class ChecksumsAndFinanceTests
{
    [Theory]
    [InlineData("4539 1488 0343 6467", true)]
    [InlineData("4539 1488 0343 6468", false)]   // last digit off by one
    [InlineData("4539 1488 0334 6467", false)]   // adjacent swap
    [InlineData("79927398713", true)]
    [InlineData("0", false)]
    [InlineData("12a4", false)]
    public void Luhn_Validates(string number, bool expected) => Assert.Equal(expected, Luhn.IsValid(number));

    [Fact]
    public void Luhn_CheckDigitAndBrand()
    {
        Assert.Equal(3, Luhn.CheckDigit("7992739871"));
        Assert.Equal("Visa", Luhn.CardBrand("4111 1111 1111 1111"));
        Assert.Equal("Mastercard", Luhn.CardBrand("5555555555554444"));
        Assert.Equal("Mastercard", Luhn.CardBrand("2221000000000009"));
        Assert.Equal("American Express", Luhn.CardBrand("378282246310005"));
        Assert.Null(Luhn.CardBrand("4111 1111 1111 1112"));
    }

    [Theory]
    [InlineData("DE89 3704 0044 0532 0130 00")]
    [InlineData("GB82 WEST 1234 5698 7654 32")]
    [InlineData("fr14 2004 1010 0505 0001 3m02 606")]
    [InlineData("MK07 2501 2000 0058 984")]
    public void Iban_AcceptsValidNumbers(string iban) => Assert.True(Iban.IsValid(iban, out _));

    [Theory]
    [InlineData("DE89 3704 0044 0532 0130 01", "typo")]
    [InlineData("DE89 3704 0044 0532 0130", "22 characters")]
    [InlineData("1234 5678", "country code")]
    [InlineData("GB82 WEST 1234 5698 7654 3!", "letters and digits")]
    public void Iban_ExplainsWhatIsWrong(string iban, string expectedError)
    {
        Assert.False(Iban.IsValid(iban, out var error));
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void Iban_CreatesAndFormats()
    {
        var iban = Iban.Create("DE", "370400440532013000");

        Assert.Equal("DE89370400440532013000", iban);
        Assert.Equal("DE89 3704 0044 0532 0130 00", Iban.Format(iban));
    }

    [Theory]
    [InlineData("0-306-40615-2", true)]
    [InlineData("0-8044-2957-X", true)]
    [InlineData("0-306-40615-3", false)]
    [InlineData("0-306-46015-2", false)] // swapped digits are always caught
    public void Isbn10_Validates(string isbn, bool expected) => Assert.Equal(expected, Isbn.IsValid10(isbn));

    [Fact]
    public void Isbn13_ValidatesAndConverts()
    {
        Assert.True(Isbn.IsValid13("978-0-306-40615-7"));
        Assert.False(Isbn.IsValid13("978-0-306-40615-8"));
        Assert.Equal("9780306406157", Isbn.ToIsbn13("0-306-40615-2"));
        Assert.Throws<FormatException>(() => Isbn.ToIsbn13("0-306-40615-3"));
    }

    [Fact]
    public void Damm_CatchesEverySingleErrorAndAdjacentSwap()
    {
        Assert.Equal(4, Damm.CheckDigit("572"));
        Assert.True(Damm.IsValid("5724"));

        var valid = Damm.Append("8402917365");
        for (var i = 0; i < valid.Length; i++)
        {
            for (var d = '0'; d <= '9'; d++)
                if (d != valid[i]) Assert.False(Damm.IsValid(valid[..i] + d + valid[(i + 1)..]));

            if (i + 1 < valid.Length && valid[i] != valid[i + 1])
                Assert.False(Damm.IsValid(valid[..i] + valid[i + 1] + valid[i] + valid[(i + 2)..]));
        }
    }

    [Fact]
    public void Luhn_MissesTheZeroNineSwapThatDammCatches()
    {
        // Doubling turns 0 into 0 and 9 into 18 → 9, so "09" and "90" add the same amount to the Luhn sum.
        var luhn = "109" + Luhn.CheckDigit("109");
        var damm = Damm.Append("109");

        Assert.True(Luhn.IsValid(luhn));
        Assert.True(Luhn.IsValid("190" + luhn[3..]));
        Assert.True(Damm.IsValid(damm));
        Assert.False(Damm.IsValid("190" + damm[3..]));
    }

    [Fact]
    public void Amortization_ScheduleEndsAtZero()
    {
        var schedule = Amortization.Schedule(200_000m, 6m, 360);

        Assert.Equal(1199.10m, Amortization.MonthlyPayment(200_000m, 6m, 360));
        Assert.Equal(360, schedule.Count);
        Assert.Equal(0m, schedule[^1].Balance);
        Assert.Equal(1000m, schedule[0].Interest);   // 0.5% of 200,000
        Assert.Equal(199.10m, schedule[0].Principal);
        Assert.Equal(200_000m, schedule.Sum(i => i.Principal));
        Assert.True(schedule[^1].Interest < 10);    // by the end it's almost all principal
    }

    [Fact]
    public void Amortization_ExtraPaymentsAndZeroInterest()
    {
        var (saved, months) = Amortization.ExtraPaymentEffect(200_000m, 6m, 360, extra: 200m);

        Assert.True(saved > 60_000m);
        Assert.InRange(months, 90, 110);
        Assert.Equal(100m, Amortization.MonthlyPayment(1_200m, 0m, 12));
    }

    [Fact]
    public void CashFlows_NpvAndIrr()
    {
        double[] project = [-1000, 300, 400, 500];

        Assert.Equal(200, CashFlows.NetPresentValue(0, project), 9);
        Assert.Equal(-21.04, CashFlows.NetPresentValue(0.1, project), 2);

        var irr = CashFlows.InternalRateOfReturn(project)!.Value;
        Assert.Equal(0, CashFlows.NetPresentValue(irr, project), 6);
        Assert.InRange(irr, 0.0889, 0.0890);

        Assert.Null(CashFlows.InternalRateOfReturn([100, 200]));
    }

    [Fact]
    public void CashFlows_PaybackCompoundingAndDoubling()
    {
        Assert.Equal(2.6, CashFlows.PaybackPeriod([-1000, 300, 400, 500])!.Value, 9);
        Assert.Null(CashFlows.PaybackPeriod([-1000, 100]));

        Assert.Equal(1104.71, CashFlows.FutureValue(1000, 0.10, 1, 12), 2); // monthly compounding beats 10%
        var (rule, exact) = CashFlows.YearsToDouble(8);
        Assert.Equal(9, rule);
        Assert.Equal(9.006, exact, 3);
    }
}
