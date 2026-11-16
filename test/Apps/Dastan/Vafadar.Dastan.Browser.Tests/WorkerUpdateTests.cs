using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>What happens to an open page when the site gets a newer service worker (ADR 0007, point 5).</summary>
public sealed class WorkerUpdateTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    [Fact]
    public async Task A_newer_worker_takes_over_an_open_page_at_once_and_removes_what_the_old_one_kept()
    {
        var page = await site.Visitor();
        await page.GotoAsync("/en/");
        await page.EvaluateAsync("() => navigator.serviceWorker.ready.then(() => new Promise(done => navigator.serviceWorker.controller ? done() : navigator.serviceWorker.addEventListener('controllerchange', () => done())))");
        Assert.Equal(["dastan-shell-1"], await page.EvaluateAsync<string[]>("caches.keys()"));

        // The site now answers with a worker that keeps its files under another name – as after a release that
        // changed what is kept. The browser is told to look for it, which it also does by itself on a later visit.
        site.NewerWorker = true;
        var stores = await page.EvaluateAsync<string[]>(
            """
            async () => {
                const registration = await navigator.serviceWorker.ready;
                const taken = new Promise(done => navigator.serviceWorker.addEventListener('controllerchange', () => done(), { once: true }));
                await registration.update();
                await taken;

                // The page changes hands when the new worker starts to take over; what it does first – removing the
                // old store – is finished when it says that it is active.
                const worker = navigator.serviceWorker.controller;
                if (worker.state !== 'activated') {
                    await new Promise(done => worker.addEventListener('statechange', () => worker.state === 'activated' && done()));
                }

                return (await caches.keys()).sort();
            }
            """);

        // The page was not closed and not loaded again: the new worker is in charge of it, and the old store is gone.
        Assert.Equal(["dastan-shell-2"], stores);
        Assert.Equal("Dastan", await page.Locator("h1").InnerTextAsync());

        // What the worker is there for still works, from the new store.
        await page.Context.SetOfflineAsync(true);
        await page.GotoAsync($"/fa/{BrowserSite.Story}");
        Assert.Equal("No connection", await page.Locator("h1").InnerTextAsync());
        await page.Context.SetOfflineAsync(false);
        await page.Locator("a.action").First.ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "fa");
        await Assertions.Expect(page.Locator(".reading-form")).ToHaveCountAsync(1);
    }
}
