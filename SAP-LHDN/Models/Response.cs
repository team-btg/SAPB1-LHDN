using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SAP_LHDN.Models
{
    public class ApiSuccessMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ApiSuccessResponse
    {
        [JsonProperty("success")]
        public ApiSuccessMessage Success { get; set; } = new ApiSuccessMessage();
    }

    public class ApiValidationErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; } = "The request is invalid.";

        [JsonProperty("modelState")]
        // Key is the field name (e.g., "salesinvoice.InvDate"), Value is an array of error messages.
        public Dictionary<string, string[]> ModelState { get; set; }
    }
}
