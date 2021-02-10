using Doppler.Sap.Models;
using Microsoft.Extensions.Logging;
using System;

namespace Doppler.Sap.Validations.Billing
{
    public class BillingForUsValidation : IBillingValidation
    {
        private const string _sapSystemSupported = "US";
        private readonly ILogger<BillingForUsValidation> _logger;

        public BillingForUsValidation(ILogger<BillingForUsValidation> logger)
        {
            _logger = logger;
        }

        public bool CanCreate(SapBusinessPartner sapBusinessPartner, SapSaleOrderModel billingRequest)
        {
            if (sapBusinessPartner == null)
            {
                _logger.LogError($"Failed at generating billing request for user: {billingRequest.UserId}.");
                return false;
            }

            return true;
        }

        public bool CanUpdate(SapSaleOrderInvoiceResponse saleOrder, SapSaleOrderModel billingRequest)
        {
            if (saleOrder == null)
            {
                _logger.LogError($"Failed at updating billing for invoice: {billingRequest.InvoiceId}.");
                return false;
            }

            return true;
        }

        public bool CanValidateSapSystem(string sapSystem)
        {
            return _sapSystemSupported == sapSystem;
        }

        public void ValidateRequest(BillingRequest dopplerBillingRequest)
        {
            if (dopplerBillingRequest.Id.Equals(default))
            {
                _logger.LogError("Billing Request won't be sent to SAP because it doesn't have the user's Id.");
                throw new ArgumentException("Value can not be null", "Id");
            }
        }

        public void ValidateUpdateRequest(UpdatePaymentStatusRequest updateBillingRequest)
        {
            if (updateBillingRequest.InvoiceId.Equals(default))
            {
                _logger.LogError("Billing Request won't be sent to SAP because it doesn't have the invoice's Id.");
                throw new ArgumentException("Value can not be null", "InvoiceId");
            }
        }

        public void ValidateCreditNoteRequest(CreditNoteRequest creditNoteRequest)
        {
            if (creditNoteRequest.InvoiceId.Equals(default))
            {
                _logger.LogError("Create Credit Note Request won't be sent to SAP because it doesn't have the invoice's Id.");
                throw new ArgumentException("Value can not be null", "InvoiceId");
            }

            if (creditNoteRequest.BillingSystemId.Equals(default))
            {
                _logger.LogError("Create Credit Note Request won't be sent to SAP because it doesn't have the billing system's Id.");
                throw new ArgumentException("Value can not be null", "BillingSystemId");
            }

            if (creditNoteRequest.ClientId.Equals(default))
            {
                _logger.LogError("Create Credit Note Request won't be sent to SAP because it doesn't have the client's Id.");
                throw new ArgumentException("Value can not be null", "ClientId");
            }
        }

        public void ValidateUpdateCreditNotePaymentStatusRequest(UpdateCreditNotePaymentStatusRequest updateCreditNotePaymentStatusRequest)
        {
            if (updateCreditNotePaymentStatusRequest.CreditNoteId.Equals(default))
            {
                _logger.LogError("Update credit note update payment request won't be sent to SAP because it doesn't have the invoice's Id.");
                throw new ArgumentException("Value can not be null", "InvoiceId");
            }

            if (updateCreditNotePaymentStatusRequest.BillingSystemId.Equals(default))
            {
                _logger.LogError("Update credit note update payment won't be sent to SAP because it doesn't have the billing system's Id.");
                throw new ArgumentException("Value can not be null", "BillingSystemId");
            }
        }

        public void ValidateCancelCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest)
        {
            if (cancelCreditNoteRequest.CreditNoteId.Equals(default))
            {
                _logger.LogError("Cancel Credit Note Request won't be sent to SAP because it doesn't have the credit note's Id.");
                throw new ArgumentException("Value can not be null", "CreditNoteId");
            }

            if (cancelCreditNoteRequest.BillingSystemId.Equals(default))
            {
                _logger.LogError("Cancel Credit Note Request won't be sent to SAP because it doesn't have the billing system's Id.");
                throw new ArgumentException("Value can not be null", "BillingSystemId");
            }
        }
    }
}
