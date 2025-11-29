using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAP_LHDN.Models;
using SAP_LHDN.Models.CreditNote;
using SAP_LHDN.Models.Invoice;
using StringExtensions;

namespace SAP_LHDN
{
    public class InvoicePollingWorker : BackgroundService
    {
        private readonly ILogger<InvoicePollingWorker> _logger;
        private readonly EInvoiceService _invoiceService;
        private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(90);
        private readonly HanaService _hanaService;

        public InvoicePollingWorker(
            ILogger<InvoicePollingWorker> logger,
            EInvoiceService invoiceService,
            HanaService hanaService)
        {
            _logger = logger;
            _invoiceService = invoiceService;
            _hanaService = hanaService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("--- E-Invoice Polling Worker Service is starting... ---");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"--- Worker executing cycle at: {DateTimeOffset.Now} ---");

                await CreateSalesInvoiceTask();

                await CreateDownPaymentInvoiceTask();

                await CreatePurchaseInvoiceTask();

                await CreateARCNDNTask();

                await CreateAPCNDNTask();

                await GetStatusListTask();

                _logger.LogInformation($"--- Cycle finished. Waiting {PollingInterval.TotalSeconds} seconds... ---");

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("--- E-Invoice Polling Worker Service has stopped. ---");
        }

        private async Task CreateDownPaymentInvoiceTask()
        {
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery("CALL EINV_STAGING.GET_ARDPINVOICE_DETAILS('SERVOMY', ?)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for Downpayment Invoice.");
                return;
            }


            if (oDt == null || oDt.Rows.Count == 0)
            {
                _logger.LogInformation("[DPInvoice Create] No new sales invoices found in HANA staging table.");
                return;
            }

            var invoiceGroups = oDt.AsEnumerable()
                .GroupBy(row => row.Field<int>("RefNo"));

            int invoicesSubmitted = 0;

            foreach (var invoiceGroup in invoiceGroups)
            {
                DataRow headerRow = invoiceGroup.First();

                var newInvoice = new SalesInvoice
                {
                    RefNo = invoiceGroup.Key.ToString(),
                    Podate = Convert.ToDateTime(headerRow["InvDate"]),
                    InvDate = Convert.ToDateTime(headerRow["InvDate"]),
                    PostDate = Convert.ToDateTime(headerRow["PostDate"]),
                    HeaderAmount = Convert.ToDecimal(headerRow["HeaderAmount"]),
                    Remark = headerRow["U_PORemark"].ToString() ?? string.Empty,

                    State = headerRow["State"].ToString() ?? string.Empty,
                    City = headerRow["City"].ToString() ?? string.Empty,
                    TelNo = headerRow["Phone2"].ToString() ?? string.Empty,
                    Address1 = headerRow["Address1"].ToString() ?? string.Empty,
                    Address2 = headerRow["Address2"].ToString() ?? string.Empty,
                    Address3 = headerRow["Address3"].ToString() ?? string.Empty,
                    BName = headerRow["BName"].ToString() ?? string.Empty,
                    BnpType = "BRN",
                    Tin = headerRow["TIN"].ToString() ?? string.Empty,
                    Brn = headerRow["BRN"].ToString() ?? string.Empty,
                    Email = headerRow["Email"].ToString() ?? string.Empty,
                    Country = headerRow["Country"].ToString() ?? string.Empty,
                    PaymentTerm = headerRow["PaymentTerm"].ToString() ?? string.Empty,

                    CreatedBy = headerRow["CreatedBy"].ToString(),

                    Currency = headerRow["Currency"].ToString() ?? string.Empty,
                    CurrencyRate = Convert.ToDouble(headerRow["CurrencyRate"]),
                    CreatedDate = DateTimeOffset.Parse(headerRow["CreatedDate"].ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ")),

                    InvoiceParts = new List<InvoicePart>()
                };

                foreach (DataRow lineRow in invoiceGroup)
                {
                    newInvoice.InvoiceParts.Add(new InvoicePart
                    {
                        Uom = lineRow["UOM"].ToString() ?? string.Empty,
                        OrderQty = Convert.ToDouble(lineRow["OrderQty"]),
                        UnitPrice = Convert.ToDouble(lineRow["UnitPrice"]),
                        Amount = Convert.ToDecimal(lineRow["Amount"]),
                        Description = lineRow["Description"].ToString() ?? string.Empty,
                        Classification = lineRow["Classification"].ToString() ?? string.Empty,
                        TaxType = "06" ?? string.Empty,
                        TaxRate = Convert.ToDouble(lineRow["TaxRate"]),
                        TaxAmount = Convert.ToDecimal(lineRow["TaxAmount"])
                    });
                }

                var result = await _invoiceService.CreateSalesInvoiceAsync(newInvoice);

                if (result.Success)
                {
                    _logger.LogInformation($"[DPInvoice] SUCCESS RefNo {newInvoice.RefNo}: {result.Message}");
                    invoicesSubmitted++;

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EDPI", "Y");
                }
                else
                {
                    _logger.LogError($"[DPInvoice Create] FAILURE RefNo {newInvoice.RefNo}: {result.Message}");

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EDPI", "E", result.Message.Truncate(300));
                }
            }

            _logger.LogInformation($"[DPInvoice Create] Completed cycle. Total invoices processed and submitted: {invoicesSubmitted}");
        }

