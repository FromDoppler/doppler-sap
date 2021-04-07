using Doppler.Sap.Enums;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.BusinessPartner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Sap.Services
{
    public class BusinessPartnerService : IBusinessPartnerService
    {
        private readonly IQueuingService _queuingService;
        private readonly ILogger<BusinessPartnerService> _logger;
        private readonly SapConfig _sapConfig;
        private readonly IEnumerable<IBusinessPartnerValidation> _businessPartnerValidations;

        public BusinessPartnerService(
            IQueuingService queuingService,
            ILogger<BusinessPartnerService> logger,
            IOptions<SapConfig> sapConfig,
            IEnumerable<IBusinessPartnerValidation> businessPartnerValidations)
        {
            _queuingService = queuingService;
            _logger = logger;
            _sapConfig = sapConfig.Value;
            _businessPartnerValidations = businessPartnerValidations;
        }

        public Task CreateOrUpdateBusinessPartner(DopplerUserDto dopplerUser)
        {
            var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(dopplerUser.BillingSystemId);
            if (!GetValidator(sapSystem).IsValid(dopplerUser, sapSystem, _sapConfig, out var userVerificationError))
            {
                throw new ValidationException(userVerificationError);
            }

            _logger.LogInformation($"Add to Task in Queue: {SapTaskEnum.CreateOrUpdateBusinessPartner}");
            _queuingService.AddToTaskQueue(
                new SapTask()
                {
                    TaskType = SapTaskEnum.CreateOrUpdateBusinessPartner,
                    DopplerUser = dopplerUser
                }
            );

            return Task.CompletedTask;
        }

        private IBusinessPartnerValidation GetValidator(string sapSystem)
        {
            // Check if exists billing validator for the sapSystem
            var validator = _businessPartnerValidations.FirstOrDefault(m => m.CanValidateSapSystem(sapSystem));
            if (validator == null)
            {
                _logger.LogError($"Billing Request won't be sent to SAP because the sapSystem '{sapSystem}' is not supported.");
                throw new ArgumentException($"The sapSystem '{sapSystem}' does not have a validator.");
            }

            return validator;
        }
    }
}
