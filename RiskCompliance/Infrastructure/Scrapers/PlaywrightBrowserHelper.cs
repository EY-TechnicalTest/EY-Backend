using Microsoft.Playwright;

namespace RiskCompliance.Infrastructure.Scrapers;

public static class PlaywrightBrowserHelper
{
    public static async Task<IBrowser?> LaunchResilientBrowserAsync(IPlaywright playwright, bool headless = true)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[]
            {
                "--disable-blink-features=AutomationControlled",
                "--headless=new",
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage"
            }
        };

        try
        {
            return await playwright.Chromium.LaunchAsync(launchOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaywrightBrowserHelper] Default launch failed: {ex.Message}");

            // Intentar con Google Chrome instalado
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
            catch
            {
                // Intentar con Microsoft Edge instalado
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
                    // Si nos encontramos en un contenedor Linux sin librerías gráficas (ej. Azure App Service Linux)
                    Console.WriteLine("[PlaywrightBrowserHelper] Entorno sin soporte de librerías gráficas Linux (ej. libatk). Activando modo resiliente.");
                    return null;
                }
            }
        }
    }
}
