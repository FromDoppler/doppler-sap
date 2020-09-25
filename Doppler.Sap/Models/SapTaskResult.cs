namespace Doppler.Sap.Models
{
    public class SapTaskResult
    {
        public bool IsSuccessful { get; set; }
        public string SapResponseContent { get; set; }
        public string TaskName { get; set; }
    }
}
