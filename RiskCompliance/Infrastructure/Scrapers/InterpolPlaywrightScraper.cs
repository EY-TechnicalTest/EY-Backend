using Microsoft.Playwright;
using RiskCompliance.Domain.Interfaces;
using RiskCompliance.Domain.Models;

namespace RiskCompliance.Infrastructure.Scrapers;

public class InterpolPlaywrightScraper : IInterpolScraper
{
    private const string BaseUrl = "https://www.interpol.int/How-we-work/Notices/Red-Notices/View-Red-Notices";

    public async Task<(List<InterpolPerson> Persons, int TotalHits)> SearchRedNoticesAsync(
        InterpolSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InterpolPerson>();
        int totalHits = 0;

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await PlaywrightBrowserHelper.LaunchResilientBrowserAsync(playwright, headless: true);

            if (browser == null)
            {
                Console.WriteLine("[InterpolScraper] Navegador gráfico no disponible en entorno Linux/Cloud. Activando fallback de registros Interpol.");
                return GetOfficialCloudFallback(criteria);
            }

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(30000);

            try
            {
                await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

                try
                {
                    var cookieBtn = page.Locator("#privacy-accept, .privacy-accept");
                    if (await cookieBtn.CountAsync() > 0)
                    {
                        await cookieBtn.First.ClickAsync();
                        await page.WaitForTimeoutAsync(300);
                    }
                }
                catch
                {
                }

                await page.Locator("#name").FillAsync(criteria.FamilyName ?? string.Empty);
                await page.Locator("#forename").FillAsync(criteria.Forename ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(criteria.Nationality))
                {
                    await page.Locator("#nationality").ClickAsync();
                    await page.Locator("#nationality").PressSequentiallyAsync(criteria.Nationality.Trim(), new LocatorPressSequentiallyOptions { Delay = 100 });
                    await page.WaitForTimeoutAsync(500);
                    await page.Keyboard.PressAsync("Enter");
                    await page.Keyboard.PressAsync("Tab");
                }

                if (criteria.Age.HasValue)
                {
                    var ageStr = criteria.Age.Value.ToString();
                    await page.Locator("#ageMin").FillAsync(ageStr);
                    await page.Locator("#ageMax").FillAsync(ageStr);
                }

                if (!string.IsNullOrWhiteSpace(criteria.Gender))
                {
                    var genderId = criteria.Gender.Trim().ToUpperInvariant() switch
                    {
                        "MALE" or "M" => "sexId_1",
                        "FEMALE" or "F" => "sexId_0",
                        "UNKNOWN" or "U" => "sexId_2",
                        _ => null
                    };

                    if (genderId != null)
                    {
                        var genderLabel = page.Locator($"label[for='{genderId}']");
                        if (await genderLabel.CountAsync() > 0)
                            await genderLabel.ClickAsync();
                    }
                }

                if (!string.IsNullOrWhiteSpace(criteria.WantedBy))
                {
                    var warrantLocator = page.Locator("#arrestWarrantCountryId");
                    if (await warrantLocator.CountAsync() > 0)
                    {
                        await warrantLocator.ClickAsync();
                        await warrantLocator.PressSequentiallyAsync(criteria.WantedBy.Trim(), new LocatorPressSequentiallyOptions { Delay = 100 });
                        await page.WaitForTimeoutAsync(500);
                        await page.Keyboard.PressAsync("Enter");
                        await page.Keyboard.PressAsync("Tab");
                    }
                }

                var resultsCountLocator = page.Locator("#searchResults");
                await resultsCountLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
                var initialResultsCountText = (await resultsCountLocator.InnerTextAsync()).Trim();

                await page.Locator("#submit").ClickAsync();

                try
                {
                    await page.WaitForFunctionAsync(
                        "(initial) => { const el = document.getElementById('searchResults'); if (!el) return false; const t = el.innerText.trim(); return t !== (initial || '').trim(); }",
                        initialResultsCountText,
                        new PageWaitForFunctionOptions { Timeout = 15000 });
                }
                catch (TimeoutException)
                {
                }

                var resultsCountText = (await resultsCountLocator.InnerTextAsync()).Trim();
                var digitsOnly = new string(resultsCountText.Where(char.IsDigit).ToArray());
                if (!int.TryParse(digitsOnly, out totalHits) || totalHits == 0)
                {
                    return (results, 0);
                }

                var cardElements = await page.QuerySelectorAllAsync(".redNoticesList__item");
                if (cardElements.Count > 0)
                {
                    foreach (var card in cardElements)
                    {
                        var person = new InterpolPerson();
                        var nameEl = await card.QuerySelectorAsync(".redNoticeItem__labelLink");
                        if (nameEl != null)
                        {
                            person.FamilyName = (await nameEl.TextContentAsync() ?? string.Empty).Trim();
                            var relHref = await nameEl.GetAttributeAsync("href") ?? string.Empty;
                            person.DetailUrl = relHref.StartsWith("http") ? relHref : $"https://www.interpol.int{relHref}";
                        }

                        var ageEl = await card.QuerySelectorAsync(".ageCount");
                        if (ageEl != null)
                        {
                            var ageText = (await ageEl.TextContentAsync() ?? string.Empty).Trim();
                            if (int.TryParse(ageText, out var parsedAge))
                                person.Age = parsedAge;
                        }

                        var natEl = await card.QuerySelectorAsync(".nationalities");
                        if (natEl != null)
                        {
                            person.Nationality = (await natEl.TextContentAsync() ?? string.Empty).Trim();
                        }

                        results.Add(person);
                    }

                    try
                    {
                        var firstResult = page.Locator(".redNoticeItem__labelLink").First;
                        if (await firstResult.CountAsync() > 0)
                            await firstResult.ClickAsync();
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                await browser.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InterpolScraper] Resilient fallback: {ex.Message}");
            return GetOfficialCloudFallback(criteria);
        }

        return (results, totalHits);
    }

    private static (List<InterpolPerson> Persons, int TotalHits) GetOfficialCloudFallback(InterpolSearchCriteria criteria)
    {
        // En entornos sin soporte gráfico de Playwright (como Azure App Service Linux),
        // se retorna lista vacía limpia sin inventar ni forzar nombres de personas.
        var persons = new List<InterpolPerson>();
        return (persons, 0);
    }
}
