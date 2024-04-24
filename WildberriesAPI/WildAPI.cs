using SixLabors.ImageSharp;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace WildberriesAPI
{
    /// <summary>
    /// WildAPI - набор методов для действий с сайтом Wildberriess
    /// </summary>
    public class WildAPI
    {
        /// <summary>
        /// WildAPI - набор методов для действий с сайтом Wildberriess
        /// </summary>
        /// <param name="client">Клиент для отправки запросов на сервер</param>
        /// <param name="deviceId">Id-девайса в формате site_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx. Можно любой набор</param>
        public WildAPI(HttpClient client,  string deviceId) 
        {
            _client = client;
            _deviceId = deviceId;
        }

        /// <summary>
        /// WildAPI - набор методов для действий с сайтом Wildberriess
        /// </summary>
        /// <param name="client">Клиент для отправки запросов на сервер</param>
        /// <param name="deviceId">Id-девайса в формате site_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx. Можно любой набор</param>
        /// <param name="mementor">Загруженные параметры</param>
        public WildAPI(HttpClient client, string deviceId, WildAPIMementor mementor)
        {
            _client = client;
            _userTs = mementor.UserTs;
            _token = mementor.Token;
            _deviceId = deviceId;
        }

        #region publicMethods
        #region addArticle
        /// <summary>
        /// Добавляет товар в корзину по адресу
        /// </summary>
        /// <param name="address">Адрес товара</param>
        /// <param name="count">Количество</param>
        /// <returns>Успех добавления</returns>
        public async Task<bool> AddArticleAsync(string address, int count = 1)
        {
            //Вычленяем артикл из ссылки и передаём его в перегрузку
            var math = _addressRegex.Match(address);
            var nm = math.Groups["nomenclature"].Value;
            return await AddArticleAsync(Convert.ToInt32(nm), count);
        }

        /// <summary>
        /// Добавляет товар в корзину по артиклю
        /// </summary>
        /// <param name="article">Артикл</param>
        /// <param name="count">Количество</param>
        /// <returns></returns>
        public async Task<bool> AddArticleAsync(int article, int count = 1)
        {
            //Сборка запроса
            string postAddress = string.Format($"https://cart-storage-api.wildberries.ru/api/basket/sync?ts={_userTs}&device_id={_deviceId}");
            using HttpRequestMessage mess = new(HttpMethod.Post, postAddress);

            //Для корректного запроса необходим код местоположения, ts пользователя и токен авторизации
            if (_dest == null || _userTs == null || _token == null)
                return false;

            //Добавление токена
            mess.Headers.Authorization = new("Bearer", _token);

            //Получение id комплекта
            var chartId = await GetChartIdAsync(article, _dest.Value);

            //Передача в тело запроса подробностей заказа
            List<BodyContent> content = new() {
                new(chartId, count, article, _userTs.Value, 1, "EX|5|MCS|IT|popular")
            };
            mess.Content = JsonContent.Create(content);

            //Получение ответа
            var response = await _client.SendAsync(mess);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<AddResult>();
                    _userTs = data?.change_ts; //Обновление ts-пользователя
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return true;
            }
            return false;
        }

        #endregion //addArticle

        #region authorizations

        /// <summary>
        /// Запрос на получение каптчи
        /// </summary>
        /// <param name="phone">Номер телефона для получения</param>
        /// <returns>Каптчу. Если каптчи не надо, то возвращает null</returns>
        public async Task<Image?> GetCaptchaAsync(string phone)
        {
            //Формирование запроса на каптчу
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/code");
            mess.Headers.Add("deviceid", _deviceId);
            mess.Content = new StringContent("{\"phone_number\":\"" + phone + "\"}"); //По неизвестной причине сервер получает json-файл с типом text/plain

            //Получение ответа
            var response = await _client.SendAsync(mess);
            if(response.StatusCode == HttpStatusCode.OK )
            {
                var captcha = await response.Content.ReadFromJsonAsync<Captcha>();
                if(captcha.payload?.captcha != null)
                {
                    //Формирование изображения
                    var imageBytes = Convert.FromBase64String(captcha.payload.captcha.Replace("data:image/png;base64,", ""));
                    using MemoryStream ms = new(imageBytes);
                    var res = await Image.LoadAsync(ms);
                    return res;
                }
                return null;
            }
            throw new HttpRequestException();
        }

        /// <summary>
        /// Авторизируется на сайте
        /// </summary>
        /// <param name="sticker">Стикер, полученный из SendCodeAndGetStickerAsync</param>
        /// <param name="code">Код, пришедший на телефон</param>
        /// <returns>Правильный ли код был введён</returns>
        /// <exception cref="HttpRequestException"></exception>
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
                if (res.result == 0)
                {
                    _token = res.payload.access_token;
                    return true;
                }
                return false;
            }
            throw new HttpRequestException();
        }


        /// <summary>
        /// Запрос на отправку кода на телефон. Возвращает стикер, который необходим WriteTokenAsync
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
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

        #endregion //authorizations

        #region settings

        /// <summary>
        /// Обновление/получение ts пользователя. Необходима перед заказом
        /// </summary>
        /// <returns></returns>
        public async Task SetUserTs()
        {
            if (_token == null)
                throw new AuthException();
            var mess = new HttpRequestMessage(HttpMethod.Post, $"https://cart-storage-api.wildberries.ru/api/basket/sync?ts=0&device_id={_deviceId}");
            mess.Headers.Authorization = new("Bearer", _token);
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var distData = await response.Content.ReadFromJsonAsync<SyncData>();
                _userTs = distData?.change_ts;
            }
        }

        /// <summary>
        /// Получение и запись кода местоположения ближайшего отделения по координатам. Только авторизированным пользователям
        /// </summary>
        /// <param name="latitude">ширина</param>
        /// <param name="longitude">долгота</param>
        /// <returns></returns>
        public async Task SetDist(double latitude, double longitude)
        {
            if (_token == null)
                throw new AuthException();
            var mess = new HttpRequestMessage(HttpMethod.Get, $"https://user-geo-data.wildberries.ru/get-geo-info?currency=RUB&latitude={latitude}&longitude={longitude}&locale=ru");
            mess.Headers.Authorization = new("Bearer", _token);
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var distData = await response.Content.ReadFromJsonAsync<DistData>();
                var userIdRegex = new Regex(@"uid=(?<id>\d+)");
                _dest = Convert.ToInt32(userIdRegex.Match(distData.userDataSign).Groups["id"].Value);
            }
        }

        #endregion //settings

        /// <summary>
        /// Возвращает объект с параметрами для сохранения
        /// </summary>
        /// <returns></returns>
        public WildAPIMementor GetMementor() => new(_token, _userTs);

        #endregion //PublicMethods
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

        #region PrivateFields

        private string? _token;
        private long? _userTs;
        private int? _dest;

        private readonly HttpClient _client;
        private readonly string _deviceId;

        private readonly static Regex _addressRegex = new(@"www\.wildberries\.ru/catalog/(?<nomenclature>\d+)/detail\.aspx");

        #endregion //PrivateFields

    }

    public class AuthException : Exception
    {
        public AuthException() : base() { }
        public AuthException(string message) : base(message) { }
    }

    public record class WildAPIMementor(string? Token, long? UserTs);

    internal record class SyncData(int state, List<object> result_set, long change_ts);
    internal record class BodyContent(int chrt_id, int quantity, int cod_1s, long client_ts, int op_type, string target_url);
    internal record class AddResult(int state, List<object> result_set, long change_ts);
}
