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
            StripeConfiguration.SetApiKey(_stripeSecret);

            var options = new ProductCreateOptions
            {
                Name = "TabCollab",
                Type = "service",
            };
            var service = new ProductService();
            return service.Create(options);
        }

        public Plan CreatePlan(IConfiguration configuration, StripeProduct product, SubscriptionRecurrence recurrence)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);            

            var options = new PlanCreateOptions
            {
                ProductId = product.Id,
                Nickname = $"TabCollab Pro {recurrence.ToString()} Subscription",
                Interval = recurrence == SubscriptionRecurrence.Monthly ? "month" : "year",
                Currency = "usd",
                Amount = recurrence == SubscriptionRecurrence.Monthly ? 499 : 4999,
            };

            var service = new PlanService();
            return service.Create(options);
        }

        public Customer CreateCustomer(IConfiguration configuration, ApplicationUser user, string paymentToken)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                SourceToken = paymentToken
            };
            var service = new CustomerService();
            return service.Create(options);
        }

        public Subscription CreateSubscription(IConfiguration configuration, StripePlan plan, StripeCustomer customer, ApplicationUser user)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var items = new List<SubscriptionItemOption>
            {
                new SubscriptionItemOption { PlanId = plan.Id }
            };

            SubscriptionCreateOptions options = null;

            options = new SubscriptionCreateOptions
            {
                CustomerId = customer.Id,
                Items = items                
            };

            options.AddExtraParam("enable_incomplete_payments", "false");

            var service = new SubscriptionService();
            return service.Create(options);
        }

        public Subscription CancelSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };
            return service.Update(subscription.Id, options);
        }

        public Subscription ActivateSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };
            return service.Update(subscription.Id, options);
        }

        public Subscription GetSubscription(IConfiguration configuration, StripeSubscription subscription)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var service = new SubscriptionService();
            return service.Get(subscription.Id, null);
        }

        public Customer GetCustomer(IConfiguration configuration, StripeCustomer customer)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var service = new CustomerService();
            return service.Get(customer.Id, null);
        }

        public Customer UpdateCustomerPayment(IConfiguration configuration, StripeCustomer customer, string paymentToken)
        {
            StripeConfiguration.SetApiKey(_stripeSecret);

            var options = new CustomerUpdateOptions
            {
                SourceToken = paymentToken
            };
            var service = new CustomerService();
            return service.Update(customer.Id, options);
        }
    }
}
