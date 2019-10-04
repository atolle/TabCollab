using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Helpers
{
    public class StripeProcessor
    {
        private IHostingEnvironment _env;
        private IConfiguration _configuration;
        private string _stripeSecret;

        public StripeProcessor(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;

            if (_env.IsDevelopment())
            {
                _stripeSecret = configuration["Stripe:TestSecret"];
            }
            else
            {
                _stripeSecret = configuration["Stripe:ProductionSecret"];
            }
        }

        public Product CreateProduct(IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var options = new ProductCreateOptions
            {
                Name = "TabCollab",
                Type = "service",
            };
            var service = new ProductService();
            return service.Create(options);
        }

        public Plan CreatePlan(IConfiguration configuration, StripeProduct product, SubscriptionInterval interval)
        {
            StripeConfiguration.ApiKey = _stripeSecret;            

            var options = new PlanCreateOptions
            {
                Product = product.Id,
                Nickname = $"TabCollab Pro {interval.ToString()} Subscription",
                Interval = interval == SubscriptionInterval.Monthly ? "month" : "year",
                Currency = "usd",
                Amount = interval == SubscriptionInterval.Monthly ? 499 : 4999,
            };

            var service = new PlanService();
            return service.Create(options);
        }

        public Customer CreateCustomer(IConfiguration configuration, ApplicationUser user, string paymentToken)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                Source = paymentToken
            };
            var service = new CustomerService();
            return service.Create(options);
        }

        public TaxRate CreateTaxRate(IConfiguration configuration, ApplicationUser user, string description, string jurisdiction, decimal? percentage)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var options = new TaxRateCreateOptions
            {
                DisplayName = "Sales Tax",
                Description = description,
                Jurisdiction = jurisdiction,
                Percentage = percentage,
                Inclusive = false,
            };
            var service = new TaxRateService();
            return service.Create(options);
        }

        public Subscription CreateSubscription(IConfiguration configuration, StripePlan plan, StripeCustomer customer, ApplicationUser user, TaxRate taxRate)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var items = new List<SubscriptionItemOption>
            {
                new SubscriptionItemOption { PlanId = plan.Id }
            };

            SubscriptionCreateOptions options = null;

            if (taxRate != null)
            {
                options = new SubscriptionCreateOptions
                {
                    CustomerId = customer.Id,
                    Items = items,
                    DefaultTaxRates = new List<string> { taxRate.Id }
                    // Uncomment TrialEnd for Stripe testing
                    //,TrialEnd = DateTime.Now.Add(new TimeSpan(0, 1, 0)).ToUniversalTime()
                };
            }
            else
            {
                options = new SubscriptionCreateOptions
                {
                    CustomerId = customer.Id,
                    Items = items
                    // Uncomment TrialEnd for Stripe testing
                    //,TrialEnd = DateTime.Now.Add(new TimeSpan(0, 1, 0)).ToUniversalTime()
                };
            }

            options.AddExtraParam("enable_incomplete_payments", "false");

            var service = new SubscriptionService();
            return service.Create(options);
        }

        public Subscription UpdateSubscription(IConfiguration configuration, string subscriptionId, TaxRate taxRate)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            SubscriptionUpdateOptions options = null;

            if (taxRate != null)
            {
                options = new SubscriptionUpdateOptions
                {
                    DefaultTaxRates = new List<string> { taxRate.Id }
                    // Uncomment TrialEnd for Stripe testing
                    //,TrialEnd = DateTime.Now.Add(new TimeSpan(0, 1, 0)).ToUniversalTime()
                };
            }

            var service = new SubscriptionService();
            return service.Update(subscriptionId, options);
        }

        public Subscription CancelSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };
            return service.Update(subscription.Id, options);
        }

        public Subscription ActivateSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };
            return service.Update(subscription.Id, options);
        }

        public Subscription GetSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new SubscriptionService();
            return service.Get(subscription.Id, null);
        }

        public Customer GetCustomer(IConfiguration configuration, StripeCustomer customer)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new CustomerService();
            return service.Get(customer.Id, null);
        }

        public Customer GetCustomer(IConfiguration configuration, string customerId)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new CustomerService();
            return service.Get(customerId, null);
        }

        public Customer UpdateCustomerPayment(IConfiguration configuration, StripeCustomer customer, string paymentToken)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var options = new CustomerUpdateOptions
            {
                Source = paymentToken
            };
            var service = new CustomerService();
            return service.Update(customer.Id, options);
        }

        public Charge GetCharge(IConfiguration configuration, string chargeId)
        {
            StripeConfiguration.ApiKey = _stripeSecret;

            var service = new ChargeService();
            return service.Get(chargeId, null);
        }
    }
}
