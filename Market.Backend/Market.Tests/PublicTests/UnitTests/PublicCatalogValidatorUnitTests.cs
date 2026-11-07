using Market.Application.Modules.Public.Queries.ListDentists;
using Market.Application.Modules.Public.Queries.ListServices;

namespace Market.Tests.PublicTests.UnitTests;

/// <summary>
/// Public catalog filters: search up to 100 characters, max price 0–10 000 KM, sort only from the enum,
/// ids greater than 0. All filters are optional.
/// </summary>
public sealed class PublicCatalogValidatorUnitTests
{
    private readonly ListPublicServicesQueryValidator _servicesValidator = new();
    private readonly ListPublicDentistsQueryValidator _dentistsValidator = new();

    // === services ===

    [Fact]
    public void Services_NoFilters_IsValid()
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Services_AllFilters_IsValid()
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery
        {
            Search = "plomba",
            CategoryId = 1,
            MaxPriceBam = 100m,
            Sort = PublicServiceSort.PriceDesc
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Services_SearchTooLong_IsInvalid()
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery { Search = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicServicesQuery.Search));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10000.01)]
    public void Services_MaxPriceOutOfRange_IsInvalid(decimal maxPrice)
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery { MaxPriceBam = maxPrice });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicServicesQuery.MaxPriceBam));
    }

    [Fact]
    public void Services_MaxPriceExactly10000_IsValid()
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery { MaxPriceBam = 10000m });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Services_UnknownSort_IsInvalid()
    {
        // ?sort=7 binds to a number that is not in the enum
        var result = _servicesValidator.Validate(new ListPublicServicesQuery { Sort = (PublicServiceSort)7 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicServicesQuery.Sort));
    }

    [Fact]
    public void Services_CategoryIdZero_IsInvalid()
    {
        var result = _servicesValidator.Validate(new ListPublicServicesQuery { CategoryId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicServicesQuery.CategoryId));
    }

    // === dentists ===

    [Fact]
    public void Dentists_NoFilters_IsValid()
    {
        var result = _dentistsValidator.Validate(new ListPublicDentistsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Dentists_SearchTooLong_IsInvalid()
    {
        var result = _dentistsValidator.Validate(new ListPublicDentistsQuery { Search = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicDentistsQuery.Search));
    }

    [Fact]
    public void Dentists_SpecializationIdZero_IsInvalid()
    {
        var result = _dentistsValidator.Validate(new ListPublicDentistsQuery { SpecializationId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListPublicDentistsQuery.SpecializationId));
    }
}