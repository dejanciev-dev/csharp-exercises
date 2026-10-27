using System.Linq.Expressions;
using CSharpExercises.Domain;

namespace CSharpExercises.Tests;

public class DomainTests
{
    private static DateOnly D(int month, int day) => new(2026, month, day);

    [Fact]
    public void PrefixedId_FormatsAndParses()
    {
        var id = new CustomerId(Guid.Parse("0192f3a8-1b2c-7d4e-8f90-a1b2c3d4e5f6"));

        Assert.Equal("cus_0192f3a81b2c7d4e8f90a1b2c3d4e5f6", id.ToString());
        Assert.Equal(id, PrefixedId.Parse<CustomerId>(id.ToString()));
    }

    [Fact]
    public void PrefixedId_RejectsAnIdOfTheWrongKind()
    {
        var order = PrefixedId.New<OrderId>();

        Assert.False(PrefixedId.TryParse<CustomerId>(order.ToString(), out _));
        var ex = Assert.Throws<FormatException>(() => PrefixedId.Parse<CustomerId>(order.ToString()));
        Assert.Contains("is a 'ord' id, but a 'cus' id was expected", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cus_")]
    [InlineData("cus_not-hex")]
    [InlineData("CUS_0192f3a81b2c7d4e8f90a1b2c3d4e5f6")]
    public void PrefixedId_RejectsMalformedText(string? text)
    {
        Assert.False(PrefixedId.TryParse<CustomerId>(text, out _));
    }

    [Fact]
    public void PrefixedId_SortsByCreationTimeAndRemembersIt()
    {
        var times = new[] { 3, 1, 2 }.Select(h => new DateTimeOffset(2026, 10, 27, h, 0, 0, TimeSpan.Zero)).ToList();
        var ids = times.Select(t => PrefixedId.New<OrderId>(t)).ToList();

        Assert.Equal(times.Order(), ids.OrderBy(id => id.ToString(), StringComparer.Ordinal).Select(PrefixedId.CreatedAt));
    }

    [Fact]
    public void EmailAddress_CanOnlyBeCreatedValid()
    {
        var created = EmailAddress.Create("  Ana@Example.MK ");
        var rejected = EmailAddress.Create("not an email");

        Assert.Equal("ana@example.mk", created.Value.Value);
        Assert.Equal("example.mk", created.Value.Domain);
        Assert.Equal("email.invalid", Assert.Single(rejected.Errors).Code);
        Assert.Equal(EmailAddress.Create("ana@example.mk").Value, created.Value); // value equality
    }

    [Fact]
    public void DateRange_EnforcesItsInvariant()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(D(3, 5), D(3, 1)));
        Assert.Equal(1, new DateRange(D(3, 1), D(3, 1)).Days);
        Assert.Equal(1, default(DateRange).Days); // default is a valid one-day range, not a broken one
    }

    [Fact]
    public void DateRange_OverlapAndIntersection()
    {
        var march = new DateRange(D(3, 1), D(3, 31));
        var holiday = new DateRange(D(3, 28), D(4, 4));
        var may = new DateRange(D(5, 1), D(5, 31));

        Assert.True(march.Overlaps(holiday));
        Assert.Equal(new DateRange(D(3, 28), D(3, 31)), march.Intersect(holiday));
        Assert.False(march.Overlaps(may));
        Assert.Null(march.Intersect(may));
        Assert.True(holiday.Contains(D(4, 4)));
    }

    [Fact]
    public void DateRange_MergesOverlappingAndTouchingRanges()
    {
        DateRange[] bookings =
        [
            new(D(3, 20), D(3, 21)),
            new(D(3, 1), D(3, 5)),
            new(D(3, 9), D(3, 10)),   // touches the merged 1–8, so joins it
            new(D(3, 4), D(3, 8)),
            new(D(3, 2), D(3, 3)),    // inside another range
        ];

        Assert.Equal([new(D(3, 1), D(3, 10)), new(D(3, 20), D(3, 21))], DateRange.Merge(bookings));
    }

