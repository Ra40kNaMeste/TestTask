// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using System.Text.Json;
using WildberriesAPI;
static class Program
{
    public static async Task<int> Main(string[] args)
    {

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddCommandLine(args)
            .Build();

        var settings = config.Get<AppSetting>();

        using HttpClient client = new HttpClient();

        var api = await BuildWildAPI(client, settings);
        if (api == null)
        {
            Console.WriteLine("Authorization failed with an error");
            return -1;
        }

        await api.SetDist(settings.latitude, settings.longitude);
        await api.SetUserTs();

        await StartMenu(api);

        SaveAPI(api, settings.saveFile);

        return 0;
    }

    private static async Task<WildAPI?> BuildWildAPI(HttpClient client, AppSetting setting)
    {
        if (File.Exists(setting.saveFile))
        {
            var memento = JsonSerializer.Deserialize<WildAPIMementor>(File.ReadAllText(setting.saveFile));
            if (memento != null)
                return new(client, setting.deviceId, memento);
        }

        var api = new WildAPI(client, setting.deviceId);
        try
        {
            Image? captcha;
            while ((captcha = await api.GetCaptchaAsync(setting.phone)) != null)
            {
                captcha.SaveAsJpeg("./captcha.jpeg");
                Console.WriteLine($"Please, open captcha at address: {Directory.GetCurrentDirectory()}\\captcha.jpeg and write code");
                var captchaCode = Console.ReadLine();
                await api.SendCaptchaCodeAsync(setting.phone, captchaCode);
            }
            bool res = false;
            Console.WriteLine($"Write the code from sms by number {setting.phone}");
            var code = Console.ReadLine();
            res = await api.WriteTokenAsync(Convert.ToInt32(code));
            return res ? api : null;

        }
        catch (Exception)
        {
            Console.WriteLine("Http request is failed");
            return null;
        }

    }

    private static async Task StartMenu(WildAPI api)
    {
        bool isEnd = false;
        while (!isEnd)
        {
            Console.WriteLine("".PadRight(20, '-'));
            Console.WriteLine("""
                Input number action
                0 Exit
                1 Add article by url
                2 Add article by number
                """);
            Console.WriteLine("".PadRight(20, '-'));
            try
            {
                switch (Convert.ToInt32(Console.ReadLine()))
                {
                    case 0:
                        isEnd = true;
                        break;
                    case 1:
                        await AddArticleByAddress(api);
                        break;
                    case 2:
                        await AddArticle(api);
                        break;
                    default:
                        Console.WriteLine("Number is incorrect");
                        break;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("There was an error. Please, try again");
            }
        }
    }

    private static void SaveAPI(WildAPI api, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(api.GetMementor()));
    }

    private static async Task AddArticleByAddress(WildAPI api)
    {
        Console.WriteLine("Please, enter web address");
        var article = Console.ReadLine();
        Console.WriteLine("Please, enter count");
        var count = Convert.ToInt32(Console.ReadLine());
        await api.AddArticleAsync(article, count);
        Console.WriteLine("Article is success added");
    }

    private static async Task AddArticle(WildAPI api)
    {
        Console.WriteLine("Please, enter article");
        var article = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine("Please, enter count");
        var count = Convert.ToInt32(Console.ReadLine());
        await api.AddArticleAsync(article, count);
        Console.WriteLine("Article is success added");
    }
}

record class AppSetting(double latitude, double longitude, string deviceId, string phone, string saveFile);
