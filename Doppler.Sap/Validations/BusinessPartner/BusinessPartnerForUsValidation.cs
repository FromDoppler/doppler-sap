using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Sap.Validations.BusinessPartner
{
    public class BusinessPartnerForUsValidation : IBusinessPartnerValidation
    {
        private const string _sapSystemSupported = "US";
        private readonly ILogger<BusinessPartnerForUsValidation> _logger;

        public BusinessPartnerForUsValidation(ILogger<BusinessPartnerForUsValidation> logger)
        {
            _logger = logger;
        }

        public bool CanValidateSapSystem(string sapSystem)
        {
            return _sapSystemSupported == sapSystem;
        }

        public bool IsValid(DopplerUserDto dopplerUser, string sapSystem, SapConfig sapConfig, out string error)
        {
            if (!sapConfig.SapServiceConfigsBySystem.ContainsKey(sapSystem))
            {
                _logger.LogInformation($"{dopplerUser.Email} won't be sent to SAP because it's not from {string.Join(", ", sapConfig.SapServiceConfigsBySystem.Select(x => x.Key))}");
                error = "Invalid billing system value.";

                return false;
            }

            if (string.IsNullOrEmpty(dopplerUser.FirstName) && string.IsNullOrEmpty(dopplerUser.LastName))
            {
                _logger.LogInformation($"{dopplerUser.Email} won't be sent to SAP because it doesn't have a first name or last name");
                error = "Invalid first name or last name.";
                return false;
            }

            if (!dopplerUser.PlanType.HasValue)
            {
                _logger.LogInformation($"{dopplerUser.Email} won't be sent to SAP because it doesn't have a plan type id");
                error = "Invalid plan type value.";

                return false;
            }

            error = string.Empty;

            return true;
        }
    }
}
