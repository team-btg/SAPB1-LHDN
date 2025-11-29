using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SAP_LHDN.Models
{
    public class InvoicePart
    { 
        [JsonProperty("OrderQty")]
        public double OrderQty { get; set; }
        [JsonProperty("UOM")]
        public string Uom { get; set; } = string.Empty;
        [JsonProperty("UnitPrice")]
        public double UnitPrice { get; set; }
        [JsonProperty("Amount")]
        public decimal Amount { get; set; }
        [JsonProperty("DisAmt")]
        public double DisAmt { get; set; }
        [JsonProperty("DisPer")]
        public double DisPer { get; set; }
        [JsonProperty("Reference")]
        public string Reference { get; set; } = string.Empty;
        [JsonProperty("Classification")]
        public string Classification { get; set; } = string.Empty;
        [JsonProperty("Description")]
        public string Description { get; set; } = string.Empty;
        [JsonProperty("TaxType")]
        public string TaxType { get; set; } = string.Empty;
        [JsonProperty("TaxRate")]
        public double TaxRate { get; set; }
        [JsonProperty("TaxAmount")]
        public decimal TaxAmount { get; set; }
        [JsonProperty("TaxExemption")]
        public string TaxExemption { get; set; } = string.Empty;
        [JsonProperty("TaxExemptionAmt")]
        public decimal TaxExemptionAmt { get; set; }
    }

    public abstract class BaseInvoice
    {
        // Core Identity and Dates
        [JsonProperty("RefNo")]
        public string RefNo { get; set; } = string.Empty;
        [JsonProperty("InvDate")]
        public DateTime InvDate { get; set; }
        [JsonProperty("PostDate")]
        public DateTime PostDate { get; set; }
        [JsonProperty("PaymentTerm")]
        public string PaymentTerm { get; set; } = string.Empty;
        [JsonProperty("HeaderAmount")]
        public decimal HeaderAmount { get; set; }
        [JsonProperty("RoundingAdj")]
        public decimal RoundingAdj { get; set; }
        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; } = "SYSTEM";
        [JsonProperty("CreatedDate")]
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        [JsonProperty("Remark")]
        public string Remark { get; set; } = string.Empty;

        // Vendor/Buyer Information 
        [JsonProperty("TIN")]
        public string Tin { get; set; } = string.Empty;
        [JsonProperty("BRN")]
        public string Brn { get; set; } = string.Empty;
        [JsonProperty("BNPType")]
        public string BnpType { get; set; } = string.Empty;
        [JsonProperty("BName")]
        public string BName { get; set; } = string.Empty;
        [JsonProperty("Address1")]
        public string Address1 { get; set; } = string.Empty;
        [JsonProperty("Address2")]
        public string Address2 { get; set; } = string.Empty;
        [JsonProperty("Address3")]
        public string Address3 { get; set; } = string.Empty;
        [JsonProperty("PostCode")]
        public string PostCode { get; set; } = string.Empty;
        [JsonProperty("City")]
        public string City { get; set; } = string.Empty;
        [JsonProperty("State")]
        public string State { get; set; } = string.Empty;
        [JsonProperty("Country")]
        public string Country { get; set; } = string.Empty;
        [JsonProperty("TelNo")]
        public string TelNo { get; set; } = string.Empty;
        [JsonProperty("FaxNo")]
        public string FaxNo { get; set; } = string.Empty;
        [JsonProperty("Email")]
        public string Email { get; set; } = string.Empty;

        // Financial/Trade Information
        [JsonProperty("Currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonProperty("CurrencyRate")]
        public double CurrencyRate { get; set; }
        [JsonProperty("Terms")]
        public string Terms { get; set; } = string.Empty;

        // Customs and Trade Details 
        [JsonProperty("CustomForm1")]
        public string CustomForm1 { get; set; } = string.Empty;
        [JsonProperty("Incoterm")]
        public string Incoterm { get; set; } = string.Empty;
        [JsonProperty("FTA")]
        public string Fta { get; set; } = string.Empty;
        [JsonProperty("AuthNoCertExp")]
        public string AuthNoCertExp { get; set; } = string.Empty;
        [JsonProperty("CustomForm2")]
        public string CustomForm2 { get; set; } = string.Empty;
        [JsonProperty("CountryOfOrigin")]
        public string CountryOfOrigin { get; set; } = string.Empty;
        [JsonProperty("DetOtherCharge")]
        public string DetOtherCharge { get; set; } = string.Empty;

        // Line Items
        [JsonProperty("InvoicePart")]
        public List<InvoicePart> InvoiceParts { get; set; } = new List<InvoicePart>();
    }
}
