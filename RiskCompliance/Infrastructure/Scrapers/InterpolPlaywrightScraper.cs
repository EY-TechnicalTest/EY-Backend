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

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60000);

        try
        {
            await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

            try
            {
                var cookieBtn = page.Locator("#privacy-accept, .privacy-accept");
                if (await cookieBtn.CountAsync() > 0)
                {
                    await cookieBtn.First.ClickAsync();
                    await page.WaitForTimeoutAsync(500);
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
                await page.WaitForTimeoutAsync(1000);
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
                    await page.WaitForTimeoutAsync(1000);
                    await page.Keyboard.PressAsync("Enter");
                    await page.Keyboard.PressAsync("Tab");
                }
            }

            var resultsCountLocator = page.Locator("#searchResults");
            await resultsCountLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var initialResultsCountText = (await resultsCountLocator.InnerTextAsync()).Trim();

            await page.Locator("#submit").ClickAsync();

            try
            {
                await page.WaitForFunctionAsync(
                    "(initial) => { const el = document.getElementById('searchResults'); if (!el) return false; const t = el.innerText.trim(); return t !== (initial || '').trim(); }",
                    initialResultsCountText,
                    new PageWaitForFunctionOptions { Timeout = 30000 });
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
                    {
                        await firstResult.ClickAsync();
                        var detailPanel = page.Locator("#singlePanel");
                        await detailPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

                        if (results.Count > 0)
                        {
                            var detailedPerson = results[0];
                            detailedPerson.FamilyName = (await detailPanel.Locator("#name").InnerTextAsync()).Trim();
                            detailedPerson.Forename = (await detailPanel.Locator("#forename").InnerTextAsync()).Trim();
                            detailedPerson.Gender = (await detailPanel.Locator("#sex_id").InnerTextAsync()).Trim();
                            detailedPerson.DateOfBirth = (await detailPanel.Locator("#date_of_birth").InnerTextAsync()).Trim();
                            detailedPerson.PlaceOfBirth = (await detailPanel.Locator("#place_of_birth").InnerTextAsync()).Trim();
                            detailedPerson.Nationality = (await detailPanel.Locator("#nationalities").InnerTextAsync()).Trim();
                            detailedPerson.Charges = (await detailPanel.Locator("#charge").InnerTextAsync()).Trim();
                        }
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InterpolScraper] Error: {ex.Message}");
            throw;
        }
        finally
        {
            await browser.CloseAsync();
        }

        return (results, totalHits);
    }
}
