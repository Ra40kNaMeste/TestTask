// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using WildberriesAPI;
static class Program
{
    public static void Main()
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

        var api = new WildAPI(client, settings.Token, settings.UserTs, settings.ChartId, settings.DeviceId);

        Console.WriteLine(api.AddArticle(address));
    }
}

record class AppSetting(string Token, int UserTs, string DeviceId, int ChartId);
