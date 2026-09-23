using Microsoft.Playwright;

namespace RiskCompliance.Infrastructure.Scrapers;

public static class PlaywrightBrowserHelper
{
    private static bool _driverInstalled = false;
    private static readonly object _lock = new();

    public static async Task<IBrowser> LaunchResilientBrowserAsync(IPlaywright playwright, bool headless = true)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[]
            {
                "--disable-blink-features=AutomationControlled",
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage"
            }
        };

        try
        {
            return await playwright.Chromium.LaunchAsync(launchOptions);
        }
        catch (PlaywrightException)
        {
            try
            {
                var chromeOptions = new BrowserTypeLaunchOptions
                {
                    Channel = "chrome",
                    Headless = headless,
                    Args = launchOptions.Args
                };
                return await playwright.Chromium.LaunchAsync(chromeOptions);
            }
            catch (PlaywrightException)
            {
                try
                {
                    var edgeOptions = new BrowserTypeLaunchOptions
                    {
                        Channel = "msedge",
                        Headless = headless,
                        Args = launchOptions.Args
                    };
                    return await playwright.Chromium.LaunchAsync(edgeOptions);
                }
                catch
                {
                    lock (_lock)
                    {
                        if (!_driverInstalled)
                        {
                            Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                            _driverInstalled = true;
                        }
                    }
                    return await playwright.Chromium.LaunchAsync(launchOptions);
                }
            }
        }
    }
}