        private async Task CreateSalesInvoiceTask()
        {
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery("CALL EINV_STAGING.GET_INVOICE_DETAILS('SERVOMY', ?)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for Sales Invoice.");
                return;
            }


            if (oDt == null || oDt.Rows.Count == 0)
            {
                _logger.LogInformation("[Sales Create] No new sales invoices found in HANA staging table.");
                return;
            }

            var invoiceGroups = oDt.AsEnumerable()
                .GroupBy(row => row.Field<int>("RefNo"));

            int invoicesSubmitted = 0;

            foreach (var invoiceGroup in invoiceGroups)
            {
                DataRow headerRow = invoiceGroup.First();

                var newInvoice = new SalesInvoice
                {
                    RefNo = invoiceGroup.Key.ToString(),
                    Podate = Convert.ToDateTime(headerRow["InvDate"]),
                    InvDate = Convert.ToDateTime(headerRow["InvDate"]),
                    PostDate = Convert.ToDateTime(headerRow["PostDate"]),
                    HeaderAmount = Convert.ToDecimal(headerRow["HeaderAmount"]),
                    Remark = headerRow["U_PORemark"].ToString() ?? string.Empty,

                    State = headerRow["State"].ToString() ?? string.Empty,
                    City = headerRow["City"].ToString() ?? string.Empty,
                    TelNo = headerRow["Phone2"].ToString() ?? string.Empty,
                    Address1 = headerRow["Address1"].ToString() ?? string.Empty,
                    Address2 = headerRow["Address2"].ToString() ?? string.Empty,
                    Address3 = headerRow["Address3"].ToString() ?? string.Empty,
                    BName = headerRow["BName"].ToString() ?? string.Empty,
                    BnpType = "BRN",
                    Tin = headerRow["TIN"].ToString() ?? string.Empty,
                    Brn = headerRow["BRN"].ToString() ?? string.Empty,
                    Email = headerRow["Email"].ToString() ?? string.Empty,
                    Country = headerRow["Country"].ToString() ?? string.Empty,
                    PaymentTerm = headerRow["PaymentTerm"].ToString() ?? string.Empty,

                    CreatedBy = headerRow["CreatedBy"].ToString(),

                    Currency = headerRow["Currency"].ToString() ?? string.Empty,
                    CurrencyRate = Convert.ToDouble(headerRow["CurrencyRate"]),
                    CreatedDate = DateTimeOffset.Parse(headerRow["CreatedDate"].ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ")),

                    InvoiceParts = new List<InvoicePart>()
                };

                foreach (DataRow lineRow in invoiceGroup)
                {
                    newInvoice.InvoiceParts.Add(new InvoicePart
                    {
                        Uom = lineRow["UOM"].ToString() ?? string.Empty,
                        OrderQty = Convert.ToDouble(lineRow["OrderQty"]),
                        UnitPrice = Convert.ToDouble(lineRow["UnitPrice"]),
                        Amount = Convert.ToDecimal(lineRow["Amount"]),
                        Description = lineRow["Description"].ToString() ?? string.Empty,
                        Classification = lineRow["Classification"].ToString() ?? string.Empty,
                        TaxType = "06" ?? string.Empty,
                        TaxRate = Convert.ToDouble(lineRow["TaxRate"]),
                        TaxAmount = Convert.ToDecimal(lineRow["TaxAmount"])
                    });
                }

                var result = await _invoiceService.CreateSalesInvoiceAsync(newInvoice);

                if (result.Success)
                {
                    _logger.LogInformation($"[Sales Create] SUCCESS RefNo {newInvoice.RefNo}: {result.Message}");
                    invoicesSubmitted++;

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EINV", "Y");
                }
                else
                {
                    _logger.LogError($"[Sales Create] FAILURE RefNo {newInvoice.RefNo}: {result.Message}");

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EINV", "E", result.Message.Truncate(300));
                }
            }

            _logger.LogInformation($"[Sales Create] Completed cycle. Total invoices processed and submitted: {invoicesSubmitted}");
        }

