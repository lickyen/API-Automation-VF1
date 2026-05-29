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

        public WabaService(WabaRepository repository, PLFService plfService, AppSettings appSettings, IWebHostEnvironment env)
        {
            _repository = repository;
            _plfService = plfService;
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

            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // 8 char short guid
            var fileName = $"SOA_{lan}_{uniqueId}.pdf";     // e.g. SOA_LAN123456_a3f8c21d.pdf

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
    }
}
