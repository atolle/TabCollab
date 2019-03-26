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
using TabRepository.Models;

namespace TabRepository.Helpers
{
    public class PayPalProcessor
    {
        public static string GetIdempotencyKey()
        {
            Guid guid = Guid.NewGuid();
            string guidString = Convert.ToBase64String(guid.ToByteArray());
            guidString = guidString.Replace("=", "");
            guidString = guidString.Replace("+", "");

            return guidString;
        }

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

        public static async Task<string> CreateProduct(IConfiguration configuration, string token)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/catalogs/products"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                    request.Headers.Add("PayPal-Request-Id", PayPalProcessor.GetIdempotencyKey());                    

                    request.Content = new StringContent(String.Format(@"
                        {{
                          ""name"": ""TabCollab Subscription"",
                          ""description"": ""TabCollab Yearly Subscription"",
                          ""type"": ""SERVICE"",
                          ""category"": ""SOFTWARE"",
                          ""image_url"": ""{1}"",
                          ""home_url"": ""{0}""
                        }}", configuration["PayPal:SandboxHomeURL"], configuration["PayPal:SandboxImageURL"]), Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        public static async Task<string> CreatePlan(IConfiguration configuration, string token, string productId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/billing/plans"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                    request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                    request.Headers.TryAddWithoutValidation("PayPal-Request-Id", PayPalProcessor.GetIdempotencyKey());

                    request.Content = new StringContent(@"
                        {{
                            ""product_id"": ""{0}"",
                            ""name"": ""Create a flat Plan (Idempotent)"",
                            ""description"": ""Basic plan with fixed and trial definitions"",
                            ""status"": ""ACTIVE"",
                            ""billing_cycles"": [
                            {{
                                ""frequency"": {{
                                    ""interval_unit"": ""YEAR"",
                                    ""interval_count"": 1
                                }},
                                ""tenure_type"": ""REGULAR"",
                                ""sequence"": 2,
                                ""total_cycles"": 12,
                                ""pricing_scheme"": {{
                                    ""fixed_price"": {{
                                    ""value"": ""49.99"",
                                    ""currency_code"": ""USD""
                                    }}
                                }}
                            }}
                            ],
                            ""payment_preferences"": {{
                                ""payment_failure_threshold"": 3
                            }}
                        }}", Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        public static async Task<string> CreateSubscription(IConfiguration configuration, string token, string planId, ApplicationUser user)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/billing/subscriptions"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                    request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                    request.Headers.TryAddWithoutValidation("PayPal-Request-Id", PayPalProcessor.GetIdempotencyKey());

                    request.Content = new StringContent(String.Format(@"
                        {{
                            ""plan_id"": ""{0}"",
                            ""start_time"": ""{1}"",
                            ""subscriber"": {{
                                ""name"": {{
                                    ""given_name"": ""{2}"",
                                    ""surname"": ""{3}""
                                }},
                                ""email_address"": ""{4}"",
                            }},
                            ""auto_renewal"": true,
                            ""application_context"": {{
                                ""brand_name"": ""{5}"",
                                ""locale"": ""en-US"",
                                ""user_action"": ""SUBSCRIBE"",
                                ""payment_method"": {{
                                    ""payer_selected"": ""PAYPAL"",
                                    ""payee_preferred"": ""IMMEDIATE_PAYMENT_REQUIRED""
                                }},
                                ""return_url"": ""{6}/Account/SubscriptionConfirmation"",
                                ""cancel_url"": ""{6}/Account/SubscriptionCancel"",
                            }}
                        }}", 
                        planId, 
                        DateTime.UtcNow.AddDays(1).ToString("s") + "Z",
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        "TabCollab",
                        configuration["PayPal:SandboxURL"]),
                        Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(request);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        public static async Task<string> GetSubscription(string requestToken, string subscriptionId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.sandbox.paypal.com/v1/billing/subscriptions/" + subscriptionId))
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

        public static async Task<bool> CancelSubscription(string requestToken, string subscriptionId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/billing/subscriptions/" + subscriptionId + "/cancel"))
                {
                    // Set Content-Type header
                    request.Content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestToken);

                    var response = await httpClient.SendAsync(request);

                    // 204 indicates success per PayPal API
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        public static async Task<bool> ActivateSubscription(string requestToken, string subscriptionId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/billing/subscriptions/" + subscriptionId + "/activate"))
                {
                    // Set Content-Type header
                    request.Content = new StringContent("", Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestToken);

                    var response = await httpClient.SendAsync(request);

                    // 204 indicates success per PayPal API
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        #region Deprecated Billing Agreement PayPal API

        public static async Task<string> CreateBillingPlan(IConfiguration configuration, string token)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/payments/billing-plans/"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

                    request.Content = new StringContent(String.Format(@"
                        {{  
                            ""name"": ""TabCollab Subscription Plan"",
                            ""description"": ""Standard TabCollab Subscription Plan."",
                            ""type"": ""INFINITE"",
                            ""payment_definitions"": [
                                {{
                                    ""name"": ""Standard TabCollab Paymet"",
                                    ""type"": ""REGULAR"",
                                    ""frequency"": ""YEAR"",
                                    ""frequency_interval"": ""1"",
                                    ""amount"":
                                    {{
                                        ""value"": ""50"",
                                        ""currency"": ""USD""
                                    }},
                                    ""cycles"": ""0""
                                }}],
                            ""merchant_preferences"":
                            {{
                                ""return_url"": ""{0}/Account/SubscriptionConfirmation"",
                                ""cancel_url"": ""{0}/Account/SubscriptionCancel"",
                                ""auto_bill_amount"": ""YES"",
                                ""initial_fail_amount_action"": ""CANCEL"",
                                ""max_fail_attempts"": ""0""
                            }}
                        }}", configuration["PayPal:SandboxURL"]), Encoding.UTF8, "application/json");

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
                        }}", DateTime.UtcNow.AddDays(1).ToString("s") + "Z", planId), Encoding.UTF8, "application/json");

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

        public static async Task<bool> CancelBillingAgreement(string requestToken, string agreementId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/payments/billing-agreements/" + agreementId + "/cancel"))
                {
                    // Set Content-Type header
                    request.Content = new StringContent(@"
                        {  
                            ""note"": ""Canceling TabCollab subscription.""
                        }", Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestToken);

                    var response = await httpClient.SendAsync(request);

                    // 204 indicates success per PayPal API
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        public static async Task<bool> ReactivateBillingAgreement(string requestToken, string agreementId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.sandbox.paypal.com/v1/payments/billing-agreements/" + agreementId + "/re-activate"))
                {
                    // Set Content-Type header
                    request.Content = new StringContent(@"
                        {  
                            ""note"": ""Reactivating TabCollab subscription.""
                        }", Encoding.UTF8, "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestToken);

                    var response = await httpClient.SendAsync(request);

                    // 204 indicates success per PayPal API
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        public static async Task<string> GetBillingAgreement(string requestToken, string agreementId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.sandbox.paypal.com/v1/payments/billing-agreements/" + agreementId))
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

        #endregion
    }
}
