using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PdfFromRazor.Invoices;

//Microsoft.Playwright.Program.Main(["install"]);

IServiceCollection services = new ServiceCollection();
services.AddLogging();

var serviceProvider = services.BuildServiceProvider();

var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

var invoice = InvoiceGenerator.Generate();

var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
{
    var dictionary = new Dictionary<string, object?>
    {
        { "Invoice", invoice }
    };

    var parameters = ParameterView.FromDictionary(dictionary);
    var output = await htmlRenderer.RenderComponentAsync<InvoiceView>(parameters);
    return output.ToHtmlString();
});

using var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true
});

var page = await browser.NewPageAsync();
await page.SetContentAsync(html);

await page.PdfAsync(new PagePdfOptions
{
    Format = "A4",
    Path = "./invoice.pdf"
});

await page.CloseAsync();
