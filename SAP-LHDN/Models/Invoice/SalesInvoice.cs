using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SAP_LHDN.Models.Invoice
{
    public class SalesInvoice : BaseInvoice
    {
        // Sales Specific Fields 
        [JsonProperty("CustPO")]
        public string CustPO { get; set; } = string.Empty;
        [JsonProperty("PODate")]
        public DateTime? Podate { get; set; }
        [JsonProperty("DeclarationNo")]
        public string DeclarationNo { get; set; } = string.Empty;
        [JsonProperty("DiscountVoucherAdj")]
        public decimal DiscountVoucherAdj { get; set; }
        [JsonProperty("DiscountVoucher")]
        public string DiscountVoucher { get; set; } = string.Empty;
    }

    public class EInvoiceStatusRequest
    {
        [JsonProperty("DocType")]
        public string DocType { get; set; } = "SInvoice";

        [JsonProperty("RefNo")]
        public List<string> RefNo { get; set; } 
    }

    // --- 7. E-Invoice Status Response Models (Reusable) ---
    public class EInvoiceStatusResponse
    {
        [JsonProperty("Data")]
        public List<EInvoiceStatusData> Data { get; set; } = new List<EInvoiceStatusData>();
    }

    public class EInvoiceStatusData
    {
        [JsonProperty("DocType")]
        public string DocType { get; set; } = string.Empty;

        [JsonProperty("RefNo")]
        public string RefNo { get; set; } = string.Empty;

        [JsonProperty("DocDate")]
        public DateTime DocDate { get; set; }

        [JsonProperty("EInvIRBMNo")]
        public string EInvIRBMNo { get; set; } = string.Empty;

        [JsonProperty("EInvValLink")]
        public string EInvValLink { get; set; } = string.Empty;

        [JsonProperty("EInvValDate")]
        public DateTimeOffset? EInvValDate { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; } = string.Empty;
    }
}
