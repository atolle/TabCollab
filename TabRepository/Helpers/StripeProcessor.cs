using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;

namespace TabRepository.Helpers
{
    public class StripeProcessor
    {
        public static Product CreateProduct(IConfiguration configuration)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var options = new ProductCreateOptions
            {
                Name = "TabCollab",
                Type = "service",
            };
            var service = new ProductService();
            return service.Create(options);
        }

        public static Plan CreatePlan(IConfiguration configuration, StripeProduct product)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var options = new PlanCreateOptions
            {
                ProductId = product.Id,
                Nickname = "TabCollab Standard Subscription",
                Interval = "year",
                Currency = "usd",
                Amount = 4999,
            };
            var service = new PlanService();
            return service.Create(options);
        }

        public static Customer CreateCustomer(IConfiguration configuration, ApplicationUser user, string paymentToken)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                SourceToken = paymentToken
            };
            var service = new CustomerService();
            return service.Create(options);
        }

        public static Subscription CreateSubscription(IConfiguration configuration, StripePlan plan, StripeCustomer customer, ApplicationUser user, bool startFromSubscriptionEnd)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var items = new List<SubscriptionItemOption>
            {
                new SubscriptionItemOption { PlanId = plan.Id }
            };

            SubscriptionCreateOptions options = null;

            if (startFromSubscriptionEnd)
            {
                options = new SubscriptionCreateOptions
                {
                    CustomerId = customer.Id,
                    Items = items,
                    TrialEnd = user.SubscriptionExpiration
                };
            }
            else
            {
                options = new SubscriptionCreateOptions
                {
                    CustomerId = customer.Id,
                    Items = items
                };
            }

            var service = new SubscriptionService();
            return service.Create(options);
        }

        public static Subscription CancelSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var service = new SubscriptionService();
            return service.Cancel(subscription.Id, null);
        }

        public static Subscription GetSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var service = new SubscriptionService();
            return service.Get(subscription.Id, null);
        }
    }
}
