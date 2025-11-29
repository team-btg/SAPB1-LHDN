using System;
using System.Collections.Generic; 
using Newtonsoft.Json; 

namespace SAP_LHDN.Models.CreditNote
{ 
    public class APCreditMemo
    {
        [JsonProperty("RefNo")]
        public string RefNo { get; set; }

        [JsonProperty("Date")]
        public DateTime? Date { get; set; } 

        [JsonProperty("Type")]
        public string _Type { get; set; }

        [JsonProperty("HeaderAmount")]
        public decimal? HeaderAmount { get; set; }

        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("CreatedDate")]
        public DateTimeOffset? CreatedDate { get; set; }

        [JsonProperty("Remark")]
        public string Remark { get; set; }

        [JsonProperty("TIN")]
        public string Tin { get; set; }

        [JsonProperty("BRN")]
        public string Brn { get; set; }

        [JsonProperty("BNPType")]
        public string BnpType { get; set; }

        [JsonProperty("BName")]
        public string BName { get; set; }

        [JsonProperty("Address1")]
        public string Address1 { get; set; }

        [JsonProperty("Address2")]
        public string Address2 { get; set; }

        [JsonProperty("Address3")]
        public string Address3 { get; set; }

        [JsonProperty("PostCode")]
        public string PostCode { get; set; }

        [JsonProperty("City")]
        public string City { get; set; }

        [JsonProperty("State")]
        public string State { get; set; }

        [JsonProperty("Country")]
        public string Country { get; set; }

        [JsonProperty("TelNo")]
        public string TelNo { get; set; }

        [JsonProperty("FaxNo")]
        public string FaxNo { get; set; }

        [JsonProperty("Email")]
        public string Email { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("CurrencyRate")]
        public double? CurrencyRate { get; set; }

        [JsonProperty("Terms")]
        public string Terms { get; set; }

        [JsonProperty("ShipReceiptName")]
        public string ShipReceiptName { get; set; }

        [JsonProperty("ShipReceiptAddress1")]
        public string ShipReceiptAddress1 { get; set; }

        [JsonProperty("ShipReceiptAddress2")]
        public string ShipReceiptAddress2 { get; set; }

        [JsonProperty("ShipReceiptAddress3")]
        public string ShipReceiptAddress3 { get; set; }

        [JsonProperty("ShipReceiptTIN")]
        public string ShipReceiptTIN { get; set; }

        [JsonProperty("ShipReceiptBRN")]
        public string ShipReceiptBRN { get; set; }

        [JsonProperty("ShipReceiptBNPType")]
        public string ShipReceiptBNPType { get; set; }

        [JsonProperty("ShipReceiptPostcode")]
        public string ShipReceiptPostcode { get; set; }

        [JsonProperty("ShipReceiptCity")]
        public string ShipReceiptCity { get; set; }

        [JsonProperty("ShipReceiptState")]
        public string ShipReceiptState { get; set; }

        [JsonProperty("ShipReceiptCountry")]
        public string ShipReceiptCountry { get; set; }

        [JsonProperty("CustomForm1")]
        public string CustomForm1 { get; set; }

        [JsonProperty("Incoterm")]
        public string Incoterm { get; set; }

        [JsonProperty("FTA")]
        public string FTA { get; set; }

        [JsonProperty("AuthNoCertExp")]
        public string AuthNoCertExp { get; set; }

        [JsonProperty("CustomForm2")]
        public string CustomForm2 { get; set; }

        [JsonProperty("CountryOfOrigin")]
        public string CountryOfOrigin { get; set; }

        [JsonProperty("DetOtherCharge")]
        public decimal? DetOtherCharge { get; set; }

        [JsonProperty("EInvRefNo")]
        public string EInvRefNo { get; set; }

        [JsonProperty("SelfBill")]
        public bool SelfBill { get; set; }

        [JsonProperty("MSICCode")]
        public string MSICCode { get; set; }

        [JsonProperty("apcndnpart")]
        public List<CreditMemoDetail> apcndnpart { get; set; } = new List<CreditMemoDetail>();
    }

}
