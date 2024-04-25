using Microsoft.Extensions.Configuration;
using WildberriesAPI;

namespace WildberriesAPITests
{
    public class SimpleTests
    {
        public SimpleTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _numenclaturs = configuration.GetSection("Numenclaturs").Get<Numenclaturs>();

            _settings = configuration.GetSection("APISettings").Get<APISettings>();
        }

        [Fact]
        public async void DestTest()
        {
            using HttpClient client = new();
            var api = await BuildAPI(client);
            Assert.Equal(_settings.Dest, api.Dest);
        }

        [Fact]
        public async void AddArticleSimleTest()
        {
            await AddArticleTestAsync(_numenclaturs.ArticleSimleTest);
        }

        [Fact]
        public async void AddArticleMultiTest()
        {
            await AddArticleTestAsync(_numenclaturs.ArticleMultiTest, 3);
        }

        [Fact]
        public async void AddSiteSimleTest()
        {
            await AddArticleTestAsync(_numenclaturs.SiteSimpleTest);
        }

        [Fact]
        public async void AddSiteMultiTest()
        {
            await AddArticleTestAsync(_numenclaturs.SiteMultiTest, 3);
        }


        private async Task AddArticleTestAsync(int nm, int count = 1)
        {
            using HttpClient client = new();
            var api = await BuildAPI(client);
            await api.AddArticleAsync(nm);
            await api.SyncAsync(true);
            var item = api.BasketItems.FirstOrDefault(i => i.cod_1s == nm);
            Assert.NotNull(item);
            Assert.Equal(item.quantity, 1);
        }

        private async Task AddArticleTestAsync(SiteNumenclatur nm, int count = 1)
        {
            using HttpClient client = new();
            var api = await BuildAPI(client);
            await api.AddArticleAsync(nm.Site);
            await api.SyncAsync(true);
            var item = api.BasketItems.FirstOrDefault(i => i.cod_1s == nm.Numenclature);
            Assert.NotNull(item);
            Assert.Equal(item.quantity, 1);
        }

        private async Task<WildAPI> BuildAPI(HttpClient client)
        {
            var api = new WildAPI(client, _settings.DeviceId);
            api.Token = _settings.Token;
            await api.SetDistAsync(_settings.Latitude, _settings.Longitude);
            await api.SyncAsync();
            return api;
        }


        private APISettings _settings;

        private Numenclaturs _numenclaturs;
    }

    record class APISettings(string DeviceId, string Token, double Latitude, double Longitude, int Dest);
    record class Numenclaturs(int ArticleSimleTest, SiteNumenclatur SiteSimpleTest, int ArticleMultiTest, SiteNumenclatur SiteMultiTest);
    record class SiteNumenclatur(string Site, int Numenclature);
}