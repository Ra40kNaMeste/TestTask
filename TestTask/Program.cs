// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
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

        var api = new WildAPI(client, settings.Token, settings.UserTs, settings.Dest, settings.DeviceId);

        

        Console.WriteLine(await api.AddArticleAsync(address));
    }
}

record class AppSetting(string Token, long UserTs, string DeviceId, int Dest);
