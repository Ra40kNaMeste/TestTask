using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace WildberriesAPI
{
    public class WildAPI:IDisposable
    {
        public WildAPI(HttpClient client, string token, int userTs, int chartId, string deviceId) 
        {
            _client = client;
            _token = token;
            _userTs = userTs;
            _deviceId = deviceId;
            _chartId = chartId;
        }

        public bool AddArticle(string address, int count = 1)
        {
            var math = _addressRegex.Match(address);
            var nm = math.Groups["nomenclature"].Value;
            return AddArticle(Convert.ToInt32(nm), count);
        }
        public bool AddArticle(int article, int count = 1)
        {
            string postAddress = string.Format($"https://cart-storage-api.wildberries.ru/api/basket/sync?ts={_userTs}&device_id={_deviceId}");
            using HttpRequestMessage mess = new(HttpMethod.Post, postAddress);

            mess.Headers.Authorization = new("Bearer", _token);
            mess.Headers.Referrer = new($"https://www.wildberries.ru/catalog/{article}/detail.aspx");


            List<BodyContent> content = new() {
                new(_chartId, count, article, _userTs, 1, "EX|5|MCS|IT|popular")
            };

            mess.Content = JsonContent.Create(content);

            var response = _client.Send(mess);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private readonly string _token;

        private readonly HttpClient _client;
        private readonly int _userTs;
        private readonly int _chartId;
        private readonly string _deviceId;

        private readonly static Regex _addressRegex = new(@"www\.wildberries\.ru/catalog/(?<nomenclature>\d+)/detail\.aspx");
    }
    internal record class BodyContent(long chrt_id, int quantity, int cod_1s, long client_ts, int op_type, string target_url);
}
