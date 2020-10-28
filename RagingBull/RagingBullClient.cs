using Core;
using Core.Model;
using LottoXService;
using PuppeteerSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#nullable enable

namespace RagingBull
{
    public abstract class RagingBullClient : ILivePortfolioClient
    {
        public RagingBullClient(RagingBullConfig config)
        {
            Email = config.Email;
            Password = config.Password;
            PortfolioUrl = config.PortfolioUrl;
            ChromePath = config.ChromePath;
        }

        protected string Email { get; }
        protected string Password { get; }
        protected string PortfolioUrl { get; }
        protected string ChromePath { get; }
        protected Browser? Browser { get; private set; }

        public abstract IList<Position> GetPositions();

        public virtual async Task<bool> Logout()
        {
            Log.Information("Logging out of RagingBull");
            try
            {
                Page page = await GetPage();
                await page.ClickAsync("#userbox");
                ElementHandle? logout = await GetElementWithContent(page, "a", "Logout");
                if (logout == null)
                {
                    throw new RagingBullException("Could not find logout button");
                }
                await logout.ClickAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to logout of RagingBull");
                return false;
            }
            return true;
        }

        protected async Task<bool> IsLoggedIn()
        {
            if (Browser == null) return false;

            Page page = await GetPage();
            return page.Url == PortfolioUrl;
        }

        protected virtual async Task<bool> TryLogin()
        {
            if (await IsLoggedIn()) return true;

            if (Browser == null)
            {
                Log.Information("Starting headless browser");
                Browser = await StartBrowserAndCreatePage();
            }
            Page page = await GetPage();

            try
            {
                await page.GoToAsync(PortfolioUrl);
                if (page.Url == PortfolioUrl) return true;

                Log.Information("Logging into RagingBull");
                await page.ClickAsync("#email");
                await page.Keyboard.TypeAsync(Email);
                await page.ClickAsync("#password");
                await page.Keyboard.TypeAsync(Password);
                await page.Keyboard.DownAsync("Enter");
                int timeOut = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
                await page.WaitForNavigationAsync(new NavigationOptions { Timeout = timeOut });

                if (page.Url != PortfolioUrl)
                {
                    throw new RagingBullException("Navigation to portfolio page failed");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to login to RagingBull");
                return false;
            }
            return true;
        }

        protected async Task<ElementHandle?> GetElementWithContent(Page page, string elementType, string content)
        {
            string xPathMatch = string.Format("//{0}[contains(., '{1}')]", elementType, content);
            return await GetElement(page, xPathMatch);
        }

        protected async Task<ElementHandle?> GetElement(Page page, string xPathMatch)
        {
            ElementHandle[] elementHandles = await page.XPathAsync(xPathMatch);

            List<ElementHandle> visibleHandles = new List<ElementHandle>();
            foreach (ElementHandle handle in elementHandles)
            {
                if (await handle.IsIntersectingViewportAsync())
                {
                    visibleHandles.Add(handle);
                }
            }
            if (visibleHandles.Count == 0)
            {
                return null;
            }
            if (visibleHandles.Count > 1)
            {
                Log.Warning(string.Format("Multiple elements found with xPath {0}", xPathMatch));
            }
            return visibleHandles[0];
        }

        protected async Task<Page> GetPage()
        {
            Page[] pages = await Browser.PagesAsync();
            return pages[0];
        }

        private async Task<Browser> StartBrowserAndCreatePage()
        {
            // This only downloads the browser version if it is has not been downloaded already
            //await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = ChromePath
            });
            await browser.NewPageAsync();
            return browser;
        }
    }
}
