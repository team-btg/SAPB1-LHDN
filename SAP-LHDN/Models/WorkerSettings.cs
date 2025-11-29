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
        // Corresponds to "WorkerSettings:PollingIntervalSeconds"
        public int PollingIntervalSeconds { get; set; }
    }
}