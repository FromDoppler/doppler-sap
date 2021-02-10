using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class SapCreditNoteList
    {
        public string Metadata { get; set; }
        public List<SapCreditNoteResponse> Value { get; set; }
    }
}
