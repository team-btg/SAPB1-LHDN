using Newtonsoft.Json;

namespace SAP_LHDN.Models.CreditNote
{ 
    public class CreditMemoDetail
    {
        [JsonProperty("PartNo")]
        public string PartNo { get; set; }

        [JsonProperty("Qty")]
        public double? Qty { get; set; }

        [JsonProperty("UOM")]
        public string Uom { get; set; }

        [JsonProperty("UnitPrice")]
        public double? UnitPrice { get; set; }

        [JsonProperty("Amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("Classification")]
        public string Classification { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("TaxType")]
        public string TaxType { get; set; }

        [JsonProperty("TaxRate")]
        public double? TaxRate { get; set; }

        [JsonProperty("TaxAmount")]
        public decimal? TaxAmount { get; set; }

        [JsonProperty("TaxExemption")]
        public string TaxExemption { get; set; }

        [JsonProperty("TaxExemptionAmt")]
        public decimal? TaxExemptionAmt { get; set; }
    }

}
