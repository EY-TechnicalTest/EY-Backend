using Microsoft.Playwright;
using RiskCompliance.Domain.Interfaces;
using RiskCompliance.Domain.Models;

namespace RiskCompliance.Infrastructure.Scrapers;

public class SmvPlaywrightScraper : ISmvScraper
{
    private const string BaseUrl = "https://www.smv.gob.pe/SIMV/Default.aspx";

    public async Task<SmvScreeningResult> SearchSanctionsAsync(string entityName, CancellationToken cancellationToken = default)
    {
        var result = new SmvScreeningResult
        {
            SearchedEntity = entityName,
            MatchedEntity = entityName
        };

        if (string.IsNullOrWhiteSpace(entityName))
            return result;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await PlaywrightBrowserHelper.LaunchResilientBrowserAsync(playwright, headless: true);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60000);

        try
        {
            await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            await page.Locator("#txtSearch").FillAsync(entityName);
            await page.Locator("#ibtnB").ClickAsync();

            try
            {
                await page.WaitForSelectorAsync("#btnSanciones", new PageWaitForSelectorOptions { Timeout = 12000, State = WaitForSelectorState.Visible });
            }
            catch (TimeoutException)
            {
                return result;
            }

            await page.Locator("#btnSanciones").ClickAsync();
            await page.WaitForSelectorAsync("#txtFechDesde", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 20000 });

            var today = DateTime.Today.ToString("dd/MM/yyyy");
            await page.EvaluateAsync($@"
                if (typeof window.ValidarCampo === 'function') {{ window.ValidarCampo = () => true; }}
                var elDesde = document.getElementById('txtFechDesde');
                if (elDesde) elDesde.value = '01/01/2008';
                var elHasta = document.getElementById('txtFechHasta');
                if (elHasta) elHasta.value = '{today}';
            ");

            await page.Locator("#MainContent_cbBuscar").ClickAsync();
            await page.WaitForTimeoutAsync(3000);

            try
            {
                var titleElement = page.Locator("#MainContent_lbltitulo");
                if (await titleElement.CountAsync() > 0)
                {
                    var rawTitle = await titleElement.InnerTextAsync() ?? string.Empty;
                    if (rawTitle.Contains(" | "))
                        result.MatchedEntity = rawTitle.Split(" | ")[0].Trim();
                    else
                        result.MatchedEntity = rawTitle.Replace("SANCIONES", "").Trim();
                }
            }
            catch
            {
            }

            var tableCount = await page.Locator("#MainContent_grdReporte").CountAsync();
            if (tableCount == 0)
                return result;

            var rows = await page.Locator("#MainContent_grdReporte tr").AllAsync();
            foreach (var row in rows)
            {
                var isHeader = await row.EvaluateAsync<bool>("el => el.classList.contains('headertable') || el.querySelector('th') !== null");
                if (isHeader) continue;

                var cells = await row.Locator("td").AllAsync();
                if (cells.Count < 9) continue;

                var sanction = new SmvSanction();
                sanction.ResolutionDate = (await cells[1].InnerTextAsync() ?? string.Empty).Trim();

                var linkLocator = cells[2].Locator("a");
                if (await linkLocator.CountAsync() > 0)
                {
                    sanction.ResolutionNumber = (await linkLocator.InnerTextAsync() ?? string.Empty).Trim();
                    var href = await linkLocator.GetAttributeAsync("href") ?? string.Empty;
                    sanction.ResolutionUrl = href.StartsWith("http") ? href : $"https://www.smv.gob.pe{href}";
                }
                else
                {
                    sanction.ResolutionNumber = (await cells[2].InnerTextAsync() ?? string.Empty).Trim();
                }

                sanction.Description = (await cells[3].InnerTextAsync() ?? string.Empty).Trim();
                sanction.Type = (await cells[4].InnerTextAsync() ?? string.Empty).Trim();
                sanction.Amount = (await cells[5].InnerTextAsync() ?? string.Empty).Trim();
                sanction.HasAppeal = (await cells[6].InnerTextAsync() ?? string.Empty).Trim();
                sanction.ResolutiveNumber = (await cells[7].InnerTextAsync() ?? string.Empty).Trim();
                sanction.ResolutiveDate = (await cells[8].InnerTextAsync() ?? string.Empty).Trim();

                result.Sanctions.Add(sanction);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmvScraper] Error: {ex.Message}");
            throw;
        }
        finally
        {
            await browser.CloseAsync();
        }

        return result;
    }
}
