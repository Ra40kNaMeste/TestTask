using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace WildberriesAPI
{
    public class WildAPI:IDisposable
    {
        public WildAPI(HttpClient client, string token, long userTs, int dest, string deviceId) 
        {
            _client = client;
            _token = token;
            _userTs = userTs;
            _deviceId = deviceId;
            _dest = dest;
        }

        public async Task<bool> AddArticleAsync(string address, int count = 1)
        {
            var math = _addressRegex.Match(address);
            var nm = math.Groups["nomenclature"].Value;
            return await AddArticleAsync(Convert.ToInt32(nm), count);
        }
        public async Task<bool> AddArticleAsync(int article, int count = 1)
        {
            string postAddress = string.Format($"https://cart-storage-api.wildberries.ru/api/basket/sync?ts={_userTs}&device_id={_deviceId}");
            using HttpRequestMessage mess = new(HttpMethod.Post, postAddress);

            mess.Headers.Authorization = new("Bearer", _token);
            mess.Headers.Referrer = new($"https://www.wildberries.ru/catalog/{article}/detail.aspx");

            var chartId = await GetChartId(article, _dest);
            List<BodyContent> content = new() {
                new(chartId, count, article, _userTs, 1, "EX|5|MCS|IT|popular")
            };

            mess.Content = JsonContent.Create(content);

            var response = await _client.SendAsync(mess);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<AddResult>();
                    _userTs = data.change_ts;
                }
                catch (Exception ex)
                {

                    throw ex;
                }
                
            }
            return response.StatusCode == HttpStatusCode.OK;
        }

        private async Task<int> GetChartId(int article, int dest, string curr = "rub")
        {
            string gg = $"https://card.wb.ru/cards/v2/detail?appType=1&curr={curr}&dest={dest}&spp=30&nm={article}";
            var response = await _client.GetAsync($"https://card.wb.ru/cards/v2/detail?appType=1&curr={curr}&dest={dest}&spp=30&nm={article}");
            if(response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<CardsResponseData>();
                    var res = data?.data?.products?.First().sizes.First().optionId;
                    return res.Value;
                }
                catch (Exception ex)
                {

                    throw new HttpRequestException("JSON-file not suppored");
                }
            }
            throw new HttpRequestException();
        }
        

        public void Dispose()
        {
            _client.Dispose();
        }

        private readonly string _token;

        private readonly HttpClient _client;
        private long _userTs;
        private readonly int _dest;
        private readonly string _deviceId;

        private readonly static Regex _addressRegex = new(@"www\.wildberries\.ru/catalog/(?<nomenclature>\d+)/detail\.aspx");
    }
    internal record class BodyContent(int chrt_id, int quantity, int cod_1s, long client_ts, int op_type, string target_url);
    internal record class AddResult(int state, List<object> result_set, long change_ts);
}
