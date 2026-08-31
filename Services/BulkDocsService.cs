using System.IO.Compression;
using ClosedXML.Excel;
using Newtonsoft.Json;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BulkDocsService
    {
        private const string SuccessDocStatus = "Success";
        private const string ErrorDocStatus = "Error";
        private const string SoaDocCode = "SOA";
        private const string SoaServiceName = "Annual Loan Statement";
        private const int MaxErrorMessageLength = 4000;
        private const long MaxDownloadZipBytes = 250L * 1024 * 1024;
        private const int MaxBatchIdLength = 50;
        private const int BatchTimestampLength = 15;
        private const string BatchIdFallbackPrefix = "USER";

        private readonly BulkDocsRepository _repository;
        private readonly PLFService _plfService;
        private readonly string _filePath;
        private readonly ILogger<BulkDocsService> _logger;

        public BulkDocsService(
            BulkDocsRepository repository,
            PLFService plfService,
            AppSettings appSettings,
            ILogger<BulkDocsService> logger)
        {
            _repository = repository;
            _plfService = plfService;
            _filePath = appSettings.FilePaths.BulkDocsFilePath;
            _logger = logger;
        }

        public async Task<BulkDocsAddResult> UploadAsync(IFormFile file, string createdBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required.");

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy is required.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                throw new ArgumentException("Invalid file type. Please upload an Excel file.");

            List<BulkDocItem> items;

            using (var stream = file.OpenReadStream())
            {
                items = ReadItemsFromExcel(stream);
            }

            if (items.Count == 0)
                throw new ArgumentException("No records found in the uploaded file.");

            var batchId = GenerateBatchId(createdBy);
            var result = new BulkDocsAddResult
            {
                BatchId = batchId
            };

            foreach (var item in items)
            {
                try
                {
                    var output = await _repository.AddLanAsync(item, createdBy.Trim(), batchId);
                    if (output == 1)
                        result.RowsAdded++;
                    else if (output == -1)
                        result.Errors.Add($"LAN {item.Lan}: Already exists in this batch.");
                    else
                        result.Errors.Add($"LAN {item.Lan}: Could not be added.");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"LAN {item.Lan}: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<List<BulkDocsBatchListItem>> GetBatchesAsync(BulkDocsGetBatchesRequest request)
        {
            var batches = (await _repository.GetBatchesAsync(request)).ToList();

            foreach (var batch in batches.Where(x => string.IsNullOrWhiteSpace(x.Status)))
            {
                batch.Status = batch.ActiveCount > 0 ? "Active" : "Processed";
            }

            return batches;
        }

        public async Task<List<BulkDocsModel>> GetLansAsync(BulkDocsGetLansRequest? request = null)
        {
            var items = await _repository.GetLansAsync(request);
            return items.ToList();
        }

        public async Task<BulkDocsFetchResult> FetchDocumentAsync(
            BulkDocsFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new BulkDocsFetchResult
            {
                BatchId = request?.BatchId?.Trim() ?? string.Empty,
                Lan = request?.Lan?.Trim() ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(result.BatchId) && !IsSafeBatchId(result.BatchId))
                return Fail(result, "Valid BatchId is required.");

            if (string.IsNullOrWhiteSpace(result.Lan))
                return Fail(result, "LAN is required.");

            var doc = string.IsNullOrWhiteSpace(result.BatchId)
                ? await GetLanAsync(result.Lan)
                : await GetLanAsync(result.BatchId, result.Lan);

            if (doc == null)
            {
                var message = string.IsNullOrWhiteSpace(result.BatchId)
                    ? $"LAN {result.Lan} was not found."
                    : $"LAN {result.Lan} was not found for BatchId {result.BatchId}.";

                return Fail(result, message);
            }

            var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy)
                ? "System"
                : request.UpdatedBy.Trim();

            return await FetchDocumentAsync(doc, updatedBy, cancellationToken);
        }

        public async Task<BulkDocsFetchBatchResult> FetchBatchAsync(
            BulkDocsFetchBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new BulkDocsFetchBatchResult
            {
                BatchId = request?.BatchId?.Trim() ?? string.Empty
            };

            if (!IsSafeBatchId(result.BatchId))
                return FailBatch(result, "Valid BatchId is required.");

            var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy)
                ? "System"
                : request.UpdatedBy.Trim();

            var docs = (await _repository.GetLansAsync(new BulkDocsGetLansRequest
            {
                BatchId = result.BatchId,
                Status = "Active"
            })).ToList();

            result.TotalLans = docs.Count;

            if (!docs.Any())
                return FailBatch(result, $"No active LANs found for BatchId {result.BatchId}.");

            foreach (var doc in docs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Results.Add(await FetchDocumentAsync(doc, updatedBy, cancellationToken));
            }

            SetFetchBatchCounts(result);
            return result;
        }

        private async Task<BulkDocsFetchResult> FetchDocumentAsync(
            BulkDocsModel doc,
            string updatedBy,
            CancellationToken cancellationToken)
        {
            var result = new BulkDocsFetchResult
            {
                BatchId = doc.BatchId ?? string.Empty,
                Lan = doc.Lan?.Trim() ?? string.Empty
            };

            try
            {
                ValidateFetchConfiguration();
                cancellationToken.ThrowIfCancellationRequested();

                var fetch = await GetSoaDocumentAsync(doc, cancellationToken);
                var fileName = await SaveDocumentAsync(doc, fetch.DocumentBytes, cancellationToken);

                await UpdateDocStatusAsync(doc.Id, SuccessDocStatus, fetch.PlfResponse, null, updatedBy);

                result.Success = true;
                result.DocStatus = SuccessDocStatus;
                result.FileName = fileName;
                result.Message = "Document fetched successfully.";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorMessage = BuildErrorMessage(ex.Message);
                await TryUpdateDocStatusAsync(doc.Id, ErrorDocStatus, null, errorMessage, updatedBy);

                result.Success = false;
                result.DocStatus = ErrorDocStatus;
                result.ErrorMessage = errorMessage ?? string.Empty;
                result.Message = result.ErrorMessage;

                _logger.LogError(ex, "BulkDocs SOA fetch failed for BatchId {BatchId}, LAN {Lan}", result.BatchId, result.Lan);
            }

            return result;
        }

        public async Task<BulkDocsDownloadResult> DownloadZipAsync(
            BulkDocsDownloadRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new BulkDocsDownloadResult
            {
                BatchId = request?.BatchId?.Trim() ?? string.Empty,
                MaxAllowedBytes = MaxDownloadZipBytes
            };

            if (!IsSafeBatchId(result.BatchId))
                return FailDownload(result, "Valid BatchId is required.");

            if (string.IsNullOrWhiteSpace(_filePath))
                return FailDownload(result, "FilePaths:BulkDocsFilePath is required.");

            if (!Directory.Exists(_filePath))
                return FailDownload(result, $"Source path was not found: {_filePath}");

            var batchDocs = await GetLansByBatchId(result.BatchId);
            if (!batchDocs.Any())
                return FailDownload(result, $"No LANs found for BatchId {result.BatchId}.");

            var batchFolderPath = GetBatchFolderPath(result.BatchId, batchDocs.First().CreatedBy);
            result.SourcePath = batchFolderPath;

            if (!Directory.Exists(batchFolderPath))
                return FailDownload(result, $"Batch folder was not found: {batchFolderPath}");

            var fetchedDocs = batchDocs
                .Where(x => string.Equals(x.DocStatus, SuccessDocStatus, StringComparison.OrdinalIgnoreCase))
                .ToList();

            result.TotalDocuments = fetchedDocs.Count;

            if (!fetchedDocs.Any())
                return FailDownload(result, $"No successfully fetched documents found for BatchId {result.BatchId}.");

            var files = new List<(string SourceFile, string EntryName)>();
            var entryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in fetchedDocs)
            {
                var fileName = BuildDocumentFileName(doc);
                var sourceFile = Path.Combine(batchFolderPath, fileName);

                if (!File.Exists(sourceFile))
                {
                    result.MissingFiles.Add(fileName);
                    continue;
                }

                result.TotalBytes += new FileInfo(sourceFile).Length;
                files.Add((sourceFile, GetUniqueZipEntryName(fileName, entryNames)));
            }

            if (result.MissingFiles.Any())
                return FailDownload(result, $"Some files are missing for BatchId {result.BatchId}.");

            if (result.TotalBytes > MaxDownloadZipBytes)
            {
                return FailDownload(
                    result,
                    $"Batch size is {FormatBytes(result.TotalBytes)}. Maximum allowed download size is {FormatBytes(MaxDownloadZipBytes)}.");
            }

            await using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entry = archive.CreateEntry(file.EntryName, CompressionLevel.Fastest);

                    await using var sourceStream = new FileStream(
                        file.SourceFile,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 81920,
                        useAsync: true);

                    await using var entryStream = entry.Open();
                    await sourceStream.CopyToAsync(entryStream, cancellationToken);

                    result.FileCount++;
                }
            }

            result.Success = true;
            result.FileName = $"{BuildSafeFileName(result.BatchId)}.zip";
            result.FileContent = zipStream.ToArray();
            result.Message = $"{result.FileCount} file(s) added to zip.";

            return result;
        }

        private List<BulkDocItem> ReadItemsFromExcel(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            var items = new List<BulkDocItem>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var lan = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(lan))
                    continue;

                var docType = row.Cell(2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(docType))
                    docType = SoaDocCode;

                items.Add(new BulkDocItem
                {
                    Lan = lan,
                    DocType = docType,
                    FromDate = ReadOptionalDate(row.Cell(3), row.RowNumber(), "SOA_FromDt"),
                    ToDate = ReadOptionalDate(row.Cell(4), row.RowNumber(), "SOA_ToDt")
                });
            }

            return items;
        }

        private async Task<BulkDocsModel?> GetLanAsync(string lan)
        {
            var docs = await _repository.GetLansAsync();

            return docs.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Lan)
                && string.Equals(x.Lan.Trim(), lan, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<BulkDocsModel?> GetLanAsync(string batchId, string lan)
        {
            var docs = await _repository.GetLansAsync(new BulkDocsGetLansRequest
            {
                BatchId = batchId
            });

            return docs.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Lan)
                && string.Equals(x.Lan.Trim(), lan, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<BulkDocsModel>> GetLansByBatchId(string batchId)
        {
            var docs = await _repository.GetLansAsync(new BulkDocsGetLansRequest
            {
                BatchId = batchId
            });

            return docs.ToList();
        }

        private async Task<(byte[] DocumentBytes, string PlfResponse)> GetSoaDocumentAsync(
            BulkDocsModel doc,
            CancellationToken cancellationToken)
        {
            var serviceName = GetSoaServiceName(doc.RequiredDoc);

            if (!doc.SOA_FromDt.HasValue)
                throw new InvalidOperationException("SOA_FromDt is required.");

            if (!doc.SOA_ToDt.HasValue)
                throw new InvalidOperationException("SOA_ToDt is required.");

            var generateRequest = new GenerateStatementRequest
            {
                FinReference = doc.Lan,
                FromDate = doc.SOA_FromDt.Value,
                ToDate = doc.SOA_ToDt.Value,
                ServiceName = serviceName
            };

            var generateResult = await _plfService.GenerateStatement(generateRequest);
            ValidateGenerateStatementResult(generateResult);

            cancellationToken.ThrowIfCancellationRequested();

            var transferRequest = new TransferLetterRequest
            {
                ServiceName = serviceName,
                RequestID = generateResult.LetterID.ToString()
            };

            var transferResult = await _plfService.TransferLetter(transferRequest);
            ValidateTransferLetterResult(transferResult);

            var documentBytes = DecodeBase64Document(transferResult.DocContent);
            var plfResponse = BuildPlfResponse(generateResult, transferResult);

            return (documentBytes, plfResponse);
        }

        private static void ValidateGenerateStatementResult(GenerateStatementResponse? result)
        {
            if (result == null)
                throw new InvalidOperationException("GenerateStatement API returned empty response.");

            var returnCode = result.ReturnStatus?.returnCode;
            if (!string.IsNullOrWhiteSpace(returnCode) && returnCode != "0000")
                throw new InvalidOperationException($"{returnCode}-{result.ReturnStatus?.returnText}");

            if (result.LetterID <= 0)
                throw new InvalidOperationException("GenerateStatement API did not return a valid LetterID.");
        }

        private static void ValidateTransferLetterResult(TransferLetterResponse? result)
        {
            if (result == null)
                throw new InvalidOperationException("TransferLetter API returned empty response.");

            var returnCode = result.ReturnStatus?.returnCode;
            if (!string.IsNullOrWhiteSpace(returnCode) && returnCode != "0000")
                throw new InvalidOperationException($"{returnCode}-{result.ReturnStatus?.returnText}");

            if (string.IsNullOrWhiteSpace(result.DocContent))
                throw new InvalidOperationException("TransferLetter API returned empty document.");
        }

        private async Task<string> SaveDocumentAsync(
            BulkDocsModel doc,
            byte[] documentBytes,
            CancellationToken cancellationToken)
        {
            var batchFolderPath = GetBatchFolderPath(doc.BatchId, doc.CreatedBy);
            Directory.CreateDirectory(batchFolderPath);

            var fileName = BuildDocumentFileName(doc);
            var fullPath = Path.Combine(batchFolderPath, fileName);

            await File.WriteAllBytesAsync(fullPath, documentBytes, cancellationToken);

            return fileName;
        }

        private async Task UpdateDocStatusAsync(
            int id,
            string docStatus,
            string? plfResponse,
            string? errorMessage,
            string updatedBy)
        {
            var output = await _repository.UpdateStatusAsync(
                id,
                docStatus,
                updatedBy,
                plfResponse,
                BuildErrorMessage(errorMessage));

            if (output != 1)
                _logger.LogWarning("BulkDocs status update returned {Output} for ID {Id}", output, id);
        }

        private async Task TryUpdateDocStatusAsync(
            int id,
            string docStatus,
            string? plfResponse,
            string? errorMessage,
            string updatedBy)
        {
            try
            {
                await UpdateDocStatusAsync(id, docStatus, plfResponse, errorMessage, updatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update BulkDocs status for ID {Id}", id);
            }
        }

        private void ValidateFetchConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_filePath))
                throw new InvalidOperationException("FilePaths:BulkDocsFilePath is required.");
        }

        private string GetBatchFolderPath(string batchId, string? createdBy)
        {
            return Path.Combine(
                _filePath,
                BuildSafePathSegment(createdBy, "UNKNOWN"),
                BuildSafeFileName(batchId));
        }

        private static string GetSoaServiceName(string? requiredDoc)
        {
            if (IsSoaDocument(requiredDoc))
                return SoaServiceName;

            throw new InvalidOperationException("BulkDocs fetch currently supports SOA only.");
        }

        private static bool IsSoaDocument(string? requiredDoc)
        {
            if (string.IsNullOrWhiteSpace(requiredDoc))
                return true;

            var value = requiredDoc.Trim();
            return value.Equals(SoaDocCode, StringComparison.OrdinalIgnoreCase)
                || value.Equals(SoaServiceName, StringComparison.OrdinalIgnoreCase)
                || value.Contains("statement", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildDocumentFileName(BulkDocsModel doc)
        {
            return $"{BuildSafeFileName(doc.Lan)}_{SoaDocCode}.pdf";
        }

        private static byte[] DecodeBase64Document(string value)
        {
            try
            {
                var base64 = value.Contains(",")
                    ? value[(value.IndexOf(',') + 1)..]
                    : value;

                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("TransferLetter API returned invalid base64 document content.", ex);
            }
        }

        private static string BuildPlfResponse(
            GenerateStatementResponse generateResult,
            TransferLetterResponse transferResult)
        {
            return JsonConvert.SerializeObject(new
            {
                GenerateStatement = new
                {
                    generateResult.FinReference,
                    generateResult.FromDate,
                    generateResult.ToDate,
                    generateResult.LetterID,
                    generateResult.ServiceName,
                    ReturnCode = generateResult.ReturnStatus?.returnCode,
                    ReturnText = generateResult.ReturnStatus?.returnText
                },
                TransferLetter = new
                {
                    transferResult.FinReference,
                    transferResult.FromDate,
                    transferResult.ToDate,
                    transferResult.ServiceName,
                    ReturnCode = transferResult.ReturnStatus?.returnCode,
                    ReturnText = transferResult.ReturnStatus?.returnText
                }
            });
        }

        private static DateTime? ReadOptionalDate(IXLCell cell, int rowNumber, string columnName)
        {
            if (cell.IsEmpty())
                return null;

            if (cell.TryGetValue<DateTime>(out var date))
                return date;

            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (DateTime.TryParse(text, out var parsed))
                return parsed;

            throw new ArgumentException($"Invalid {columnName} at row {rowNumber}.");
        }

        private static string GenerateBatchId(string createdBy)
        {
            var safePrefix = BuildSafeBatchIdPrefix(createdBy);
            return $"{safePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private static string BuildSafeBatchIdPrefix(string? value)
        {
            var input = (value ?? string.Empty).Trim();
            var chars = new char[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                chars[i] = char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'
                    ? ch
                    : '_';
            }

            var prefix = new string(chars).Trim('_', '-');
            if (string.IsNullOrWhiteSpace(prefix))
                return BatchIdFallbackPrefix;

            var maxPrefixLength = MaxBatchIdLength - BatchTimestampLength - 1;
            if (prefix.Length > maxPrefixLength)
                prefix = prefix.Substring(0, maxPrefixLength).Trim('_', '-');

            return string.IsNullOrWhiteSpace(prefix)
                ? BatchIdFallbackPrefix
                : prefix;
        }

        private static string BuildSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(value
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(safeName)
                ? Guid.NewGuid().ToString("N")
                : safeName;
        }

        private static string BuildSafePathSegment(string? value, string fallback)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string((value ?? string.Empty)
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(safeName)
                ? fallback
                : safeName;
        }

        private static string? BuildErrorMessage(string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return null;

            var trimmed = errorMessage.Trim();
            return trimmed.Length <= MaxErrorMessageLength
                ? trimmed
                : trimmed[..MaxErrorMessageLength];
        }

        private static BulkDocsFetchResult Fail(BulkDocsFetchResult result, string message)
        {
            result.Success = false;
            result.DocStatus = ErrorDocStatus;
            result.ErrorMessage = BuildErrorMessage(message) ?? string.Empty;
            result.Message = message;
            return result;
        }

        private static BulkDocsFetchBatchResult FailBatch(BulkDocsFetchBatchResult result, string message)
        {
            result.Success = false;
            result.Message = message;
            return result;
        }

        private static void SetFetchBatchCounts(BulkDocsFetchBatchResult result)
        {
            result.SuccessCount = result.Results.Count(x => x.Success);
            result.ErrorCount = result.Results.Count(x => !x.Success);
            result.Success = result.TotalLans > 0 && result.ErrorCount == 0;
            result.Message = $"{result.SuccessCount} succeeded, {result.ErrorCount} failed.";
        }

        private static BulkDocsDownloadResult FailDownload(BulkDocsDownloadResult result, string message)
        {
            result.Success = false;
            result.Message = message;
            return result;
        }

        private static string FormatBytes(long bytes)
        {
            return $"{bytes / 1024d / 1024d:0.##} MB";
        }

        private static string GetUniqueZipEntryName(string fileName, HashSet<string> entryNames)
        {
            if (entryNames.Add(fileName))
                return fileName;

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 2;

            while (true)
            {
                var entryName = $"{name}_{counter}{extension}";
                if (entryNames.Add(entryName))
                    return entryName;

                counter++;
            }
        }

        private static bool IsSafeBatchId(string batchId)
        {
            return !string.IsNullOrWhiteSpace(batchId)
                && batchId.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-');
        }
    }
}
