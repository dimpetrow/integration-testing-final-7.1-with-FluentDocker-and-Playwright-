using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;

namespace Customers.WebApp.Tests.Integration.CustomersPage;

[Collection(nameof(SharedTestCollection))]
public class AddCustomerTests : IAsyncLifetime
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _customerGenerator = new Faker<Customer>()
            .RuleFor(x => x.Email, faker => faker.Person.Email)
            .RuleFor(x => x.FullName, faker => faker.Person.FullName)
            .RuleFor(x => x.GitHubUsername, SharedTestContext.ValidGitHubUsername)
            .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth.Date));
    private IPage _page = default!;

    public AddCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
    }

    public async Task InitializeAsync()
    {
        _page = await _testContext.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            BaseURL = SharedTestContext.AppUrl,
            IgnoreHTTPSErrors = true,
        });
    }

    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
    }

    [Fact]
    public async Task Create_CreatesCustomer_WhenDataIsValid()
    {
        // Arrange
        await _page.GotoAsync("/add-customer");
        var customer = _customerGenerator.Generate();

        // Act
        await _page.FillAsync("#fullname", customer.FullName);
        await _page.FillAsync("#email", customer.Email);
        await _page.FillAsync("#github-username", customer.GitHubUsername);
        await _page.FillAsync("#dob", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        await _page.ClickAsync("button[type=submit]");

        // Assert
        var linkElement = _page.Locator("article>p>a")
            .First
            ;
        var link = await linkElement.GetAttributeAsync("href");
        await _page.GotoAsync(link!);

        (await _page.Locator("#fullname-field").InnerTextAsync()).Should().Be(customer.FullName);
        (await _page.Locator("#email-field").InnerTextAsync()).Should().Be(customer.Email);
        (await _page.Locator("#github-username-field").InnerTextAsync()).Should().Be(customer.GitHubUsername);
        var formattedDob = customer.DateOfBirth.ToString("dd\\/MM\\/yyyy");
        var dobField = await _page.Locator("#dob-field").InnerTextAsync();
        dobField.Should().Be(formattedDob);
    }

    [Fact]
    public async Task Create_ShowsError_WhenEmailIsInValid()
    {
        // Arrange
        await _page.GotoAsync("/add-customer");
        var customer = _customerGenerator.Generate();

        // Act
        await _page.FillAsync("#fullname", customer.FullName);
        await _page.FillAsync("#email", "notanemail"/*customer.Email*/);
        await _page.FillAsync("#github-username", customer.GitHubUsername);
        await _page.FillAsync("#dob", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        // Assert
        var listItem = _page.Locator("#create-customer-form>ul.validation-errors>li.validation-message");
        (await listItem.InnerTextAsync()).Should().Be("Invalid email format");
    }
}
