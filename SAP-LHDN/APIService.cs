using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SAP_LHDN.Models.CreditNote;
using SAP_LHDN.Models.Invoice;

namespace SAP_LHDN.Models
{
    public class EInvoiceService
    {
        // Custom class to replace C# 7.0+ Value Tuples (bool, string)
        public class ServiceResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        // --- Token Cache per Base URL ---
        // Key: BaseUrl (string), Value: (Token, Expiry)
        private readonly Dictionary<string, (string token, DateTimeOffset expiry)> _tokenCache = new Dictionary<string, (string, DateTimeOffset)>();

        private readonly HttpClient _httpClient;
        private readonly ILogger<EInvoiceService> _logger;
        private readonly string _defaultApiBaseUrl;

        // API Constants 
        private const string TokenEndpoint = "connect/token";
        private const string SalesCreateEndpoint = "api/salesinvoice/create";
        private const string PurchaseCreateEndpoint = "api/purchaseinvoice/create";
        private const string ARCMEndpoint = "api/arcndn/create";
        private const string APCMEndpoint = "api/apcndn/create";
        private const string StatusEndpoint = "api/einvoicestatus";

        // Auth Credentials (Staging provided by user)
        private const string UsernameValue = "lqzpqlw-9fuevkd-vjz9-m2sq-kqclkqqmmnffg";
        private const string PasswordValue = "exevnqn-r3u6-xsb3-68qc-plqtnhfnvfe";
        private const string GrantType = "password";

        public EInvoiceService(HttpClient httpClient, ILogger<EInvoiceService> logger, string apiBaseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _defaultApiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? string.Empty;

            // Ensure BaseAddress is set as a fallback to satisfy HttpClient requirements.
            if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_defaultApiBaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_defaultApiBaseUrl + "/");
            }
        }

        private string GetTargetUrl(string endpoint, string overrideBaseUrl)
        {
            var baseUrl = string.IsNullOrEmpty(overrideBaseUrl) ? _defaultApiBaseUrl : overrideBaseUrl.TrimEnd('/');
            return $"{baseUrl}/{endpoint.TrimStart('/')}";
        }

        /// <summary>
        /// Gets a valid token for the specific base URL provided.
        /// </summary>
        public async Task<string> GetValidTokenAsync(string baseUrl)
        {
            // 1. Check cache for valid token
            if (_tokenCache.TryGetValue(baseUrl, out var cached) && DateTimeOffset.UtcNow < cached.expiry)
            {
                return cached.token;
            }

            // 2. Request new token from environment-specific endpoint
            var tokenUrl = $"{baseUrl}/{TokenEndpoint.TrimStart('/')}";
            _logger.LogInformation($"Requesting new token for environment: {baseUrl}");

            var authContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", UsernameValue),
                new KeyValuePair<string, string>("password", PasswordValue),
                new KeyValuePair<string, string>("grant_type", GrantType)
            });

            try
            {
                var response = await _httpClient.PostAsync(new Uri(tokenUrl), authContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    var expiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 10);
                    _tokenCache[baseUrl] = (tokenResponse.AccessToken, expiry);
                    return tokenResponse.AccessToken;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to acquire token for {baseUrl}");
            }

            return null;
        }

        /// <summary>
        /// Generic method to handle authorized POST requests to different environments.
        /// </summary>
        private async Task<ServiceResult> SendAuthorizedRequestAsync<T>(string endpoint, T data, string overrideBaseUrl)
        {
            var baseUrl = string.IsNullOrEmpty(overrideBaseUrl) ? _defaultApiBaseUrl : overrideBaseUrl.TrimEnd('/');
            var targetUrl = GetTargetUrl(endpoint, overrideBaseUrl);

            var token = await GetValidTokenAsync(baseUrl);
            if (string.IsNullOrEmpty(token))
            {
                return new ServiceResult { Success = false, Message = $"Authentication failed for {baseUrl}" };
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(data);

                using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(targetUrl)))
                {
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(request);
                    var resultBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return new ServiceResult { Success = true, Message = resultBody };
                    }

                    // Format detailed validation errors if request failed
                    var defaultError = $"API Error ({response.StatusCode}): {resultBody}";
                    var friendlyError = FormatValidationErrors(resultBody, defaultError);

                    _logger.LogWarning($"Request to {targetUrl} failed: {friendlyError}");
                    return new ServiceResult { Success = false, Message = friendlyError };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during request to {targetUrl}");
                return new ServiceResult { Success = false, Message = $"Internal Error: {ex.Message}" };
            }
        }

        private string FormatValidationErrors(string errorContent, string defaultMessage)
        {
            try
            {
                var validationError = JsonConvert.DeserializeObject<ApiValidationErrorResponse>(errorContent);
                if (validationError?.ModelState != null && validationError.ModelState.Count > 0)
                {
                    var errorBuilder = new StringBuilder();
                    errorBuilder.AppendLine($"Validation Failed: {validationError.Message}");
                    errorBuilder.AppendLine("Details:");

                    foreach (var kvp in validationError.ModelState)
                    {
                        errorBuilder.AppendLine($"  - Field: {kvp.Key}");
                        foreach (var error in kvp.Value)
                        {
                            errorBuilder.AppendLine($"    Error: {error}");
                        }
                    }
                    return errorBuilder.ToString();
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to deserialize detailed validation error.");
            }
            return defaultMessage;
        }

        public async Task<ServiceResult> CreateSalesInvoiceAsync(SalesInvoice newInvoice, string overrideBaseUrl = null)
        {
            var result = await SendAuthorizedRequestAsync(SalesCreateEndpoint, newInvoice, overrideBaseUrl);
            if (result.Success)
            {
                var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(result.Message);
                result.Message = $"Sales Invoice RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}";
            }
            return result;
        }

        public async Task<ServiceResult> CreatePurchaseInvoiceAsync(PurchaseInvoice newInvoice, string overrideBaseUrl = null)
        {
            var result = await SendAuthorizedRequestAsync(PurchaseCreateEndpoint, newInvoice, overrideBaseUrl);
            if (result.Success)
            {
                var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(result.Message);
                result.Message = $"Purchase Invoice RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}";
            }
            return result;
        }

        public async Task<ServiceResult> CreateARCMAsync(ARCreditMemo newInvoice, string overrideBaseUrl = null)
        {
            var result = await SendAuthorizedRequestAsync(ARCMEndpoint, newInvoice, overrideBaseUrl);
            if (result.Success)
            {
                var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(result.Message);
                result.Message = $"ARCM RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}";
            }
            return result;
        }

        public async Task<ServiceResult> CreateAPCMAsync(APCreditMemo newInvoice, string overrideBaseUrl = null)
        {
            var result = await SendAuthorizedRequestAsync(APCMEndpoint, newInvoice, overrideBaseUrl);
            if (result.Success)
            {
                var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(result.Message);
                result.Message = $"APCM RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}";
            }
            return result;
        }

        public async Task<List<EInvoiceStatusData>> GetEInvoiceStatusListAsync(EInvoiceStatusRequest request, string overrideBaseUrl = null)
        {
            var result = await SendAuthorizedRequestAsync(StatusEndpoint, request, overrideBaseUrl);
            if (result.Success)
            {
                var statusResponse = JsonConvert.DeserializeObject<EInvoiceStatusResponse>(result.Message);
                return statusResponse?.Data ?? new List<EInvoiceStatusData>();
            }
            return new List<EInvoiceStatusData>();
        }
    }
}