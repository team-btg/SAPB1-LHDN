using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAP_LHDN.Models
{
    public class WorkerSettings
    {
        public int PollingIntervalSeconds { get; set; } = 90;
        public List<CompanyConfig> Companies { get; set; } = new List<CompanyConfig>();
    }

    public class CompanyConfig
    {
        public string CompanyName { get; set; } = string.Empty; 
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ApiStagingUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableTest { get; set; }
    }
}