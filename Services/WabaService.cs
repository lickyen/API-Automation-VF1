using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using Serilog;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class WabaService
    {
        private readonly WabaRepository _repository;
        private readonly PLFService _plfService;
        private readonly SharedService _sharedService;
        private readonly KotakService _kotakService;
        private readonly AppSettings _appSettings;
        private readonly IWebHostEnvironment _env;

        private static readonly Serilog.ILogger _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: "logs/WABA/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            )
            .CreateLogger();

        public WabaService(WabaRepository repository, PLFService plfService, SharedService sharedService, KotakService kotakService, AppSettings appSettings, IWebHostEnvironment env)
        {
            _repository = repository;
            _plfService = plfService;
            _sharedService = sharedService;
            _kotakService = kotakService;
            _appSettings = appSettings;
            _env = env;
        }
        public async Task<int> SaveWabaLogAsync(WabaModel dto)
        {
            var json = JsonSerializer.Serialize(dto);

            _logger.Information(
                "Request Received: {RequestBody}",
                json
            );

            return await _repository.SavaWabaLogAsync(dto);
        }

        public async Task<WabaSOAResponse> GetSOAAsync(string lan)
        {
            // Step 0: Get finance details
            var finance = await _plfService.GetFinance(lan);
            var today = DateTime.Today;

            var fromDate = finance?.financeSchedule?.financeDetail?.firstDisbDate;
            if (fromDate == null)
                throw new Exception("Disbursement Date not found for the given LAN");

            // Step 1: Save request to DB — status = CREATED
            var requestId = await _repository.CreateSOARequestAsync(
                lan: lan,
                disbDate: (DateTime)fromDate
            );

            // Step 2: Generate Statement
            var request = new GenerateStatementRequest
            {
                FinReference = lan,
                FromDate = (DateTime)fromDate,
                ToDate = today,
                ServiceName = "Annual Loan Statement"
            };

            var generateResult = await _plfService.GenerateStatement(request);
            if (generateResult == null || generateResult.ReturnStatus?.returnCode != "0000")
                throw new Exception("LAN not found or InActive");

            var letterId = generateResult.LetterID;
            if (letterId == 0)
                throw new Exception("Generate Statement API did not return a valid Letter ID");

            // Step 3: Transfer Letter
            var transferRequest = new TransferLetterRequest
            {
                ServiceName = "Annual Loan Statement",
                RequestID = letterId.ToString()
            };

            var transferResult = await _plfService.TransferLetter(transferRequest);
            if (transferResult == null || string.IsNullOrWhiteSpace(transferResult.DocContent))
                throw new Exception("Transfer Letter API returned empty document");

            // Step 4: Save PDF to disk
            //var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "WabaFiles");
            var basePath = Path.Combine(_env.WebRootPath, "WabaFiles");

            //var uniqueId = Guid.NewGuid().ToString("N")[..8]; // 8 char short guid
            //var fileName = $"SOA_{lan}_{uniqueId}.pdf";     // e.g. SOA_LAN123456_a3f8c21d.pdf

            var fileName = $"{Guid.NewGuid():N}.pdf";

            var fullPath = Path.Combine(basePath, fileName);

            Directory.CreateDirectory(basePath);
            await File.WriteAllBytesAsync(
                fullPath,
                Convert.FromBase64String(transferResult.DocContent)
            );

            var fileUrl = $"{_appSettings.BaseUrl}/WabaFiles/{fileName}";

            // Step 5: Update letter path in DB
            await _repository.UpdateSOALetterIssuedAsync(requestId, fileUrl);

            // Step 6: Return URL
            return new WabaSOAResponse
            {
                FileName = fileName,
                FileUrl = fileUrl,
                FinReference = transferResult.FinReference
            };
        }
        public async Task<WabaKotakResponse> GetKotakDocumentAsync(string lan)
        {
            // Step 1: Pull Kotak master data for this LAN
            var kotakData = await _sharedService.GetKotakByLanAsync(lan);
            if (kotakData == null)
                throw new Exception("Kotak data not found for the given LAN");

            if (string.IsNullOrWhiteSpace(kotakData.LoanAcId))
                throw new Exception("Loan Account ID is missing in Kotak data");

            if (kotakData.Dob == null)
                throw new Exception("Date of Birth is missing in Kotak data");

            if (string.IsNullOrWhiteSpace(kotakData.ClientName))
                throw new Exception("Client name is missing in Kotak data");

            // Step 2: Save request to DB — status = CREATED
            var requestId = await _repository.CreateKotakRequestAsync(lan);

            // Step 3: Call Kotak ValidateCOICS API
            var kotakRequest = new KotakValidateRequest
            {
                Request = new KotakRequestBody
                {
                    LoanAccountNo = kotakData.LoanAcId,
                    DOB = kotakData.Dob.Value.ToString("dd/MM/yyyy"),
                    PolicyHolderName = kotakData.ClientName
                }
            };

            var kotakResult = await _kotakService.ValidateCOICSAsync(kotakRequest);

            // Validate response structure
            if (kotakResult?.Response == null)
                throw new Exception("Kotak API returned no response");

            if (!string.Equals(kotakResult.Response.StatusInfo?.Status, "Success", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Kotak API error: {kotakResult.Response.StatusInfo?.StatusMessage ?? "Unknown error"}");

            if (string.IsNullOrWhiteSpace(kotakResult.Response.PDFContent))
                throw new Exception("Kotak API returned an empty PDF");

            // Step 4: Save PDF to disk
            var basePath = Path.Combine(_env.WebRootPath, "WabaFiles");
            Directory.CreateDirectory(basePath);

            //var uniqueId = Guid.NewGuid().ToString("N")[..8];
            //var fileName = $"{lan}_kotak_{uniqueId}.pdf";

            var fileName = $"{Guid.NewGuid():N}.pdf";

            var fullPath = Path.Combine(basePath, fileName);

            await File.WriteAllBytesAsync(
                fullPath,
                Convert.FromBase64String(kotakResult.Response.PDFContent)  // ← PDFContent not DocContent
            );

            var fileUrl = $"{_appSettings.BaseUrl}/WabaFiles/{fileName}";

            await _repository.UpdateKotakDocumentIssuedAsync(requestId, fileUrl);

            return new WabaKotakResponse
            {
                FileName = fileName,
                FileUrl = fileUrl,
                LoanAccountNo = kotakData.LoanAcId,
                PolicyNumber = kotakData.PolicyNumber ?? string.Empty
            };
        }

        public (byte[] bytes, string fileName) GetDocument(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(_env.WebRootPath, "WabaFiles", safeFileName);

            if (!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException("File not found");

            return (System.IO.File.ReadAllBytes(fullPath), safeFileName);
        }
    }
}
