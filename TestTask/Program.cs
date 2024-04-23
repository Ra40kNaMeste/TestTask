// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using WildberriesAPI;
static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Write address with articel");
        var address = "https://www.wildberries.ru/catalog/203803687/detail.aspx";
        if (address == null)
        {
            Console.WriteLine("Неверный адресс");
            return;
        }
        
        

        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        Console.WriteLine(config.GetSection("token").Value);
        var settings = config.Get<AppSetting>();


        using HttpClient client = new();

        var api = new WildAPI(client, settings.UserTs, settings.Dest, settings.DeviceId);

        var captcha = await api.GetCaptchaAsync(settings.Phone);
        captcha.SaveAsJpeg("./captcha.jpeg");
        Console.WriteLine($"open captcha at address: {Directory.GetCurrentDirectory()}\\captcha.jpeg and write code");
        var codeCaptcha = Console.ReadLine();
        try
        {
            var sticker = await api.SendCodeAndGetStickerAsync(settings.Phone, codeCaptcha);
            Console.WriteLine($"Write the code from sms by number{settings.Phone}");
            var code = Console.ReadLine();
            var temp = await api.WriteTokenAsync(sticker, Convert.ToInt32(code));
            temp = await api.AddArticleAsync("https://www.wildberries.ru/catalog/176188787/detail.aspx");
        }
        catch (Exception)
        {
            Console.WriteLine("captha is not correct");
            return;
        }


        Console.WriteLine(await api.AddArticleAsync(address));
    }
}

record class AppSetting(long UserTs, string DeviceId, int Dest, string Phone);
