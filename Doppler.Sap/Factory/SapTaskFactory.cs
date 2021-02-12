using System;
using System.Threading.Tasks;
using Doppler.Sap.Enums;
using Doppler.Sap.Models;

namespace Doppler.Sap.Factory
{
    public class SapTaskFactory : ISapTaskFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SapTaskFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<SapTaskResult> CreateHandler(SapTask sapTask)
        {
            switch (sapTask.TaskType)
            {
                case SapTaskEnum.CurrencyRate:
                    return await ((SetCurrencyRateHandler)_serviceProvider.GetService(typeof(SetCurrencyRateHandler))).Handle(sapTask);
                case SapTaskEnum.BillingRequest:
                case SapTaskEnum.UpdateBilling:
                    return await ((BillingRequestHandler)_serviceProvider.GetService(typeof(BillingRequestHandler))).Handle(sapTask);
                case SapTaskEnum.CreateOrUpdateBusinessPartner:
                    return await ((CreateOrUpdateBusinessPartnerHandler)_serviceProvider.GetService(typeof(CreateOrUpdateBusinessPartnerHandler))).Handle(sapTask);
                case SapTaskEnum.CreateCreditNote:
                    return await ((CreditNoteHandler)_serviceProvider.GetService(typeof(CreditNoteHandler))).Handle(sapTask);
                case SapTaskEnum.UpdateCreditNote:
                    return await ((CreditNoteHandler)_serviceProvider.GetService(typeof(CreditNoteHandler))).UpdatePaymentStatusHandle(sapTask);
                case SapTaskEnum.CancelCreditNote:
                    return await ((CreditNoteHandler)_serviceProvider.GetService(typeof(CreditNoteHandler))).CancelCreditNoteHandle(sapTask);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