        private async Task CreatePurchaseInvoiceTask()
        {
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery("CALL EINV_STAGING.GET_PURCHASEINVOICE_DETAILS('SERVOMY', ?)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for Sales Invoice.");
                return;
            }


            if (oDt == null || oDt.Rows.Count == 0)
            {
                _logger.LogInformation("[Purchase Create] No new sales invoices found in HANA staging table.");
                return;
            }

            var invoiceGroups = oDt.AsEnumerable()
                .GroupBy(row => row.Field<int>("RefNo"));

            int invoicesSubmitted = 0;

            foreach (var invoiceGroup in invoiceGroups)
            {
                DataRow headerRow = invoiceGroup.First();

                var newInvoice = new PurchaseInvoice
                {
                    RefNo = invoiceGroup.Key.ToString(),
                    RecDate = Convert.ToDateTime(headerRow["InvDate"]),
                    InvDate = Convert.ToDateTime(headerRow["InvDate"]),
                    PostDate = Convert.ToDateTime(headerRow["PostDate"]),
                    HeaderAmount = Convert.ToDecimal(headerRow["HeaderAmount"]),
                    Remark = headerRow["U_PORemark"].ToString() ?? string.Empty,

                    State = headerRow["State"].ToString() ?? string.Empty,
                    City = headerRow["City"].ToString() ?? string.Empty,
                    TelNo = headerRow["Phone2"].ToString() ?? string.Empty,
                    Address1 = headerRow["Address1"].ToString() ?? string.Empty,
                    Address2 = headerRow["Address2"].ToString() ?? string.Empty,
                    Address3 = headerRow["Address3"].ToString() ?? string.Empty,
                    BName = headerRow["BName"].ToString() ?? string.Empty,
                    BnpType = "BRN",
                    Tin = headerRow["TIN"].ToString() ?? string.Empty,
                    Brn = headerRow["BRN"].ToString() ?? string.Empty,
                    Email = headerRow["Email"].ToString() ?? string.Empty,
                    Country = headerRow["Country"].ToString() ?? string.Empty,
                    PaymentTerm = headerRow["PaymentTerm"].ToString() ?? string.Empty,

                    CreatedBy = headerRow["CreatedBy"].ToString(),

                    Currency = headerRow["Currency"].ToString() ?? string.Empty,
                    CurrencyRate = Convert.ToDouble(headerRow["CurrencyRate"]),
                    CreatedDate = DateTimeOffset.Parse(headerRow["CreatedDate"].ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ")),
                    MsicCode = headerRow["AliasName"].ToString(),

                    InvoiceParts = new List<InvoicePart>()
                };

                foreach (DataRow lineRow in invoiceGroup)
                {
                    newInvoice.InvoiceParts.Add(new InvoicePart
                    {
                        Uom = lineRow["UOM"].ToString() ?? string.Empty,
                        OrderQty = Convert.ToDouble(lineRow["OrderQty"]),
                        UnitPrice = Convert.ToDouble(lineRow["UnitPrice"]),
                        Amount = Convert.ToDecimal(lineRow["Amount"]),
                        Description = lineRow["Description"].ToString() ?? string.Empty,
                        Classification = lineRow["Classification"].ToString() ?? string.Empty,
                        TaxType = "06" ?? string.Empty,
                        TaxRate = Convert.ToDouble(lineRow["TaxRate"]),
                        TaxAmount = Convert.ToDecimal(lineRow["TaxAmount"])
                    });
                }

                var result = await _invoiceService.CreatePurchaseInvoiceAsync(newInvoice);

                if (result.Success)
                {
                    _logger.LogInformation($"[Purchase Create] SUCCESS RefNo {newInvoice.RefNo}: {result.Message}");
                    invoicesSubmitted++;

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EPCH", "Y");
                }
                else
                {
                    _logger.LogError($"[Purchase Create] FAILURE RefNo {newInvoice.RefNo}: {result.Message}");

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "EPCH", "E", result.Message.Truncate(300));
                }
            }

