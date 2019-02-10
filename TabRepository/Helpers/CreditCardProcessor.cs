using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;
using Microsoft.Extensions.Configuration;

namespace TabRepository.Helpers
{
    public static class CreditCardProcessor
    {
        public static CreditCardTransaction ChargeCreditCard(string cardNumber, string expirationDate, IConfiguration configuration)
        {
            try
            {
                ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX;

                // define the merchant information (authentication / transaction id)
                ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication = new merchantAuthenticationType()
                {
                    name = configuration["Authorize.NET:SandboxAPIKey"],
                    ItemElementName = ItemChoiceType.transactionKey,
                    Item = configuration["Authorize.NET:SandboxTransactionKey"],
                };

                var creditCard = new creditCardType
                {
                    cardNumber = cardNumber,
                    expirationDate = expirationDate
                };

                //standard api call to retrieve response
                var paymentType = new paymentType { Item = creditCard };

                var transactionRequest = new transactionRequestType
                {
                    transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),   // charge the card
                    amount = 10.00m,
                    payment = paymentType
                };

                var request = new createTransactionRequest { transactionRequest = transactionRequest };

                // instantiate the contoller that will call the service
                var controller = new createTransactionController(request);
                controller.Execute();

                // get the response from the service (errors contained if any)
                var response = controller.GetApiResponse();

                CreditCardTransaction creditCardTransaction = new CreditCardTransaction();

                if (response.messages.resultCode == messageTypeEnum.Ok)
                {
                    if (response.transactionResponse != null)
                    {
                        creditCardTransaction.Success = true;
                        creditCardTransaction.AuthCode = response.transactionResponse.authCode;
                        creditCardTransaction.TransactionID = response.transactionResponse.transId;
                        return creditCardTransaction;
                    }
                    else
                    {
                        creditCardTransaction.Success = false;
                        return creditCardTransaction;
                    }
                }
                else
                {
                    creditCardTransaction.Success = false;
                    creditCardTransaction.ErrorCode = response.messages.message[0].code;
                    creditCardTransaction.ErrorMessage = response.messages.message[0].text;

                    if (response.transactionResponse != null)
                    {
                        creditCardTransaction.TransactionErrorCode = response.transactionResponse.errors[0].errorCode;
                        creditCardTransaction.TransactionErrorMessage = response.transactionResponse.errors[0].errorText;
                    }

                    return creditCardTransaction;
                }
            }
            catch (Exception e)
            {
                return new CreditCardTransaction();
            }
        }
    }

    public class CreditCardTransaction
    {
        public CreditCardTransaction()
        {

        }

        public bool Success
        {
            get
            {
                return _success;
            }
            set
            {
                _success = value;
            }
        }

        public string AuthCode
        {
            get
            {
                return _authCode;
            }
            set
            {
                _authCode = value;
            }
        }

        public string TransactionID
        {
            get
            {
                return _transactionID;
            }
            set
            {
                _transactionID = value;
            }
        }

        public string TransactionErrorCode
        {
            get
            {
                return _transactionErrorCode;
            }
            set
            {
                _transactionErrorCode = value;
            }
        }

        public string TransactionErrorMessage
        {
            get
            {
                return _transactionErrorMessage;
            }
            set
            {
                _transactionErrorMessage = value;
            }
        }

        public string ErrorCode
        {
            get
            {
                return _errorCode;
            }
            set
            {
                _errorCode = value;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                _errorMessage = value;
            }
        }

        private bool _success = false;
        private string _authCode = "";
        private string _transactionID = "";
        private string _transactionErrorCode = "";
        private string _transactionErrorMessage = "";
        private string _errorCode = "";
        private string _errorMessage = "";
    }
}
