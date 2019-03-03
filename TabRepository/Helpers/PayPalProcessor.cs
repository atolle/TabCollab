using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TabRepository.Helpers
{
    public class PayPalProcessor
    {
        public static async Task<string> GetPayPalToken(IConfiguration configuration)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/oauth2/token"))
                {
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "en_US");

                    var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(configuration["PayPal:SandboxClientID"] + ":" + configuration["PayPal:SandboxSecret"]));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                    request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    var jObject = JObject.Parse(contents);
                    return (string)jObject["access_token"];
                }
            }
        }

        public static async Task<string> CreateBillingPlan(string token)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/payments/billing-plans/"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

                    request.Content = new StringContent(@"
                        {  
                            ""name"": ""TabCollab Subscription Plan"",
                            ""description"": ""Standard TabCollab Subscription Plan."",
                            ""type"": ""INFINITE"",
                            ""payment_definitions"": [
                                {
                                    ""name"": ""Standard TabCollab Paymet"",
                                    ""type"": ""REGULAR"",
                                    ""frequency"": ""YEAR"",
                                    ""frequency_interval"": ""1"",
                                    ""amount"":
                                    {
                                        ""value"": ""50"",
                                        ""currency"": ""USD""
                                    },
                                    ""cycles"": ""0""
                                }],
                            ""merchant_preferences"":
                            {
                                ""return_url"": ""https://localhost:5001/Account/SubscriptionConfirmation"",
                                ""cancel_url"": ""https://localhost:5001/Account/SubscriptionCancel"",
                                ""auto_bill_amount"": ""YES"",
                                ""initial_fail_amount_action"": ""CANCEL"",
                                ""max_fail_attempts"": ""0""
                            }
                        }", Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        public static async Task<bool> ActivateBillingPlan(string token, string id)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), "https://api.sandbox.paypal.com/v1/payments/billing-plans/" + id + "/"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

                    request.Content = new StringContent(@"
                        [{
                            ""op"": ""replace"",
                            ""path"": ""/"",
                            ""value"":
                            {
                                ""state"": ""ACTIVE""
                            }
                        }]", Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        public static async Task<string> CreateBillingAgreement(string token, string planId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/payments/billing-agreements/"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

                    // Escaping {{ }} because of String.Format
                    request.Content = new StringContent(String.Format(@"
                        {{
                            ""name"": ""TabCollab Subscription Agreement"",
                            ""description"": ""Yearly agreement with a regular yearly payment definition."",
                            ""start_date"": ""{0}"",
                            ""plan"":
                            {{
                                ""id"": ""{1}""
                            }},
                            ""payer"":
                            {{
                                ""payment_method"": ""paypal""
                            }}
                        }}", DateTime.UtcNow.ToString("s") + "Z", planId), Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        public static async Task<string> ExecuteBillingAgreement(string requestToken, string agreementToken, string executeURL)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), executeURL))
                {
                    // Set Content-Type header
                    request.Content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestToken);

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }
    }
}
