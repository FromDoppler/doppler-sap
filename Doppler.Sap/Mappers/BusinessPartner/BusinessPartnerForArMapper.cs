using Doppler.Sap.Enums;
using Doppler.Sap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace Doppler.Sap.Mappers.BusinessPartner
{
    public class BusinessPartnerForArMapper : BusinessPartnerMapper, IBusinessPartnerMapper
    {
        private const string countryCodeSupported = "AR";
        private readonly Dictionary<int, int> dopplerGroupCodes = new Dictionary<int, int>
        {
            {2,104}, //alto volumen
            {3,105}, //prepago
            {4,100}  //contactos
        };

        private readonly Dictionary<int, int> clientManagerGroupCodes = new Dictionary<int, int>
        {
            {1,106}, //Client Manager
            {2,114}, //Client Manager Resel
        };

        private readonly Dictionary<int, int> relayGroupCodes = new Dictionary<int, int>
        {
            {5,115}, //Relay
        };

        /// <summary>
        /// Key: PlanType; Value: GroupCode
        /// </summary>
        protected override Dictionary<int, int> DopplerGroupCodes { get => dopplerGroupCodes; }

        /// <summary>
        /// Key: ClientManagerType; Value: GroupCode
        /// </summary>
        protected override Dictionary<int, int> ClientManagerGroupCodes { get => clientManagerGroupCodes; }

        /// <summary>
        /// Key: PlanType; Value: GroupCode
        /// </summary>
        protected override Dictionary<int, int> RelayGroupCodes { get => relayGroupCodes; }

        public bool CanMapCountry(string countryCode)
        {
            return countryCodeSupported == countryCode;
        }

        public string MapDopplerUserIdToSapBusinessPartnerId(int id, int planType)
        {
            var planTypeCode = Dictionary.UserPlanTypesDictionary.TryGetValue(planType, out var code) ? code
                : throw new ArgumentException("Parameter does not match with any plan type.", $"userPlanTypeId");

            return $"{planTypeCode}{id:00000000000}.";
        }

        public SapBusinessPartner MapDopplerUserToSapBusinessPartner(DopplerUserDto dopplerUser, string cardCode, SapBusinessPartner fatherBusinessPartner)
        {
            var newBusinessPartner = new SapBusinessPartner
            {
                CardCode = cardCode,
                CardName = $"{dopplerUser.FirstName} {dopplerUser.LastName}".ToUpper(),
                GroupCode = MapGroupCode(dopplerUser),
                PayTermsGrpCode = dopplerUser.PaymentMethod == (int)PaymentMethodEnum.MP ? (int)PayTermsGroupEnum.MP : (int)PayTermsGroupEnum.DEFAULT,
                ContactPerson = new MailAddress((dopplerUser.BillingEmails != null && dopplerUser.BillingEmails[0] != String.Empty) ?
                dopplerUser.BillingEmails[0].ToLower() :
                    dopplerUser.Email.ToLower()).User,
                EmailAddress = dopplerUser.Email.ToLower(),
                Phone1 = dopplerUser.PhoneNumber ?? "",
                FederalTaxID = dopplerUser.FederalTaxID.Replace("-", ""),
                U_B1SYS_VATCtg = dopplerUser.IdConsumerType.HasValue ?
                            (Dictionary.ConsumerTypesDictionary.TryGetValue(dopplerUser.IdConsumerType, out string consumerType) ? consumerType : "CF")
                            : "CF",
                Currency = fatherBusinessPartner?.Currency ?? "##",
                AliasName = dopplerUser.Email.ToLower(),
                U_B1SYS_FiscIdType = CalculateFiscalType(dopplerUser.IdConsumerType, dopplerUser.FederalTaxID.Replace("-", "")),
                CardType = "C",
                U_DPL_CANCELED = dopplerUser.Cancelated ? "Y" : "N",
                U_DPL_SUSPENDED = dopplerUser.Blocked ? "Y" : "N",
                SalesPersonCode = (dopplerUser.IsInbound.HasValue ? (dopplerUser.IsInbound.GetValueOrDefault() ? 1 : 2) : 3),
                Indicator = "FC",
                DunningTerm = "ReclamoVto",
                FatherCard = fatherBusinessPartner?.CardCode,
                ContactEmployees = GetContactEmployees(dopplerUser, cardCode, "Facturacion"),
                BPAddresses = new List<Address>
                {
                    new Address
                        {
                            AddressName = "Bill to",
                            Street = dopplerUser.BillingAddress != null ? dopplerUser.BillingAddress.ToUpper() : "",
                            ZipCode = dopplerUser.BillingZip != null ? dopplerUser.BillingZip.ToUpper() : "",
                            City = dopplerUser.BillingCity != null ? dopplerUser.BillingCity.ToUpper() : "",
                            Country = dopplerUser.BillingCountryCode != null ? dopplerUser.BillingCountryCode.ToUpper() : "",
                            County = dopplerUser.County,
                            State = !string.IsNullOrEmpty(dopplerUser.BillingStateId) ? dopplerUser.BillingStateId : "99",
                            AddressType = "bo_BillTo",
                                BPCode =  cardCode,
                            RowNum = 0
                        },
                    new Address
                        {
                            AddressName = "Ship to",
                            Street = dopplerUser.BillingAddress != null ? dopplerUser.BillingAddress.ToUpper() : "",
                            ZipCode = dopplerUser.BillingZip != null ? dopplerUser.BillingZip.ToUpper() : "",
                            City = dopplerUser.BillingCity != null ? dopplerUser.BillingCity.ToUpper() : "",
                            Country = dopplerUser.BillingCountryCode != null ? dopplerUser.BillingCountryCode.ToUpper() : "",
                            County = dopplerUser.County,
                            State = !string.IsNullOrEmpty(dopplerUser.BillingStateId) ? dopplerUser.BillingStateId : "99",
                            AddressType = "bo_ShipTo",
                                BPCode =  cardCode,
                            RowNum = 1
                        }
                    }
            };

            if (dopplerUser.SAPProperties != null)
            {
                newBusinessPartner.Properties1 = dopplerUser.SAPProperties.ContractCurrency ? "tYES" : "tNO";
                newBusinessPartner.Properties2 = dopplerUser.SAPProperties.GovernmentAccount ? "tYES" : "tNO";
                newBusinessPartner.Properties3 = dopplerUser.SAPProperties.Premium ? "tYES" : "tNO";
                newBusinessPartner.Properties4 = dopplerUser.SAPProperties.Plus ? "tYES" : "tNO";
                newBusinessPartner.Properties5 = dopplerUser.SAPProperties.ComercialPartner ? "tYES" : "tNO";
                newBusinessPartner.Properties6 = dopplerUser.SAPProperties.MarketingPartner ? "tYES" : "tNO";
                newBusinessPartner.Properties7 = dopplerUser.SAPProperties.OnBoarding ? "tYES" : "tNO";
                newBusinessPartner.Properties8 = dopplerUser.SAPProperties.Layout ? "tYES" : "tNO";
                newBusinessPartner.Properties9 = dopplerUser.SAPProperties.Datahub ? "tYES" : "tNO";
                newBusinessPartner.Properties10 = dopplerUser.SAPProperties.PushNotification ? "tYES" : "tNO";
                newBusinessPartner.Properties11 = dopplerUser.SAPProperties.ExclusiveIp ? "tYES" : "tNO";
                newBusinessPartner.Properties12 = dopplerUser.SAPProperties.Advisory ? "tYES" : "tNO";
                newBusinessPartner.Properties13 = dopplerUser.SAPProperties.Reports ? "tYES" : "tNO";
                newBusinessPartner.Properties14 = dopplerUser.SAPProperties.SMS ? "tYES" : "tNO";
            }

            return newBusinessPartner;
        }

        private string CalculateFiscalType(int? idConsumerType, string federalTaxID)
        {
            var consumer = idConsumerType.HasValue ?
                            (Dictionary.ConsumerTypesDictionary.TryGetValue(idConsumerType, out string consumerType) ? consumerType : "CF")
                            : "CF";

            if (consumer == "CF")
            {
                return (federalTaxID.Length <= 8 ? (int)FiscalTypeEnum.DNI : (int)FiscalTypeEnum.CUIL).ToString();
            }

            return ((int)FiscalTypeEnum.CUIT).ToString();
        }
    }
}
