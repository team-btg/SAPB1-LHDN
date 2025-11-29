using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SAP_LHDN.Models
{
    public class TokenResponse
    {
        [JsonProperty("access_token")] // CHANGED: Attribute used by Newtonsoft.Json
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