            _logger.LogInformation($"[Purchase Create] Completed cycle. Total invoices processed and submitted: {invoicesSubmitted}");
        }

        private async Task CreateARCNDNTask()
        {
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery("CALL EINV_STAGING.GET_ARCREDITMEMO_DETAILS('SERVOMY', ?)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for AR Credit Memo.");
                return;
            }


            if (oDt == null || oDt.Rows.Count == 0)
            {
                _logger.LogInformation("[ARCM Create] No new AR CM found in HANA staging table.");
                return;
            }

            var invoiceGroups = oDt.AsEnumerable()
                .GroupBy(row => row.Field<int>("RefNo"));

            int invoicesSubmitted = 0;

            foreach (var invoiceGroup in invoiceGroups)
            {
                DataRow headerRow = invoiceGroup.First();

                var newInvoice = new ARCreditMemo
                {
                    RefNo = invoiceGroup.Key.ToString(),
                    Date = Convert.ToDateTime(headerRow["PostDate"]),
                    HeaderAmount = Convert.ToDecimal(headerRow["HeaderAmount"]),
                    Remark = headerRow["U_PORemark"].ToString() ?? string.Empty,

                    State = headerRow["State"].ToString() ?? string.Empty,
                    City = headerRow["City"].ToString() ?? string.Empty,
                    TelNo = headerRow["Phone2"].ToString() ?? string.Empty,
                    Address1 = headerRow["Address1"].ToString() ?? string.Empty,
                    Address2 = headerRow["Address2"].ToString() ?? string.Empty,
                    Address3 = headerRow["Address3"].ToString() ?? string.Empty,
                    BName = headerRow["BName"].ToString() ?? string.Empty,
                    BnpType = "BRN",
                    Tin = headerRow["TIN"].ToString() ?? string.Empty,
                    Brn = headerRow["BRN"].ToString() ?? string.Empty,
                    Email = headerRow["Email"].ToString() ?? string.Empty,
                    Country = headerRow["Country"].ToString() ?? string.Empty,

                    CreatedBy = headerRow["CreatedBy"].ToString(),

                    Currency = headerRow["Currency"].ToString() ?? string.Empty,
                    CurrencyRate = Convert.ToDouble(headerRow["CurrencyRate"]),
                    CreatedDate = DateTimeOffset.Parse(headerRow["CreatedDate"].ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ")),

                    _Type = headerRow["Type"].ToString() ?? string.Empty,
                    arcndnpart = new List<CreditMemoDetail>()
                };

                foreach (DataRow lineRow in invoiceGroup)
                {
                    newInvoice.arcndnpart.Add(new CreditMemoDetail
                    {
                        Uom = lineRow["UOM"].ToString() ?? string.Empty,
                        Qty = Convert.ToDouble(lineRow["OrderQty"]),
                        UnitPrice = Convert.ToDouble(lineRow["UnitPrice"]),
                        Amount = Convert.ToDecimal(lineRow["Amount"]),
                        Description = lineRow["Description"].ToString() ?? string.Empty,
                        Classification = lineRow["Classification"].ToString() ?? string.Empty,
                        TaxType = "06" ?? string.Empty,
                        TaxRate = Convert.ToDouble(lineRow["TaxRate"]),
                        TaxAmount = Convert.ToDecimal(lineRow["TaxAmount"])
                    });
                }

                var result = await _invoiceService.CreateARCMAsync(newInvoice);

                if (result.Success)
                {
                    _logger.LogInformation($"[ARCM Create] SUCCESS RefNo {newInvoice.RefNo}: {result.Message}");
                    invoicesSubmitted++;

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "ERIN", "Y");
                }
                else
                {
                    _logger.LogError($"[ARCM Create] FAILURE RefNo {newInvoice.RefNo}: {result.Message}");

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "ERIN", "E", result.Message.Truncate(300));
                }
            }

            _logger.LogInformation($"[ARCM Create] Completed cycle. Total invoices processed and submitted: {invoicesSubmitted}");
        }

        private async Task CreateAPCNDNTask()
        {
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery("CALL EINV_STAGING.GET_APCREDITMEMO_DETAILS('SERVOMY', ?)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for AP Credit Memo.");
                return;
            }


            if (oDt == null || oDt.Rows.Count == 0)
            {
                _logger.LogInformation("[APCM Create] No new AP CM found in HANA staging table.");
                return;
            }

            var invoiceGroups = oDt.AsEnumerable()
                .GroupBy(row => row.Field<int>("RefNo"));

            int invoicesSubmitted = 0;

            foreach (var invoiceGroup in invoiceGroups)
            {
                DataRow headerRow = invoiceGroup.First();

                var newInvoice = new APCreditMemo
                {
                    RefNo = invoiceGroup.Key.ToString(),
                    Date = Convert.ToDateTime(headerRow["PostDate"]),
                    HeaderAmount = Convert.ToDecimal(headerRow["HeaderAmount"]),
                    Remark = headerRow["U_PORemark"].ToString() ?? string.Empty,

                    State = headerRow["State"].ToString() ?? string.Empty,
                    City = headerRow["City"].ToString() ?? string.Empty,
                    TelNo = headerRow["Phone2"].ToString() ?? string.Empty,
                    Address1 = headerRow["Address1"].ToString() ?? string.Empty,
                    Address2 = headerRow["Address2"].ToString() ?? string.Empty,
                    Address3 = headerRow["Address3"].ToString() ?? string.Empty,
                    BName = headerRow["BName"].ToString() ?? string.Empty,
                    BnpType = "BRN",
                    Tin = headerRow["TIN"].ToString() ?? string.Empty,
                    Brn = headerRow["BRN"].ToString() ?? string.Empty,
                    Email = headerRow["Email"].ToString() ?? string.Empty,
                    Country = headerRow["Country"].ToString() ?? string.Empty,

                    CreatedBy = headerRow["CreatedBy"].ToString(),

                    Currency = headerRow["Currency"].ToString() ?? string.Empty,
                    CurrencyRate = Convert.ToDouble(headerRow["CurrencyRate"]),
                    CreatedDate = DateTimeOffset.Parse(headerRow["CreatedDate"].ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ")),

                    _Type = headerRow["Type"].ToString() ?? string.Empty,

                    apcndnpart = new List<CreditMemoDetail>()
                };

                foreach (DataRow lineRow in invoiceGroup)
                {
                    newInvoice.apcndnpart.Add(new CreditMemoDetail
                    {
                        Uom = lineRow["UOM"].ToString() ?? string.Empty,
                        Qty = Convert.ToDouble(lineRow["OrderQty"]),
                        UnitPrice = Convert.ToDouble(lineRow["UnitPrice"]),
                        Amount = Convert.ToDecimal(lineRow["Amount"]),
                        Description = lineRow["Description"].ToString() ?? string.Empty,
                        Classification = lineRow["Classification"].ToString() ?? string.Empty,
                        TaxType = "06" ?? string.Empty,
                        TaxRate = Convert.ToDouble(lineRow["TaxRate"]),
                        TaxAmount = Convert.ToDecimal(lineRow["TaxAmount"])
                    });
                }

                var result = await _invoiceService.CreateAPCMAsync(newInvoice);

                if (result.Success)
                {
                    _logger.LogInformation($"[ARCM Create] SUCCESS RefNo {newInvoice.RefNo}: {result.Message}");
                    invoicesSubmitted++;

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "ERPC", "Y");
                }
                else
                {
                    _logger.LogError($"[APCM Create] FAILURE RefNo {newInvoice.RefNo}: {result.Message}");

                    await UpdateRecord(headerRow["DocEntry"].ToString(), "ERPC", "E", result.Message.Truncate(300));
                }
            }

            _logger.LogInformation($"[APCM Create] Completed cycle. Total invoices processed and submitted: {invoicesSubmitted}");
        }

        private async Task GetStatusListTask()
        {
            foreach (var docType in DocumentTypesToCheck)
            {
                List<Reference> references = GetReferences(docType);
                if (references.Count > 0)
                {
                    List<string> refNo = references
                    .Select(r => r.DocNum)
                    .ToList();

                    var request = new EInvoiceStatusRequest
                    {
                        DocType = docType,
                        RefNo = refNo
                    };

                    var listOfInvoices = await _invoiceService.GetEInvoiceStatusListAsync(request);
                    if (listOfInvoices.Count > 0)
                    {
                        List<EInvoiceStatusData> validatedInvoices = listOfInvoices.Where(r => r.Status == "Validated").ToList();
                        if (validatedInvoices.Count > 0)
                        {
                            foreach (EInvoiceStatusData data in validatedInvoices)
                            {
                                Reference reference = references.Where(w => w.DocNum == data.RefNo).FirstOrDefault();

                                await UpdateRecord(reference.DocEntry, reference.TableName, "U", "");
                                await UpdateSAPRecord(reference.SAPTableName, reference.DocEntry, data.EInvIRBMNo, data.EInvValDate.ToString(), data.EInvValLink);
                            }
                        }
                    }

                }
            }
        }

        private static readonly string[] DocumentTypesToCheck = new[]
        {
            "SInvoice",
            "PInvoice",
            "ARCNDN",
            "APCNDN"
        };

        private List<Reference> GetReferences(string tableName)
        {
            List<Reference> references = new List<Reference>();
            Reference reference;
            DataTable oDt = null;
            try
            {
                oDt = _hanaService.HanaSelectQuery($"SELECT * FROM EINV_STAGING.\"GetCapturedDocs\" WHERE \"DocType\" = '{tableName}' ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute HANA Select Query for AP Credit Memo.");
                return references;
            }

            if (oDt == null || oDt.Rows.Count == 0)
            {
                return references;
            }

            foreach (DataRow _ref in oDt.Rows)
            {
                reference = new Reference();
                reference.SAPTableName = _ref["SAPTableName"].ToString();
                reference.TableName = _ref["TableName"].ToString();
                reference.DocEntry = _ref["DocEntry"].ToString();
                reference.DocNum = _ref["DocNum"].ToString();
                reference.DocType = _ref["DocType"].ToString();

                references.Add(reference);
            }

            return references;
        }

        async Task UpdateRecord(string docEntry, string tableName, string status, string message = "")
        {
            try
            {
                
                string dateColumn = "UPDATE_DATE";
                switch (status)
                {
                    case "U": 
                        dateColumn = "CAPTURED_DATE";
                        break;
                    default:
                        dateColumn = "UPDATE_DATE";
                        break;
                }
                 
                string safeMessage = message?.Replace("'", "''") ?? "";
                 
                string sqlQuery = $@"UPDATE EINV_STAGING.""{tableName}"" 
                             SET ""isCaptured"" = '{status}', 
                                 ""StatusMsg"" = '{safeMessage}', 
                                 ""{dateColumn}"" = now() 
                             WHERE ""DocEntry"" = {docEntry}";
                 
                await _hanaService.HanaExecuteNonQuery(sqlQuery);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, $"[UpdateRecord Error] Failed to update {tableName} for DocEntry: {docEntry}. Status intended: {status}");
            }
        }

        async Task UpdateSAPRecord(string sapTableName, string docEntry, string EInvIRBMNo, string EInvValDate, string EInvValLink)
        {
            await _hanaService.HanaExecuteNonQuery($"UPDATE SERVOMY.\"{sapTableName}\" SET \"U_EInvIRBMNo\" = '{EInvIRBMNo}', \"U_EInvValDate\" = '{EInvValDate}', \"U_EInvValLink\" = '{EInvValLink}' WHERE \"DocEntry\" = {docEntry}");
        }

    }

}
