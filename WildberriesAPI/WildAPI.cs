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
        public WildAPI(HttpClient client, string deviceId)
        {
            _client = client;
            _deviceId = deviceId;
            BasketItems = new List<BasketItem>();
        }

        /// <summary>
        /// WildAPI - набор методов для действий с сайтом Wildberriess
        /// </summary>
        /// <param name="client">Клиент для отправки запросов на сервер</param>
        /// <param name="deviceId">Id-девайса в формате site_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx. Можно любой набор</param>
        /// <param name="mementor">Загруженные параметры</param>
        public WildAPI(HttpClient client, string deviceId, WildAPIMementor mementor):this(client, deviceId)
        {
            _userTs = mementor.UserTs;
            Token = mementor.Token;
        }

        #region publicMethods
        #region functions
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
            if (Dest == null || _userTs == null || Token == null)
                return false;

            //Добавление токена
            mess.Headers.Authorization = new("Bearer", Token);

            //Получение id комплекта
            var chartId = await GetChartIdAsync(article, Dest.Value);

            //Передача в тело запроса подробностей заказа
            List<BodyContent> content = new() {
                new(chartId, count, article, 1, "EX|5|MCS|IT|popular")
            };
            mess.Content = JsonContent.Create(content);

            //Получение ответа
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = await response.Content.ReadFromJsonAsync<AddResult>();
                _userTs = data.change_ts; //Обновление ts-пользователя
                return true;
            }
            return false;
        }


        //public async Task<Basket?> GetBasket()
        //{
        //    if (_token == null)
        //        throw new AuthException();
        //    var mess = new HttpRequestMessage(HttpMethod.Post, $"https://ru-basket-api.wildberries.ru/webapi/lk/basket/data");
        //    mess.Headers.Authorization = new("Bearer", _token);

        //    mess.Content = new FormUrlEncodedContent(new[]
        //    {
        //        new KeyValuePair<string, string>("isBuyItNowMode", "false"),
        //        new("items[0][includeInOrder]", "true"),
        //        new("items[0][maxQuantity]", "20"),
        //        new("currency", "RUB"),
        //        new("xInfo", $"appType=1&curr=rub&dest={_dest}&spp=30"),
        //        new("iwcNetLimit", "-1")

        //    });
        //    var response = await _client.SendAsync(mess);
        //    if (response.StatusCode == HttpStatusCode.OK)
        //    {
        //        var root = await response.Content.ReadFromJsonAsync<BasketRoot>();
        //        return root.value.data.basket;
        //    }
        //    return null;
        //}
        #endregion //functions

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
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var captcha = await response.Content.ReadFromJsonAsync<Captcha>();
                if (captcha.payload?.captcha != null)
                {
                    //Формирование изображения
                    var imageBytes = Convert.FromBase64String(captcha.payload.captcha.Replace("data:image/png;base64,", ""));
                    using MemoryStream ms = new(imageBytes);
                    var res = await Image.LoadAsync(ms);
                    return res;
                }
                _sticker = captcha.payload?.sticker;
                return null;
            }
            throw new HttpRequestException();
        }

        /// <summary>
        /// Авторизируется на сайте
        /// </summary>
        /// <param name="code">Код, пришедший на телефон</param>
        /// <returns>Правильный ли код был введён</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<bool> WriteTokenAsync(int code)
        {
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/auth");
            mess.Content = new StringContent("{\"sticker\":\"" + _sticker + "\",\"code\":" + code + "}");
            mess.Headers.Add("deviceid", _deviceId);
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var res = await response.Content.ReadFromJsonAsync<Captcha>();
                if (res.result == 0)
                {
                    Token = res.payload.access_token;
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
        public async Task SendCaptchaCodeAsync(string phone, string captcha)
        {
            using HttpRequestMessage mess = new(HttpMethod.Post, "https://wbx-auth.wildberries.ru/v2/code");
            mess.Headers.Add("deviceid", _deviceId);
            mess.Content = new StringContent("{\"phone_number\":\"" + phone + "\", \"captcha_code\":\"" + captcha.ToLower() + "\"}");
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var sms = await response.Content.ReadFromJsonAsync<Captcha>();
                _sticker = sms.payload.sticker;
            }
            else
                throw new HttpRequestException();
        }

        #endregion //authorizations

        #region settings

        /// <summary>
        /// Обновление/получение ts пользователя. Необходима перед заказом
        /// </summary>
        /// <returns></returns>
        public async Task SyncAsync(bool isReset = false)
        {
            if (Token == null)
                throw new AuthException();
            var ts = isReset ? 0 : _userTs;
            var mess = new HttpRequestMessage(HttpMethod.Post, $"https://cart-storage-api.wildberries.ru/api/basket/sync?ts={ts}&device_id={_deviceId}&remember_me=true");
            mess.Headers.Authorization = new("Bearer", Token);
            mess.Content = JsonContent.Create(new List<object>());
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var distData = await response.Content.ReadFromJsonAsync<SyncData>();
                _userTs = distData.change_ts;
                var items = distData.result_set?.FirstOrDefault();
                if(items != null)
                    BasketItems = items;
            }
        }

        /// <summary>
        /// Получение и запись кода местоположения ближайшего отделения по координатам. Только авторизированным пользователям
        /// </summary>
        /// <param name="latitude">ширина</param>
        /// <param name="longitude">долгота</param>
        /// <returns></returns>
        public async Task SetDistAsync(double latitude, double longitude)
        {
            if (Token == null)
                throw new AuthException();
            var mess = new HttpRequestMessage(HttpMethod.Get, $"https://user-geo-data.wildberries.ru/get-geo-info?currency=RUB&latitude={latitude}&longitude={longitude}&locale=ru");
            mess.Headers.Authorization = new("Bearer", Token);
            var response = await _client.SendAsync(mess);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var distData = await response.Content.ReadFromJsonAsync<DistData>();
                var userIdRegex = new Regex(@"dest=(?<dest>-?\d+)");
                Dest = Convert.ToInt32(userIdRegex.Match(distData.xinfo).Groups["dest"].Value);
            }
        }

        #endregion //settings

        /// <summary>
        /// Возвращает объект с параметрами для сохранения
        /// </summary>
        /// <returns></returns>
        public WildAPIMementor GetMementor() => new(Token, _userTs);

        #endregion //PublicMethods
        public IEnumerable<BasketItem> BasketItems { get; private set; }

        public int? Dest { get; private set; }
        public string? Token;
        private async Task<int> GetChartIdAsync(int article, int dest, string curr = "rub")
        {
            var mess = new HttpRequestMessage(HttpMethod.Get, $"https://card.wb.ru/cards/v2/detail?appType=1&curr={curr}&dest={dest}&spp=30&nm={article}");
            mess.Headers.Authorization = new("Bearer", Token);

            var response = await _client.SendAsync(mess);
            mess.Headers.Authorization = new("Bearer", Token);
            if (response.StatusCode == HttpStatusCode.OK)
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

        private long _userTs = 0;

        private string? _sticker;

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

    public record class WildAPIMementor(string? Token, long UserTs);

    internal record class SyncData(int state, List<List<BasketItem>> result_set, long change_ts);

    public record class BasketItem(int chrt_id, int cod_1s, int quantity, bool is_deleted, string target_url, object? meta_data, long? ts, int? client_ts);
    internal record class BodyContent(int chrt_id, int quantity, int cod_1s, int op_type, string target_url);
    internal record class AddResult(int state, List<object> result_set, long change_ts);
}
