using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using TabRepository.Data;
using TabRepository.Helpers;

namespace TabRepository.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/StripeWebhook")]
    public class StripeWebhookController : Controller
    {
        private ApplicationDbContext _context;
        private IConfiguration _configuration;
        private IHostingEnvironment _env;
        private StripeProcessor _stripeProcessor;

        public StripeWebhookController(ApplicationDbContext context, IConfiguration configuration, IHostingEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _stripeProcessor = new StripeProcessor(_env, _configuration);
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        [HttpPost]
        public IActionResult Index()
        {
            var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            var stripeEvent = EventUtility.ParseEvent(json);

            string subscriptionId = "";

            switch (stripeEvent.Type)
            {
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    subscriptionId = (stripeEvent.Data.Object as Subscription).Id;
                    break;
                case "invoice.payment_succeeded":
                case "invoice.payment_failed":
                    subscriptionId = (stripeEvent.Data.Object as Invoice).SubscriptionId;
                    break;
            }
            
            var subscriptionInDb = _context.StripeSubscriptions.Include(s => s.Customer).Where(s => s.Id == subscriptionId).FirstOrDefault();

            // We need to make sure that the subscription for this message is still active
            if (subscriptionInDb != null)
            {
                var userInDb = _context.Users.Where(u => u.Id == subscriptionInDb.Customer.UserId).FirstOrDefault();
                var subscription = _stripeProcessor.GetSubscription(_configuration, subscriptionInDb);

                if (subscription.Status.ToLower() == "active")
                {
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Pro;
                }
                else
                {
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
                }

                _context.SaveChanges();
            }

            return new StatusCodeResult(StatusCodes.Status200OK);
        }
    }
}