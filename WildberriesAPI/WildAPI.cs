using SixLabors.ImageSharp;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace WildberriesAPI
{
    public class WildAPI:IDisposable
    {
        public WildAPI(HttpClient client, long userTs, int dest, string deviceId) 
        {
            _client = client;
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

            var chartId = await GetChartIdAsync(article, _dest);
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

        /// <summary>
        /// Phone 88005553535
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public async Task<Image> GetCaptchaAsync(string phone)
        {
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/code");
            mess.Headers.Add("deviceid", _deviceId);
            mess.Content = new StringContent("{\"phone_number\":\"" + phone + "\"}");
            var response = await _client.SendAsync(mess);
            if(response.StatusCode == HttpStatusCode.OK )
            {
                var captcha = await response.Content.ReadFromJsonAsync<Captcha>();
                var imageBytes = Convert.FromBase64String(captcha.payload.captcha.Replace("data:image/png;base64,", ""));
                using MemoryStream ms = new(imageBytes);
                var res = await Image.LoadAsync(ms);
                return res;
            }
            throw new HttpRequestException();
        }

        public async Task<string> SendCodeAndGetStickerAsync(string phone, string captcha)
        {
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/code");
            mess.Headers.Add("deviceid", _deviceId);
            mess.Content = new StringContent("{\"phone_number\":\"" + phone + "\", \"captcha_code\":\"" + captcha.ToLower() + "\"}");
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var sms = await response.Content.ReadFromJsonAsync<Captcha>();
                return sms.payload.sticker;
            }
            throw new HttpRequestException();
        }

        public async Task<bool> WriteTokenAsync(string sticker, int code)
        {
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/auth");
            var str = "{\"sticker\":\"" + sticker + "\",\"code\":" + code + "}";
            mess.Content = new StringContent("{\"sticker\":\"" + sticker + "\",\"code\":" + code + "}");
            mess.Headers.Add("deviceid", _deviceId);
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var res = await response.Content.ReadFromJsonAsync<Captcha>();
                _token = res.payload.access_token;
                return true;
            }
            throw new HttpRequestException();
        }

        private async Task<int> GetChartIdAsync(int article, int dest, string curr = "rub")
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

        private string? _token;

        private readonly HttpClient _client;
        private long _userTs;
        private readonly int _dest;
        private readonly string _deviceId;

        private readonly static Regex _addressRegex = new(@"www\.wildberries\.ru/catalog/(?<nomenclature>\d+)/detail\.aspx");
    }
    internal record class BodyContent(int chrt_id, int quantity, int cod_1s, long client_ts, int op_type, string target_url);
    internal record class AddResult(int state, List<object> result_set, long change_ts);
}
