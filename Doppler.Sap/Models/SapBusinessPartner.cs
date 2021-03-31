using System;
using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class SapBusinessPartner
    {
        public DateTime? CreateDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int GroupCode { get; set; }
        public string EmailAddress { get; set; }
        public string Phone1 { get; set; }
        public string FederalTaxID { get; set; }
        public string U_B1SYS_VATCtg { get; set; }
        public string U_B1SYS_FiscIdType { get; set; }
        public string CardType { get; set; }
        public string Properties1 { get; set; }
        public string Properties2 { get; set; }
        public string Properties3 { get; set; }
        public string Properties4 { get; set; }
        public string Properties5 { get; set; }
        public string Properties6 { get; set; }
        public string Properties7 { get; set; }
        public string Properties8 { get; set; }
        public string Properties9 { get; set; }
        public string Properties10 { get; set; }
        public string Properties11 { get; set; }
        public string Properties12 { get; set; }
        public string Properties13 { get; set; }
        public string Properties14 { get; set; }
        public string Properties15 { get; set; }
        public List<SapContactEmployee> ContactEmployees { get; set; }
        public string AliasName { get; set; }
        public string Currency { get; set; }
        public int PayTermsGrpCode { get; set; }
        public string U_DPL_CANCELED { get; set; }
        public string U_DPL_SUSPENDED { get; set; }
        public int SalesPersonCode { get; set; }
        public string Indicator { get; set; }
        public string ContactPerson { get; set; }
        public List<Address> BPAddresses { get; set; }
        public string DunningTerm { get; set; }
        public string FatherCard { get; set; }
    }
}