    [Theory]
    [InlineData(100.00, 3, new[] { 33.34, 33.33, 33.33 })]
    [InlineData(0.05, 2, new[] { 0.03, 0.02 })]
    [InlineData(0.01, 3, new[] { 0.01, 0.00, 0.00 })]
    [InlineData(-10.00, 3, new[] { -3.34, -3.33, -3.33 })]
    public void Split_AlwaysAddsUpToTheTotal(double amount, int parts, double[] expected)
    {
        var shares = Allocation.Split((decimal)amount, parts);

        Assert.Equal(expected.Select(e => (decimal)e), shares);
        Assert.Equal((decimal)amount, shares.Sum());
    }

    [Fact]
    public void Split_NaiveRoundingLosesACent()
    {
        var naive = Enumerable.Repeat(Math.Round(100m / 3, 2), 3).Sum();

        Assert.Equal(99.99m, naive);
        Assert.Equal(100m, Allocation.Split(100m, 3).Sum());
    }

    [Fact]
    public void ByRatios_SpreadsADiscountOverOrderLines()
    {
        var shares = Allocation.ByRatios(10.00m, [19.99m, 5.00m, 75.01m]);

        Assert.Equal([2.00m, 0.50m, 7.50m], shares);
    }

    [Fact]
    public void ByRatios_GivesLeftoverUnitsToTheLargestRemainders()
    {
        // Exact shares are 1.43, 2.86 and 5.71 cents. Rounding down gives 1 + 2 + 5 = 8, leaving 2 cents,
        // which go to the two largest remainders: .86 first, then .71.
        Assert.Equal([0.01m, 0.03m, 0.06m], Allocation.ByRatios(0.10m, [1, 2, 4]));
    }

    [Fact]
    public void ByRatios_RejectsBadInput()
    {
        Assert.Throws<ArgumentException>(() => Allocation.ByRatios(1m, []));
        Assert.Throws<ArgumentException>(() => Allocation.ByRatios(1m, [0, 0]));
        Assert.Throws<ArgumentException>(() => Allocation.ByRatios(1m, [1, -1]));
        Assert.Throws<ArgumentException>(() => Allocation.Split(1.005m, 2));
    }

    private static readonly Customer[] Customers =
    [
        new("Ana", "MK", 25_000m, D(10, 1)),
        new("Ben", "DE", 12_000m, null),
        new("Cleo", "MK", 500m, D(10, 20)),
        new("Dan", "MK", 11_000m, new DateOnly(2025, 1, 1)),
    ];

    [Fact]
    public void Specification_CombinesRules()
    {
        var activeMacedonianVips = CustomerRules.Vip & CustomerRules.InCountry("MK") & CustomerRules.OrderedSince(D(1, 1));

        Assert.Equal(["Ana"], Customers.Where(activeMacedonianVips.IsSatisfiedBy).Select(c => c.Name));
    }

    [Fact]
    public void Specification_OrAndNot()
    {
        var lapsedOrForeign = !CustomerRules.OrderedSince(D(1, 1)) | !CustomerRules.InCountry("MK");

        Assert.Equal(["Ben", "Dan"], Customers.Where(lapsedOrForeign.IsSatisfiedBy).Select(c => c.Name));
    }

    [Fact]
    public void Specification_WorksAsAnExpressionForLinqProviders()
    {
        var spec = CustomerRules.Vip & CustomerRules.InCountry("DE");

        // IQueryable.Where takes the expression tree, which is what lets a provider translate it to SQL.
        var query = Customers.AsQueryable().Where(spec.Criteria);

        Assert.Equal(["Ben"], query.Select(c => c.Name));
        Assert.Single(spec.Criteria.Parameters);
    }

    [Fact]
    public void Specification_CombiningWithoutRebindingTheParameterFails()
    {
        Expression<Func<Customer, bool>> vip = c => c.LifetimeSpend >= 10_000m;
        Expression<Func<Customer, bool>> macedonian = x => x.Country == "MK";

        // Glue the bodies together naively: the second body still uses its own parameter x, which the lambda never declares.
        var naive = Expression.Lambda<Func<Customer, bool>>(Expression.AndAlso(vip.Body, macedonian.Body), vip.Parameters);

        Assert.Throws<InvalidOperationException>(() => naive.Compile());
        Assert.True((new Specification<Customer>(vip) & new Specification<Customer>(macedonian)).IsSatisfiedBy(Customers[0]));
    }
}
