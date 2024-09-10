
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Model.Compose;
using Ductus.FluentDocker.Services;
using Microsoft.Playwright;

namespace Customers.WebApp.Tests.Integration
{
    [CollectionDefinition(nameof(SharedTestCollection))]
    public class SharedTestCollection : ICollectionFixture<SharedTestContext>
    { }

    public class SharedTestContext : IAsyncLifetime
    {
        public const string AppUrl = "https://localhost:7780";
        public const string ValidGitHubUsername = "validuser";

        private static readonly TemplateString DockerComposeFileRelativePath = (TemplateString)"../../../docker-compose.integration.yaml";
        private static readonly string DockerComposeFile =
            Path.Combine(Directory.GetCurrentDirectory(), DockerComposeFileRelativePath);
        private readonly ICompositeService _compositeService = new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(DockerComposeFile)
            .AssumeComposeVersion(ComposeVersion.V2)
            .RemoveOrphans()
            .WaitForHttp("test-app", AppUrl)
            .Build();
        private readonly GitHubApiServer _gitHubApiServer = new();

        private IPlaywright? _playwright;
        public IBrowser Browser { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            _gitHubApiServer.Start();
            _gitHubApiServer.SetupUser(ValidGitHubUsername);

            _compositeService.Start();

            _playwright = await Playwright.CreateAsync();
            var browserOptions = new BrowserTypeLaunchOptions
            {
                //Headless = false,
                SlowMo = 100
            };
            Browser =
                //await _playwright.Chromium.LaunchAsync(browserOptions);
                await _playwright.Firefox.LaunchAsync(browserOptions);
            //await _playwright.Webkit.LaunchAsync(browserOptions);
        }

        public async Task DisposeAsync()
        {
            if (Browser is not null)
            {
                await Browser.DisposeAsync();
            }
            _playwright?.Dispose();

            _compositeService?.Dispose();
            _gitHubApiServer?.Dispose();
        }
    }
}
