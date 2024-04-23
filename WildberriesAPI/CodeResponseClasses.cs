using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildberriesAPI
{
    internal record class Payload (string captcha, string access_token, string auth_method, int ttl, string sticker);
    internal record class Captcha (int result, string error, Payload payload);
}
