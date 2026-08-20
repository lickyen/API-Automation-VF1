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
        private const string BatchPrefix = "DMSDOCS";
        private const string CompletedStatus = "COMPLETED";
        private const string PartialStatus = "PARTIAL";
        private const string FailedStatus = "FAILED";
        private static readonly string[] AllowedNetworkRoots =
        {
            @"\\172.16.1.28\IT-FIN-Share",
            @"\\172.16.1.28\ITTeam"
        };

        private readonly DmsDocsRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly DmsDocsSettings _settings;
        private readonly string _filePath;
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
            _logger = logger;

            if (_settings.TimeoutSeconds > 0)
                _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<DmsDocsAddResult> AddLansAsync(DmsDocsAddRequest request)
        {
            var batchId = GenerateBatchId();
            var result = new DmsDocsAddResult
            {
                BatchId = batchId
            };

            if (request.Lans == null || !request.Lans.Any())
            {
                result.Errors.Add("No LANs provided.");
                return result;
            }

            foreach (var lan in request.Lans)
            {
                if (string.IsNullOrWhiteSpace(lan.LAN))
                {
                    result.Errors.Add("Empty LAN skipped.");
                    continue;
                }

                try
                {
                    var output = await _repository.AddLanAsync(lan, request.CreatedBy, batchId);
                    if (output == 1)
                        result.RowsAdded++;
                    else if (output == -1)
                        result.Errors.Add($"LAN {lan.LAN}: Already exists.");
                    else
                        result.Errors.Add($"LAN {lan.LAN}: Could not be added.");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"LAN {lan.LAN}: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<List<DmsDocsListItem>> GetLansAsync(DmsDocsGetLansRequest? request = null)
        {
            var items = await _repository.GetLansAsync(request);
            return items.ToList();
        }

        public async Task<DmsDocsFetchResult> FetchDocumentAsync(
            DmsDocsFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new DmsDocsFetchResult
            {
                LAN = request?.LAN?.Trim() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(result.LAN))
                return Fail(result, "LAN is required.");

            var doc = await GetLanAsync(result.LAN);
            if (doc == null)
                return Fail(result, $"LAN {result.LAN} was not found.");

            result.BatchId = doc.BatchId ?? string.Empty;

            var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy)
                ? "System"
                : request.UpdatedBy.Trim();

            try
            {
                ValidateFetchConfiguration();

                var token = await GetAuthenticationTokenAsync(cancellationToken);
                var documentBytes = await GetDocumentBytesAsync(doc, token, cancellationToken);
                var fileName = await SaveDocumentAsync(doc.LAN, documentBytes, cancellationToken);

                await UpdateDocStatusAsync(doc.ID, SuccessDocStatus, null, updatedBy);

                result.Success = true;
                result.DocStatus = SuccessDocStatus;
                result.FileName = fileName;
                result.Message = "Document fetched successfully.";
            }
            catch (Exception ex)
            {
                var errorMessage = BuildErrorMessage(ex.Message);
                await TryUpdateDocStatusAsync(doc.ID, ErrorDocStatus, errorMessage, updatedBy);

                result.Success = false;
                result.DocStatus = ErrorDocStatus;
                result.ErrorMessage = errorMessage;
                result.Message = errorMessage;

                _logger.LogError(ex, "DMS document fetch failed for LAN {Lan}", result.LAN);
            }

            return result;
        }

        public async Task<DmsDocsCopyResult> CopyDocumentsAsync(
            DmsDocsCopyRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new DmsDocsCopyResult
            {
                SourcePath = _filePath
            };

            if (request == null)
                return FailCopy(result, "Request is required.");

            var batchId = request.BatchId?.Trim() ?? string.Empty;
            result.BatchId = batchId;

            if (!IsSafeBatchId(batchId))
                return FailCopy(result, "Valid BatchId is required.");

            var batchDocs = await GetLansByBatchId(batchId);
            if (!batchDocs.Any())
                return FailCopy(result, $"No LANs found for BatchId {batchId}.");

            if (string.IsNullOrWhiteSpace(request.DestinationPath))
                return FailCopy(result, "Destination path is required.");

            var destinationBasePath = NormalizePath(request.DestinationPath);
            result.DestinationBasePath = destinationBasePath;

            if (!destinationBasePath.StartsWith(@"\\"))
                return FailCopy(result, "Destination path must be a UNC path.");

            if (!IsAllowedNetworkPath(destinationBasePath))
                return FailCopy(result, $"Destination path must be under one of: {string.Join(", ", AllowedNetworkRoots)}.");

            if (ContainsParentTraversal(destinationBasePath))
                return FailCopy(result, "Destination path cannot contain parent directory traversal.");

            if (string.IsNullOrWhiteSpace(_filePath))
                return FailCopy(result, "FilePaths:DmsDocsFilePath is required.");

            if (!Directory.Exists(_filePath))
                return FailCopy(result, $"Source path was not found: {_filePath}");

            var destinationFolderName = batchId;
            var destinationPath = Path.Combine(destinationBasePath, destinationFolderName);

            result.DestinationFolderName = destinationFolderName;
            result.DestinationPath = destinationPath;
            result.TotalFiles = batchDocs.Count;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Directory.CreateDirectory(destinationPath);

                foreach (var doc in batchDocs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sourceFile = Path.Combine(_filePath, $"{BuildSafeFileName(doc.LAN)}.pdf");
                    var fileName = Path.GetFileName(sourceFile);
                    var destinationFile = Path.Combine(destinationPath, fileName);
                    var item = new DmsDocsCopyItemResult
                    {
                        BatchId = batchId,
                        LAN = doc.LAN,
                        FileName = fileName,
                        SourceFilePath = sourceFile,
                        DestinationFilePath = destinationFile
                    };

                    result.Items.Add(item);

                    try
                    {
                        if (!File.Exists(sourceFile))
                            throw new FileNotFoundException($"Source file was not found: {fileName}", sourceFile);

                        File.Copy(sourceFile, destinationFile, overwrite: true);
                        result.CopiedFiles++;

                        File.Delete(sourceFile);
                        result.DeletedFiles++;

                        item.Success = true;
                        item.SourceDeleted = true;
                        item.Message = $"Copied to {destinationFile}.";
                    }
                    catch (Exception ex)
                    {
                        result.FailedFiles++;

                        item.Success = false;
                        item.Message = ex.Message;

                        _logger.LogError(ex, "Failed to copy/delete DMS document {FileName}", fileName);
                    }
                }

                result.Success = result.FailedFiles == 0;
                result.Status = GetBatchStatus(result.TotalFiles, result.CopiedFiles, result.FailedFiles);
                result.Message = result.Success
                    ? $"{result.CopiedFiles} file(s) copied to {destinationPath}."
                    : $"{result.CopiedFiles} file(s) copied, {result.FailedFiles} failed. Destination: {destinationPath}";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Status = FailedStatus;
                result.Message = ex.Message;

                _logger.LogError(ex, "DMS document copy failed");
            }

            return result;
        }

        private async Task<DmsDocsListItem?> GetLanAsync(string lan)
        {
            var docs = await _repository.GetLansAsync();

            return docs.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.LAN)
                && string.Equals(x.LAN.Trim(), lan, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<DmsDocsListItem>> GetLansByBatchId(string batchId)
        {
            var docs = await _repository.GetLansAsync(new DmsDocsGetLansRequest
            {
                BatchId = batchId
            });

            return docs.ToList();
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
            string lan,
            byte[] documentBytes,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_filePath);

            var fileName = $"{BuildSafeFileName(lan)}.pdf";
            var fullPath = Path.Combine(_filePath, fileName);

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

        private static DmsDocsCopyResult FailCopy(DmsDocsCopyResult result, string message)
        {
            result.Success = false;
            result.Status = FailedStatus;
            result.Message = message;
            return result;
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().TrimEnd('\\', '/');
        }

        private static bool IsAllowedNetworkPath(string networkPath)
        {
            return AllowedNetworkRoots.Any(root =>
            {
                var allowedRoot = NormalizePath(root);

                return string.Equals(networkPath, allowedRoot, StringComparison.OrdinalIgnoreCase)
                    || networkPath.StartsWith($"{allowedRoot}\\", StringComparison.OrdinalIgnoreCase);
            });
        }

        private static bool ContainsParentTraversal(string path)
        {
            return path
                .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(part => part == "..");
        }

        private static string GenerateBatchId()
        {
            return $"{BatchPrefix}_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private static bool IsSafeBatchId(string batchId)
        {
            return !string.IsNullOrWhiteSpace(batchId)
                && batchId.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-');
        }

        private static string GetBatchStatus(int totalFiles, int copiedFiles, int failedFiles)
        {
            if (totalFiles > 0 && copiedFiles == totalFiles && failedFiles == 0)
                return CompletedStatus;

            if (copiedFiles > 0)
                return PartialStatus;

            return FailedStatus;
        }
    }
}
