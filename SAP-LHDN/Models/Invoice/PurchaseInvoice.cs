using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SAP_LHDN.Models.Invoice
{
    public class PurchaseInvoice : BaseInvoice
    {
        // Purchase Specific Fields 
        [JsonProperty("VendorInvNo")]
        public string VendorInvNo { get; set; } = string.Empty;
        [JsonProperty("RecDate")]
        public DateTime RecDate { get; set; } // Receive Date
        [JsonProperty("ImportDeclarationNo")]
        public string ImportDeclarationNo { get; set; } = string.Empty;
        [JsonProperty("MSICCode")]
        public string MsicCode { get; set; } = string.Empty;
    }
}
