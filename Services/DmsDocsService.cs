using System.IO.Compression;
using Newtonsoft.Json.Linq;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class DmsDocsService
    {
        private const string SuccessDocStatus = "Success";
        private const string ErrorDocStatus = "Error";
        private const int MaxErrorMessageLength = 4000;
        private const long MaxDownloadZipBytes = 250L * 1024 * 1024;
        private const int MaxBatchIdLength = 50;
        private const int BatchTimestampLength = 15;
        private const string BatchIdFallbackPrefix = "USER";

        private readonly DmsDocsRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly DmsDocsSettings _settings;
        private readonly string _filePath;
        private readonly string _nachDocsPath;
        private readonly ILogger<DmsDocsService> _logger;

        public DmsDocsService(
            DmsDocsRepository repository,
            AppSettings appSettings,
            HttpClient httpClient,
            ILogger<DmsDocsService> logger)
        {
            _repository = repository;
            _httpClient = httpClient;
            _settings = appSettings.DmsDocsSettings;
            _filePath = appSettings.FilePaths.DmsDocsFilePath;
            _nachDocsPath = appSettings.FilePaths.NachDocsFilePath;
            _logger = logger;

            if (_settings.TimeoutSeconds > 0)
                _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<List<DmsDocsTagItem>> GetTagsAsync()
        {
            var items = await _repository.GetTagsAsync();
            return items.ToList();
        }

        public async Task<DmsDocsAddResult> AddLansAsync(DmsDocsAddRequest request)
        {
            var result = new DmsDocsAddResult();

            // Check if user already has an active batch
            var batches = await _repository.GetBatchesAsync(
                new DmsDocsGetBatchesRequest
                {
                    CreatedBy = request.CreatedBy
                });

            var hasActiveBatch = batches.Any(x => x.ActiveCount > 0);

            if (hasActiveBatch)
            {
                result.Errors.Add(
                    "An active batch already exists. Please wait until it is processed.");

                return result;
            }

            // Generate new batch only when no active batch exists
            var batchId = GenerateBatchId(request.CreatedBy);

            result.BatchId = batchId;

            foreach (var lan in request.Lans)
            {
                if (string.IsNullOrWhiteSpace(lan.LAN))
                {
                    result.Errors.Add("Empty LAN skipped.");
                    continue;
                }

                try
                {
                    var output = await _repository.AddLanAsync(
                        lan,
                        request.CreatedBy,
                        batchId);

                    if (output == 1)
                    {
                        result.RowsAdded++;
                    }
                    else
                    {
                        result.Errors.Add(
                            $"LAN {lan.LAN}: Could not be added.");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        $"LAN {lan.LAN}: {ex.Message}");
                }
            }

            return result;
        }

        //public async Task<DmsDocsAddResult> AddLansAsync(DmsDocsAddRequest request)
        //{
        //    var batchId = GenerateBatchId(request.CreatedBy);
        //    var result = new DmsDocsAddResult
        //    {
        //        BatchId = batchId
        //    };

        //    if (request.Lans == null || !request.Lans.Any())
        //    {
        //        result.Errors.Add("No LANs provided.");
        //        return result;
        //    }

        //    foreach (var lan in request.Lans)
        //    {
        //        if (string.IsNullOrWhiteSpace(lan.LAN))
        //        {
        //            result.Errors.Add("Empty LAN skipped.");
        //            continue;
        //        }

        //        try
        //        {
        //            var output = await _repository.AddLanAsync(lan, request.CreatedBy, batchId);
        //            if (output == 1)
        //                result.RowsAdded++;
        //            else if (output == -1)
        //                result.Errors.Add($"LAN {lan.LAN}: Already exists.");
        //            else
        //                result.Errors.Add($"LAN {lan.LAN}: Could not be added.");
        //        }
        //        catch (Exception ex)
        //        {
        //            result.Errors.Add($"LAN {lan.LAN}: {ex.Message}");
        //        }
        //    }

        //    return result;
        //}

        public async Task<List<DmsDocsBatchListItem>> GetBatchesAsync(DmsDocsGetBatchesRequest request)
        {
            var items = await _repository.GetBatchesAsync(request);
            var batches = items.ToList();

            foreach (var batch in batches)
            {
                batch.Status = batch.ActiveCount > 0 ? "Active" : "Processed";
            }

            return batches;
        }

        public async Task<List<DmsDocsListItem>> GetLansAsync(DmsDocsGetLansRequest? request = null)
        {
            var items = await _repository.GetLansAsync(request);
            return items.ToList();
        }

        public async Task<DmsDocsFetchBatchResult> FetchAsync(
            DmsDocsFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new DmsDocsFetchBatchResult
            {
                BatchId = request?.BatchId?.Trim() ?? string.Empty
            };

            if (!IsSafeBatchId(result.BatchId))
                return FailBatch(result, "Valid BatchId is required.");

            var lan = request?.LAN?.Trim() ?? string.Empty;
            var updatedBy = string.IsNullOrWhiteSpace(request?.UpdatedBy)
                ? "System"
                : request.UpdatedBy.Trim();

            var docs = string.IsNullOrWhiteSpace(lan)
                ? (await _repository.GetLansAsync(new DmsDocsGetLansRequest
                {
                    BatchId = result.BatchId,
                    Status = "Active"
                })).ToList()
                : (await _repository.GetLansAsync(new DmsDocsGetLansRequest
                {
                    BatchId = result.BatchId
                }))
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.LAN)
                    && string.Equals(x.LAN.Trim(), lan, StringComparison.OrdinalIgnoreCase))
                .ToList();

            result.TotalLans = docs.Count;

            if (!docs.Any())
            {
                var message = string.IsNullOrWhiteSpace(lan)
                    ? $"No active LANs found for BatchId {result.BatchId}."
                    : $"LAN {lan} was not found for BatchId {result.BatchId}.";

                return FailBatch(result, message);
            }

            string? token;

            try
            {
                ValidateFetchConfiguration();
                token = await GetAuthenticationTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var errorMessage = BuildErrorMessage(ex.Message) ?? string.Empty;

                foreach (var item in docs)
                {
                    await TryUpdateDocStatusAsync(item.ID, ErrorDocStatus, errorMessage, updatedBy);
                    result.Results.Add(BuildFailedFetchResult(item, errorMessage));
                }

                SetFetchBatchCounts(result);
                _logger.LogError(ex, "DMS document fetch setup failed for BatchId {BatchId}", result.BatchId);
                return result;
            }

            foreach (var item in docs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Results.Add(await FetchDocumentAsync(item, updatedBy, token, cancellationToken));
            }

            SetFetchBatchCounts(result);
            return result;
        }

        public async Task<DmsDocsDownloadResult> DownloadZipAsync(
            DmsDocsDownloadRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new DmsDocsDownloadResult
            {
                BatchId = request?.BatchId?.Trim() ?? string.Empty,
                MaxAllowedBytes = MaxDownloadZipBytes
            };

            if (!IsSafeBatchId(result.BatchId))
                return FailDownload(result, "Valid BatchId is required.");

            if (string.IsNullOrWhiteSpace(_filePath))
                return FailDownload(result, "FilePaths:DmsDocsFilePath is required.");

            if (!Directory.Exists(_filePath))
                return FailDownload(result, $"Source path was not found: {_filePath}");

            var batchDocs = await GetLansByBatchId(result.BatchId);
            if (!batchDocs.Any())
                return FailDownload(result, $"No LANs found for BatchId {result.BatchId}.");

            var batchFolderPath = await GetBatchFolderPathAsync(result.BatchId, batchDocs.First().CreatedBy, batchDocs.First().SubTag);
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
                var fileName = $"{BuildSafeFileName(doc.LAN)}.pdf";
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

        private async Task<List<DmsDocsListItem>> GetLansByBatchId(string batchId)
        {
            var docs = await _repository.GetLansAsync(new DmsDocsGetLansRequest
            {
                BatchId = batchId
            });

            return docs.ToList();
        }

        private async Task<DmsDocsFetchResult> FetchDocumentAsync(
            DmsDocsListItem doc,
            string updatedBy,
            string? token,
            CancellationToken cancellationToken)
        {
            var result = new DmsDocsFetchResult
            {
                BatchId = doc.BatchId ?? string.Empty,
                LAN = doc.LAN?.Trim() ?? string.Empty
            };

            try
            {
                ValidateFetchConfiguration();

                var authToken = string.IsNullOrWhiteSpace(token)
                    ? await GetAuthenticationTokenAsync(cancellationToken)
                    : token;

                var documentBytes = await GetDocumentBytesAsync(doc, authToken, cancellationToken);
                var fileName = await SaveDocumentAsync(doc, documentBytes, cancellationToken);

                await UpdateDocStatusAsync(doc.ID, SuccessDocStatus, null, updatedBy);

                result.Success = true;
                result.DocStatus = SuccessDocStatus;
                result.FileName = fileName;
                result.Message = "Document fetched successfully.";
            }
            catch (Exception ex)
            {
                var errorMessage = BuildErrorMessage(ex.Message) ?? string.Empty;
                await TryUpdateDocStatusAsync(doc.ID, ErrorDocStatus, errorMessage, updatedBy);

                result.Success = false;
                result.DocStatus = ErrorDocStatus;
                result.ErrorMessage = errorMessage;
                result.Message = errorMessage;

                _logger.LogError(ex, "DMS document fetch failed for BatchId {BatchId}, LAN {Lan}", result.BatchId, result.LAN);
            }

            return result;
        }

        private async Task<string> GetAuthenticationTokenAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _settings.BaseUrl);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.TryAddWithoutValidation("service", "ValidateUser");
            request.Headers.TryAddWithoutValidation("UserName", _settings.UserName);
            request.Headers.TryAddWithoutValidation("Password", _settings.Password);
            request.Headers.TryAddWithoutValidation("APIKey", _settings.ApiKey);

            var responseText = await SendDmsRequestAsync(request, cancellationToken);
            var token = GetJsonPropertyValue(responseText, "AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("DMS authentication response did not include AuthToken.");

            return token;
        }

        private async Task<byte[]> GetDocumentBytesAsync(
            DmsDocsListItem doc,
            string token,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _settings.BaseUrl);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.TryAddWithoutValidation("service", "DocumentDetails");
            request.Headers.TryAddWithoutValidation("APIKey", _settings.ApiKey);
            request.Headers.TryAddWithoutValidation("Token", token);
            request.Headers.TryAddWithoutValidation("RepoName", _settings.RepoName);
            request.Headers.TryAddWithoutValidation("RefID", doc.LAN);
            request.Headers.TryAddWithoutValidation("Category", doc.Tag ?? string.Empty);
            request.Headers.TryAddWithoutValidation("SubCategory", doc.SubTag ?? string.Empty);
            request.Headers.TryAddWithoutValidation("UpdateReferenceNo", string.Empty);
            request.Headers.TryAddWithoutValidation("ImageFormat", _settings.ImageFormat ?? string.Empty);

            var responseText = await SendDmsRequestAsync(request, cancellationToken);
            var base64 = GetJsonPropertyValue(responseText, "Imgb64");

            if (string.IsNullOrWhiteSpace(base64))
                throw new InvalidOperationException("DMS document response did not include Imgb64.");

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("DMS document response included invalid base64 content.", ex);
            }
        }

        private async Task<string> SendDmsRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"DMS API failed with status {(int)response.StatusCode} ({response.StatusCode}). Response: {responseText}");
            }

            return responseText;
        }

        private async Task<string> SaveDocumentAsync(
            DmsDocsListItem doc,
            byte[] documentBytes,
            CancellationToken cancellationToken)
        {
            var batchFolderPath = await GetBatchFolderPathAsync(doc.BatchId, doc.CreatedBy, doc.SubTag);
            Directory.CreateDirectory(batchFolderPath);

            var fileName = $"{BuildSafeFileName(doc.LAN)}.pdf";
            var fullPath = Path.Combine(batchFolderPath, fileName);

            await File.WriteAllBytesAsync(fullPath, documentBytes, cancellationToken);

            return fileName;
        }

        private async Task UpdateDocStatusAsync(int id, string docStatus, string? errorMessage, string updatedBy)
        {
            var output = await _repository.UpdateStatusAsync(new DmsDocsUpdateStatusRequest
            {
                ID = id,
                DocStatus = docStatus,
                ErrorMessage = BuildErrorMessage(errorMessage),
                UpdatedBy = updatedBy
            });

            if (output != 1)
                _logger.LogWarning("DMS doc status update returned {Output} for ID {Id}", output, id);
        }

        private async Task TryUpdateDocStatusAsync(int id, string docStatus, string? errorMessage, string updatedBy)
        {
            try
            {
                await UpdateDocStatusAsync(id, docStatus, errorMessage, updatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update DMS doc status for ID {Id}", id);
            }
        }

        private void ValidateFetchConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new InvalidOperationException("DmsDocsSettings:BaseUrl is required.");

            if (string.IsNullOrWhiteSpace(_settings.UserName))
                throw new InvalidOperationException("DmsDocsSettings:UserName is required.");

            if (string.IsNullOrWhiteSpace(_settings.Password))
                throw new InvalidOperationException("DmsDocsSettings:Password is required.");

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("DmsDocsSettings:ApiKey is required.");

            if (string.IsNullOrWhiteSpace(_settings.RepoName))
                throw new InvalidOperationException("DmsDocsSettings:RepoName is required.");

            if (string.IsNullOrWhiteSpace(_filePath))
                throw new InvalidOperationException("FilePaths:DmsDocsFilePath is required.");
        }

        private static string? GetJsonPropertyValue(string json, string propertyName)
        {
            var jObject = JObject.Parse(json);

            return jObject
                .Descendants()
                .OfType<JProperty>()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.ToString();
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

        private static string? BuildErrorMessage(string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return null;

            var trimmed = errorMessage.Trim();
            return trimmed.Length <= MaxErrorMessageLength
                ? trimmed
                : trimmed[..MaxErrorMessageLength];
        }

        private static DmsDocsFetchResult Fail(DmsDocsFetchResult result, string message)
        {
            result.Success = false;
            result.DocStatus = ErrorDocStatus;
            result.ErrorMessage = BuildErrorMessage(message) ?? string.Empty;
            result.Message = message;
            return result;
        }

        private static DmsDocsFetchBatchResult FailBatch(DmsDocsFetchBatchResult result, string message)
        {
            result.Success = false;
            result.Message = message;
            return result;
        }

        private static DmsDocsFetchResult BuildFailedFetchResult(DmsDocsListItem doc, string errorMessage)
        {
            return new DmsDocsFetchResult
            {
                BatchId = doc.BatchId ?? string.Empty,
                LAN = doc.LAN?.Trim() ?? string.Empty,
                Success = false,
                DocStatus = ErrorDocStatus,
                ErrorMessage = errorMessage,
                Message = errorMessage
            };
        }

        private static void SetFetchBatchCounts(DmsDocsFetchBatchResult result)
        {
            result.SuccessCount = result.Results.Count(x => x.Success);
            result.ErrorCount = result.Results.Count(x => !x.Success);
            result.Success = result.TotalLans > 0 && result.ErrorCount == 0;
            result.Message = $"{result.SuccessCount} succeeded, {result.ErrorCount} failed.";
        }

        private static DmsDocsDownloadResult FailDownload(DmsDocsDownloadResult result, string message)
        {
            result.Success = false;
            result.Message = message;
            return result;
        }

        //private async Task<string> GetBatchFolderPathAsync(string batchId, string? createdBy)
        //{
        //    // Try to get a user-specific base path from repository. If not available, use the default _filePath.
        //    string? userBasePath = null;

        //    try
        //    {
        //        userBasePath = await _repository.GetUserPathAsync(createdBy);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log and fallback to default path
        //        _logger.LogWarning(ex, "Failed to retrieve user-specific path for {CreatedBy}. Falling back to default path.", createdBy);
        //    }

        //    var basePath = string.IsNullOrWhiteSpace(userBasePath) ? _filePath : userBasePath!;

        //    return Path.Combine(
        //        basePath,
        //        BuildSafePathSegment(createdBy, "UNKNOWN"),
        //        BuildSafeFileName(batchId));
        //}

        private async Task<string> GetBatchFolderPathAsync(
    string batchId,
    string? createdBy,
    string? subTag)
        {
            string? userBasePath = null;

            try
            {
                userBasePath = await _repository.GetUserPathAsync(createdBy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to retrieve user-specific path for {CreatedBy}. Falling back to default path.",
                    createdBy);
            }

            var isNachDocument =
                string.Equals(subTag, "NACH", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(subTag, "Security P Nach", StringComparison.OrdinalIgnoreCase);

            var basePath = isNachDocument
                ? _nachDocsPath
                : (string.IsNullOrWhiteSpace(userBasePath) ? _filePath : userBasePath);

            return Path.Combine(
                basePath,
                BuildSafePathSegment(createdBy, "UNKNOWN"),
                BuildSafeFileName(batchId));
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
    }
}
