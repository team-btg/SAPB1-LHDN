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

        private string _accessToken = string.Empty;
        private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

        private readonly HttpClient _httpClient;
        private readonly ILogger<EInvoiceService> _logger;

        // API Constants 
        private const string TokenEndpoint = "connect/token";
        private const string SalesCreateEndpoint = "api/salesinvoice/create";
        private const string PurchaseCreateEndpoint = "api/purchaseinvoice/create";
        private const string ARCMEndpoint = "api/arcndn/create";
        private const string APCMEndpoint = "api/apcndn/create";
        private const string StatusEndpoint = "api/einvoicestatus";

        // Auth Credentials
        private const string UsernameValue = "lqzpqlw-9fuevkd-vjz9-m2sq-kqclkqqmmnffg";
        private const string PasswordValue = "exevnqn-r3u6-xsb3-68qc-plqtnhfnvfe";
        private const string GrantType = "password";

        public EInvoiceService(HttpClient httpClient, ILogger<EInvoiceService> logger, string apiBaseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new ArgumentException("ApiBaseUrl cannot be null or empty.", nameof(apiBaseUrl));
            }

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(apiBaseUrl);
            }
        }

        /// <summary>
        /// Attempts to acquire a new access token if one is not present.
        /// </summary>
        public async Task<bool> EnsureAuthenticatedAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                _logger.LogDebug("Cached token is still valid. Skipping network request."); 
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                return true;
            }

            _logger.LogInformation("Attempting to acquire new access token...");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", UsernameValue),
                new KeyValuePair<string, string>("password", PasswordValue),
                new KeyValuePair<string, string>("grant_type", GrantType)
            });

            try
            {
                var response = await _httpClient.PostAsync(TokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 5);
                     
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        tokenResponse.TokenType,
                        tokenResponse.AccessToken
                    );
                    _logger.LogInformation("Successfully acquired and set new access token.");
                    return true;
                }
                _logger.LogError("Token response was null or missing access token.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate and obtain security token.");
                return false;
            }
        }

        private string FormatValidationErrors(string errorContent, string defaultMessage)
        {
            try
            {
                // Attempt to deserialize the validation error structure
                var validationError = JsonConvert.DeserializeObject<ApiValidationErrorResponse>(errorContent);
                if (validationError?.ModelState != null && validationError.ModelState.Count > 0)
                {
                    var errorBuilder = new StringBuilder();
                    errorBuilder.AppendLine($"Validation Failed: {validationError.Message}");
                    errorBuilder.AppendLine("Details:");

                    // Loop through the dictionary of field errors
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
                // Failed to deserialize as validation error, fall through to default message
                _logger.LogWarning("Failed to deserialize detailed validation error. Returning raw error content.");
            }

            return defaultMessage;
        }

        /// <summary>
        /// Creates a new sales invoice via POST /api/salesinvoice/create.
        /// </summary>
        // FIX: Changed return type from Task<(bool, string)> to Task<ServiceResult>
        public async Task<ServiceResult> CreateSalesInvoiceAsync(SalesInvoice newInvoice)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return new ServiceResult { Success = false, Message = "Sales Invoice creation failed: Authentication required." };
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(newInvoice);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(SalesCreateEndpoint, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(content);
                    return new ServiceResult
                    {
                        Success = true,
                        Message = $"Sales Invoice RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var defaultError = $"API Error on Sales Invoice creation ({response.StatusCode}): {errorContent}";
                    var friendlyError = FormatValidationErrors(errorContent, defaultError);
                    return new ServiceResult { Success = false, Message = friendlyError };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during Sales Invoice creation for RefNo {newInvoice.RefNo}.");
                return new ServiceResult { Success = false, Message = $"Internal Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Creates a new purchase invoice via POST /api/purchaseinvoice/create.
        /// </summary>
        // FIX: Changed return type from Task<(bool, string)> to Task<ServiceResult>
        public async Task<ServiceResult> CreatePurchaseInvoiceAsync(PurchaseInvoice newInvoice)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return new ServiceResult { Success = false, Message = "Purchase Invoice creation failed: Authentication required." };
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(newInvoice);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(PurchaseCreateEndpoint, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(content);
                    return new ServiceResult
                    {
                        Success = true,
                        Message = $"Purchase Invoice RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var defaultError = $"API Error on Purchase Invoice creation ({response.StatusCode}): {errorContent}";
                    var friendlyError = FormatValidationErrors(errorContent, defaultError);
                    return new ServiceResult { Success = false, Message = friendlyError };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during Purchase Invoice creation for RefNo {newInvoice.RefNo}.");
                return new ServiceResult { Success = false, Message = $"Internal Error: {ex.Message}" };
            }
        }

        public async Task<ServiceResult> CreateARCMAsync(ARCreditMemo newInvoice)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return new ServiceResult { Success = false, Message = "ARCM creation failed: Authentication required." };
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(newInvoice);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ARCMEndpoint, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(content);
                    return new ServiceResult
                    {
                        Success = true,
                        Message = $"ARCM RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var defaultError = $"API Error on ARCM creation ({response.StatusCode}): {errorContent}";
                    var friendlyError = FormatValidationErrors(errorContent, defaultError);
                    return new ServiceResult { Success = false, Message = friendlyError };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during ARCM creation for RefNo {newInvoice.RefNo}.");
                return new ServiceResult { Success = false, Message = $"Internal Error: {ex.Message}" };
            }
        }

        public async Task<ServiceResult> CreateAPCMAsync(APCreditMemo newInvoice)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return new ServiceResult { Success = false, Message = "APCM creation failed: Authentication required." };
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(newInvoice);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(APCMEndpoint, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var successResponse = JsonConvert.DeserializeObject<ApiSuccessResponse>(content);
                    return new ServiceResult
                    {
                        Success = true,
                        Message = $"APCM RefNo {newInvoice.RefNo} created successfully. API Message: {successResponse?.Success?.Message ?? "No specific message."}"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var defaultError = $"API Error on APCM creation ({response.StatusCode}): {errorContent}";
                    var friendlyError = FormatValidationErrors(errorContent, defaultError);
                    return new ServiceResult { Success = false, Message = friendlyError };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during APCM creation for RefNo {newInvoice.RefNo}.");
                return new ServiceResult { Success = false, Message = $"Internal Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Retrieves a list of E-Invoice statuses via POST /api/einvoicestatus.
        /// </summary>
        public async Task<List<EInvoiceStatusData>> GetEInvoiceStatusListAsync(EInvoiceStatusRequest request)
        {
            if (!await EnsureAuthenticatedAsync())
            {
                _logger.LogWarning("Status retrieval failed: Authentication required.");
                return new List<EInvoiceStatusData>();
            }

            try
            {
                var jsonContent = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(StatusEndpoint, httpContent);

                // Check success manually to read body if it fails, though status endpoint usually returns 
                // 200 even with empty data, but we keep the robust check just in case.
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var defaultError = $"API Error on Status retrieval ({response.StatusCode}): {errorContent}";
                    _logger.LogError(defaultError);
                    return new List<EInvoiceStatusData>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var statusResponse = JsonConvert.DeserializeObject<EInvoiceStatusResponse>(jsonResponse);

                return statusResponse?.Data ?? new List<EInvoiceStatusData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during E-Invoice status retrieval.");
                return new List<EInvoiceStatusData>();
            }
        }
    }
}
