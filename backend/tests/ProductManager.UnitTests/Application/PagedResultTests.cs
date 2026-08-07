using ProductManager.Application.Common;

namespace ProductManager.UnitTests.Application;

/// <summary>Covers the pagination metadata clients rely on to render page controls.</summary>
public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(25, 3, 9)]
    public void TotalPages_rounds_a_partial_final_page_up(int totalCount, int pageSize, int expected)
    {
        new PagedResult<string>([], 1, pageSize, totalCount).TotalPages.ShouldBe(expected);
    }

    [Fact]
    public void The_first_page_has_no_previous_page()
    {
        var page = new PagedResult<string>([], page: 1, pageSize: 10, totalCount: 30);

        page.HasPreviousPage.ShouldBeFalse();
        page.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void The_last_page_has_no_next_page()
    {
        var page = new PagedResult<string>([], page: 3, pageSize: 10, totalCount: 30);

        page.HasPreviousPage.ShouldBeTrue();
        page.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void Map_projects_the_items_and_carries_the_metadata_across()
    {
        var page = new PagedResult<int>([1, 2, 3], page: 2, pageSize: 3, totalCount: 9);

        var mapped = page.Map(value => value * 10);

        mapped.Items.ShouldBe([10, 20, 30]);
        mapped.Page.ShouldBe(2);
        mapped.PageSize.ShouldBe(3);
        mapped.TotalCount.ShouldBe(9);
        mapped.TotalPages.ShouldBe(3);
    }
}
