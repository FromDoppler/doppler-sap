using Doppler.Sap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection.PortableExecutable;

namespace Doppler.Sap.Mappers.BusinessPartner
{
    public abstract class BusinessPartnerMapper
    {
        /// <summary>
        /// Key: PlanType; Value: GroupCode
        /// </summary>
        protected abstract Dictionary<int, int> DopplerGroupCodes { get; }

        /// <summary>
        /// Key: ClientManagerType; Value: GroupCode
        /// </summary>
        protected abstract Dictionary<int, int> ClientManagerGroupCodes { get; }

        /// <summary>
        /// Key: PlanType; Value: GroupCode
        /// </summary>
        protected abstract Dictionary<int, int> RelayGroupCodes { get; }

        protected int MapGroupCode(DopplerUserDto dopplerUser)
        {
            var groupCode = (!dopplerUser.IsClientManager && !dopplerUser.IsFromRelay) ?
                            (dopplerUser.PlanType.HasValue ? (DopplerGroupCodes.TryGetValue(dopplerUser.PlanType.Value, out var dopplerGroupCode) ? dopplerGroupCode : 0) : 0) :
                            (dopplerUser.IsClientManager) ?
                            ClientManagerGroupCodes.TryGetValue(dopplerUser.ClientManagerType, out var clientManagerGroupCode) ? clientManagerGroupCode : 0 :
                            (dopplerUser.PlanType.HasValue ? (RelayGroupCodes.TryGetValue(dopplerUser.PlanType.Value, out var relayGroupCode) ? relayGroupCode : 0) : 0);

            return groupCode;
        }

        protected List<SapContactEmployee> GetContactEmployees(string sapSystem, DopplerUserDto dopplerUser, string cardCode, string emailGroupCode)
        {
            var contactEmployees = (dopplerUser.BillingEmails != null && dopplerUser.BillingEmails[0] != String.Empty) ?
                dopplerUser.BillingEmails
                    .Select(x => new SapContactEmployee
                    {
                        Name = new MailAddress(x.ToLower()).User,
                        E_Mail = x.ToLower(),
                        CardCode = cardCode,
                        Active = "tYES",
                        EmailGroupCode = emailGroupCode
                    })
                    .Append(new SapContactEmployee
                    {
                        Name = new MailAddress(dopplerUser.Email.ToLower()).User,
                        E_Mail = dopplerUser.Email.ToLower(),
                        CardCode = cardCode,
                        Active = "tYES",
                        EmailGroupCode = emailGroupCode
                    })
                    .GroupBy(y => y.E_Mail)
                    .Select(z => z.First())
                    .ToList()
                    :
                    new List<SapContactEmployee>
                    {
                        new SapContactEmployee
                        {
                            Name = new MailAddress(dopplerUser.Email.ToLower()).User,
                            E_Mail = dopplerUser.Email.ToLower(),
                            CardCode = cardCode,
                            Active = "tYES",
                            EmailGroupCode = emailGroupCode
                        }
                    };

            var sapContactEmployeesWithoutRepeatedName = GetContactEmployeesWithoutRepeatedName(contactEmployees);
            var sapContactEmployees = sapSystem == "US" ?
                sapContactEmployeesWithoutRepeatedName :
                sapContactEmployeesWithoutRepeatedName.Select(ce => new SapContactEmployee
                {
                    E_Mail = ce.E_Mail,
                    CardCode = ce.CardCode,
                    EmailGroupCode = ce.EmailGroupCode,
                    Name = ce.Name,
                    Active = ce.Active,
                    U_BOY_85_ECAT = "1"
                }).ToList();

            return sapContactEmployees;
        }

        private List<SapContactEmployee> GetContactEmployeesWithoutRepeatedName(List<SapContactEmployee> contacts)
        {
            var contactEmployeesGrouped = contacts.GroupBy(c => c.Name).Select(z => new { z.Key, Result = z.Select(c => c) }).ToList();

            if (!contactEmployeesGrouped.Any(c => c.Result.Count() > 1))
            {
                return contacts;
            }

            var sapContactEmployees = contactEmployeesGrouped.SelectMany(g => g.Result.Count() > 1 ? g.Result.Select((z, i) => new SapContactEmployee
            {
                Active = z.Active,
                CardCode = z.CardCode,
                EmailGroupCode = z.EmailGroupCode,
                E_Mail = z.E_Mail,
                InternalCode = z.InternalCode,
                Name = z.Name + (i + 1)
            }) : new List<SapContactEmployee> { g.Result.FirstOrDefault() }).ToList();

            return GetContactEmployeesWithoutRepeatedName(sapContactEmployees);
        }
    }
}
