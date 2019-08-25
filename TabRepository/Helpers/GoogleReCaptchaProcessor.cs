using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TabRepository.Helpers
{
    public static class GoogleReCaptchaProcessor
    {
        public static bool ReCaptchaPassed(IConfiguration configuration, string gReCaptchaResponse)
        {
            HttpClient httpClient = new HttpClient();            
            var res = httpClient.GetAsync("https://www.google.com/recaptcha/api/siteverify?secret=" + configuration["ReCaptchaV3:SecretKey"] + $"&response={gReCaptchaResponse}").Result;
            if (res.StatusCode != HttpStatusCode.OK)
                return false;

            string JSONres = res.Content.ReadAsStringAsync().Result;
            dynamic JSONdata = JObject.Parse(JSONres);
            if (JSONdata.success != "true")
                return false;

            return true;
        }
    }
}
