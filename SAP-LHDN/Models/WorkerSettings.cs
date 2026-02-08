using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAP_LHDN.Models
{
    public class WorkerSettings
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ApiStagingUrl { get; set; } = string.Empty;
        public bool EnableTest { get; set; }
        public int PollingIntervalSeconds { get; set; }
    }
}