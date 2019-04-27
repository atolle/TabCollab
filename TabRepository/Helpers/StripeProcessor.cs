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
        public static Product CreateProduct(IConfiguration configuration, string token)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var options = new ProductCreateOptions
            {
                Name = "TabCollab",
                Type = "service",
            };
            var service = new ProductService();
            Product product = service.Create(options);

            return product;
        }

        public static Plan CreatePlan(IConfiguration configuration, Product product)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var options = new PlanCreateOptions
            {
                ProductId = product.Id,
                Nickname = "TabCollab Standard Subscription",
                Interval = "month",
                Currency = "usd",
                Amount = 50,
            };
            var service = new PlanService();
            Plan plan = service.Create(options);

            return plan;
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
            Customer customer = service.Create(options);

            return customer;
        }

        public static Subscription CreateSubscription(IConfiguration configuration, Plan plan, Customer customer)
        {
            StripeConfiguration.SetApiKey(configuration["Stripe:TestSecret"]);

            var items = new List<SubscriptionItemOption>
            {
                new SubscriptionItemOption { PlanId = plan.Id }
            };

            var options = new SubscriptionCreateOptions
            {
                CustomerId = customer.Id,
                Items = items,
            };
            var service = new SubscriptionService();
            Subscription subscription = service.Create(options);

            return subscription;
        }
    }
}
