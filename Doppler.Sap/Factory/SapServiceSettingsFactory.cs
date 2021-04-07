using Doppler.Sap.Mappers.BusinessPartner;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Doppler.Sap.Factory
{
    public class SapServiceSettingsFactory : ISapServiceSettingsFactory
    {
        private readonly SapConfig _sapConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SapTaskHandler> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEnumerable<IBusinessPartnerMapper> _businessPartnerMappers;

        public SapServiceSettingsFactory(
            IOptions<SapConfig> sapConfig,
            ILogger<SapTaskHandler> logger,
            IHttpClientFactory httpClientFactory,
            IDateTimeProvider dateTimeProvider,
            IEnumerable<IBusinessPartnerMapper> businessPartnerMappers)
        {
            _sapConfig = sapConfig.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dateTimeProvider = dateTimeProvider;
            _businessPartnerMappers = businessPartnerMappers;
        }

        public ISapTaskHandler CreateHandler(string sapSystem)
        {
            // Check if exists settings for the countryCode
            if (!_sapConfig.SapServiceConfigsBySystem.TryGetValue(sapSystem, out var serviceSettings))
            {
                throw new ArgumentException($"The sapSystem '{sapSystem}' does not have a settings.", sapSystem);
            }

            // Check if exists business partner mapper for the countryCode
            var mapper = _businessPartnerMappers.FirstOrDefault(m => m.CanMapCountry(sapSystem));
            if (mapper == null)
            {
                throw new ArgumentException($"The sapSystem '{sapSystem}' does not have a mapper.", sapSystem);
            }

            return new SapTaskHandler(_sapConfig, _logger, _httpClientFactory, _dateTimeProvider, serviceSettings, mapper);
        }
    }
}
