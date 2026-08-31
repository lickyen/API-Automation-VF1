using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Constants;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class ClosureService
    {
        private const string REQ_SOURCE = "FC";

        private readonly ClosureRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private readonly string _filePath;

        public ClosureService(ClosureRepository repository, EmailService emailService, TemplateService templateService, UserService userService, AppSettings appSettings)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
            _filePath = appSettings.FilePaths.ClosureFilePath;
        }

        public static bool IsCritical(string? criticality)
            => criticality == "Critical" || criticality == "High Critical";

        public static bool IsBucket(string? bucket)
            => bucket == "B1-B3" || bucket == "NPA";

        public async Task<int> AddRequestAsync(AddClosureRequestDto dto)
        {
            string status = string.Empty;

            var isCritical = IsCritical(dto.Criticality);
            var isBucket = IsBucket(dto.Bucket);
            var isRetention = !isCritical && !isBucket;

            int? retentionId = null;
            var hasActiveApprovedRetention = false;

            if (isRetention)
            {
                var activeRetentionId = await _repository.GetActiveApprovedRetentionIdAsync(dto.Lan);
                if (activeRetentionId.HasValue)
                {
                    retentionId = activeRetentionId.Value;
                    hasActiveApprovedRetention = true;
                    status = ClosureStatus.Initiated;
                }
                else
                {
                    retentionId = await _repository.CreateRetentionAsync(dto.Lan, REQ_SOURCE, null, dto.CreatedBy);
                    status = ClosureStatus.New;
                }
            }

            else
            {
                status = dto.IsWaiver
                    ? ClosureStatus.Initiated 
                    : ClosureStatus.AutoApproved;
            }

            var savedFiles = new List<string>();
            var shouldNotifyPendingUser = false;

            var id = await ExecuteFileBackedTransactionAsync(savedFiles, async (connection, transaction) =>
            {
                var requestId = await _repository.AddRequestAsync(dto, status, isRetention, retentionId, connection, transaction);
                if (requestId <= 0)
                    return requestId;

                await SaveClosureRequestFilesAsync(requestId, dto.AckFile, dto.ChecklistFile, dto.FCLetterFile, connection, transaction, savedFiles);

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = requestId,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    TotalOutstanding = dto.TotalOutstanding,
                    POS = dto.POS,
                    Interest = dto.Interest,
                    Charges = dto.Charges,
                    AmountProposed = dto.AmountProposed,
                    POSProposed = dto.POSProposed,
                    InterestProposed = dto.InterestProposed,
                    ChargesProposed = dto.ChargesProposed,
                    POSWaiverPer = dto.POSWaiverPer,
                    InterestWaiverPer = dto.InterestWaiverPer,
                    ChargesWaiverPer = dto.ChargesWaiverPer,
                    CreatedBy = dto.CreatedBy
                }, connection, transaction);

                if (isRetention && retentionId.HasValue && hasActiveApprovedRetention)
                {
                    var routeResult = await _repository.ApplyExistingRetentionApprovalAsync(requestId, retentionId.Value, dto.CreatedBy, connection, transaction);
                    shouldNotifyPendingUser = routeResult > 0;
                }

                return requestId;
            });

            if (id > 0)
            {
                if (isRetention)
                {
                    if (shouldNotifyPendingUser)
                        await NotifyPendingUserAsync(id);
                }
                else if (status == ClosureStatus.AutoApproved)
                    await NotifyBranchAsync(id, "Approved");
                else if (status == ClosureStatus.Initiated)
                    await NotifyPendingUserAsync(id);
            }

            return id;
        }

        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto)
        {
            string status = string.Empty;
            var isCritical = IsCritical(dto.Criticality);
            var isBucket = IsBucket(dto.Bucket);
            var isRetention = !isCritical && !isBucket;

            int? retentionId = null;
            var hasActiveApprovedRetention = false;

            if (isRetention)
            {
                var existing = await _repository.GetByIdAsync(dto.Id);
                if (existing == null)
                    return -1;

                var activeRetentionId = await _repository.GetActiveApprovedRetentionIdAsync(existing.Lan);
                if (activeRetentionId.HasValue)
                {
                    retentionId = activeRetentionId.Value;
                    hasActiveApprovedRetention = true;
                }
                else
                {
                    retentionId = await _repository.CreateRetentionAsync(existing.Lan, REQ_SOURCE, dto.Id, dto.UpdatedBy);
                }

                status = ClosureStatus.ReInitiated;
            }
            else
            {
                status = dto.IsWaiver
                    ? ClosureStatus.ReInitiated
                    : ClosureStatus.AutoApproved;
            }

            var savedFiles = new List<string>();
            var shouldNotifyPendingUser = false;

            var result = await ExecuteFileBackedTransactionAsync(savedFiles, async (connection, transaction) =>
            {
                var updateResult = await _repository.ReinitiateRequestAsync(dto, status, isRetention, retentionId, connection, transaction);
                if (updateResult <= 0)
                    return updateResult;

                await SaveClosureRequestFilesAsync(dto.Id, dto.AckFile, dto.ChecklistFile, dto.FCLetterFile, connection, transaction, savedFiles);

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    TotalOutstanding = dto.TotalOutstanding,
                    POS = dto.POS,
                    Interest = dto.Interest,
                    Charges = dto.Charges,
                    AmountProposed = dto.AmountProposed,
                    POSProposed = dto.POSProposed,
                    InterestProposed = dto.InterestProposed,
                    ChargesProposed = dto.ChargesProposed,
                    POSWaiverPer = dto.POSWaiverPer,
                    InterestWaiverPer = dto.InterestWaiverPer,
                    ChargesWaiverPer = dto.ChargesWaiverPer,
                    CreatedBy = dto.UpdatedBy
                }, connection, transaction);

                if (isRetention && retentionId.HasValue)
                {
                    if (hasActiveApprovedRetention)
                    {
                        var routeResult = await _repository.ApplyExistingRetentionApprovalAsync(dto.Id, retentionId.Value, dto.UpdatedBy, connection, transaction);
                        shouldNotifyPendingUser = routeResult > 0;
                    }
                    else
                    {
                        shouldNotifyPendingUser = true;
                    }
                }

                return updateResult;
            });

            if (result > 0)
            {
                if (isRetention)
                {
                    if (shouldNotifyPendingUser)
                        await NotifyPendingUserAsync(dto.Id);
                }
                else if (status == ClosureStatus.AutoApproved)
                    await NotifyBranchAsync(dto.Id, "Approved");
                else
                    await NotifyPendingUserAsync(dto.Id);
            }

            return result;
        }

        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto)
        {
            string status = ClosureStatus.AmountCollected;

            var savedFiles = new List<string>();

            var result = await ExecuteFileBackedTransactionAsync(savedFiles, async (connection, transaction) =>
            {
                var updateResult = await _repository.UpdateAmountCollectedAsync(dto, status, connection, transaction);
                if (updateResult <= 0)
                    return updateResult;

                await SaveClosureRequestFilesAsync(dto.Id, dto.AckFile, dto.ChecklistFile, dto.FCLetterFile, connection, transaction, savedFiles);

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                }, connection, transaction);

                return updateResult;
            });

            if (result > 0)
                await NotifyPendingUserAsync(dto.Id);

            return result;
        }

        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto)
        {
            string status = ClosureStatus.ReSubmitted;

            var savedFiles = new List<string>();

            var result = await ExecuteFileBackedTransactionAsync(savedFiles, async (connection, transaction) =>
            {
                var updateResult = await _repository.ResubmitRequestAsync(dto, status, connection, transaction);
                if (updateResult <= 0)
                    return updateResult;

                await SaveClosureRequestFilesAsync(dto.Id, dto.AckFile, dto.ChecklistFile, dto.FCLetterFile, connection, transaction, savedFiles);

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                }, connection, transaction);

                return updateResult;
            });

            if (result > 0)
                await NotifyPendingUserAsync(dto.Id);

            return result;
        }

        public async Task<int> ApproveAsync(ClosureApprovalDto dto)
        {
            var result = await _repository.ApproveAsync(dto);
            if (result > 0)
            {
                await NotifyBranchAsync(dto.Id, dto.Status);

                await NotifyPendingUserAsync(dto.Id);

            }
            return result;
        }

        public async Task<int> RetentionApprovalAsync(ClosureRetentionApprovalDto dto)
        {
            var result = await _repository.RetentionApprovalAsync(dto);
            if (result > 0)
            {
                await NotifyBranchAsync(dto.Id, dto.Status);
                await NotifyPendingUserAsync(dto.Id);
            }

            return result;
        }

        public async Task<int> CopsApprovalAsync(ClosureCopsApprovalDto dto)
        {
            var result = await _repository.CopsApprovalAsync(dto);
            if (result > 0)
            {
                await NotifyBranchAsync(dto.Id, dto.Status);
            }
            return result;
        }

        public async Task<SubmitQuestionnaireResultDto> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto)
        {
            string status = ClosureStatus.Initiated;

            // First, save any questionnaire responses provided in the request
            if (dto.Responses != null && dto.Responses.Any())
            {
                await SaveQuestionnaireUploadResponsesAsync(dto);

                await _repository.AddQuestionnaireResponseAsync(
                    dto.Id,
                    dto.Source,
                    dto.RetentionId,
                    dto.Responses,
                    dto.CreatedBy
                );
            }

            var result = await _repository.SubmitQuestionnaireAsync(dto, status);

            if (result > 0 && dto.Source == REQ_SOURCE && dto.Id.HasValue)
            {
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id.Value,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    CreatedBy = dto.CreatedBy
                });

                await NotifyPendingUserAsync(dto.Id.Value);
            }

            return new SubmitQuestionnaireResultDto
            {
                Result = result,
                Responses = dto.Responses
            };
        }

        private async Task NotifyPendingUserAsync(int reqId)
        {
            var transaction = await _repository.GetByIdAsync(reqId);
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.PendingUser))
                return;

            var user = _userService.GetUser(transaction.PendingUser);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var placeholders = new Dictionary<string, string>
            {
                { "Module", "Foreclosure" },
                { "LAN", transaction.Lan }
            };

            var template = _templateService.GetEmailTemplate("MovedToQEmail");

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            await _emailService.SendEmail(
                "Action Required - Foreclosure",
                body,
                new List<string> { user.Email },
                new List<string>()
            );
        }

        private async Task NotifyBranchAsync(int reqId, string action)
        {
            var transaction = await _repository.GetByIdAsync(reqId);
            if (transaction == null)
                return;

            var placeholders = new Dictionary<string, string>
            {
                { "Module", "Foreclosure" },
                { "LAN", transaction.Lan }
            };

            if (action == "Approved")
            {
                var isApproved = transaction.IsApproved;

                if (isApproved)
                {
                    // ✅ FINAL → BRANCH
                    var user = _userService.GetUser(transaction.CreatedBy);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var template = _templateService.GetEmailTemplate("ApprovedEmail");
                        var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                        await _emailService.SendEmail(
                            "Foreclosure - Approved",
                            body,
                            new List<string> { user.Email },
                            new List<string>()
                        );
                    }
                }
            }

            else if (action == "Rejected")
            {
                var user = _userService.GetUser(transaction.CreatedBy);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var template = _templateService.GetEmailTemplate("RejectedEmail");
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    await _emailService.SendEmail(
                        "Foreclosure - Rejected",
                        body,
                        new List<string> { user.Email },
                        new List<string>()
                    );
                }
            }

            else if (action == "Closed")
            {
                var user = _userService.GetUser(transaction.CreatedBy);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var template = _templateService.GetEmailTemplate("ProcessedEmail");
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    await _emailService.SendEmail(
                        "Foreclosure - Processed",
                        body,
                        new List<string> { user.Email },
                        new List<string>()
                    );
                }
            }
        }

        public async Task<ClosureTransaction?> GetByIdAsync(int id)
        {
            var txn = await _repository.GetByIdAsync(id);
            if (txn == null)
                return null;

            txn.Payments = await _repository.GetPaymentsAsync(id);
            txn.Logs = await _repository.GetLogsAsync(id);
            txn.Questionnaire = await _repository.GetQuestionnaireAsync(id, "FC", txn.RetentionId);
            if (txn.RetentionId.HasValue)
            {
                txn.CAM = await _repository.GetCAMAsync(txn.RetentionId.Value);
            }

            return txn;
        }

        public Task<List<ClosureTransaction>> GetListByUserAsync(string userId, string role, string? status)
        {
            return _repository.GetByUserAsync(userId, role, status);
        }

        public async Task<List<ClosureTransaction>> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);

        public async Task<int> AddPaymentAsync(AddClosurePaymentDto dto)
        {
            string? proofPath = null;
            var savedFiles = new List<string>();

            try
            {
                if (dto.ProofFile != null)
                {
                    if (dto.ReqId <= 0)
                        throw new ArgumentException("ReqId is required for payment proof upload");

                    proofPath = await UploadFileAsync(dto.ReqId, dto.ProofFile, GetPaymentFileType(dto.PaymentMode));
                    savedFiles.Add(proofPath);
                }

                var result = await _repository.AddPaymentAsync(dto, proofPath);
                if (result <= 0)
                    DeleteSavedFiles(savedFiles);

                return result;
            }
            catch
            {
                DeleteSavedFiles(savedFiles);
                throw;
            }
        }

        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto)
        {
            string? proofPath = null;
            var savedFiles = new List<string>();

            try
            {
                if (dto.ProofFile != null)
                {
                    if (dto.ReqId <= 0)
                        throw new ArgumentException("ReqId is required for payment proof upload");

                    proofPath = await UploadFileAsync(dto.ReqId, dto.ProofFile, GetPaymentFileType(dto.PaymentMode));
                    savedFiles.Add(proofPath);
                }

                var result = await _repository.UpdatePaymentAsync(dto, proofPath);
                if (result <= 0)
                    DeleteSavedFiles(savedFiles);

                return result;
            }
            catch
            {
                DeleteSavedFiles(savedFiles);
                throw;
            }
        }

        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
            => await _repository.DeletePaymentAsync(id, deletedBy);

        public async Task<string?> GetWaiverApproverAsync(GetWaiverApproverDto dto)
            => await _repository.GetWaiverApproverAsync(dto);

        // ── File Upload / Download ──────────────────────────────────────────

        private async Task<T> ExecuteFileBackedTransactionAsync<T>(
            List<string> savedFiles,
            Func<SqlConnection, SqlTransaction, Task<T>> action)
        {
            try
            {
                return await _repository.ExecuteInTransactionAsync(action);
            }
            catch
            {
                DeleteSavedFiles(savedFiles);
                throw;
            }
        }

        private async Task SaveClosureRequestFilesAsync(
            int reqId,
            FileUploadBase64? ackFile,
            FileUploadBase64? checklistFile,
            FileUploadBase64? fcLetterFile,
            SqlConnection connection,
            SqlTransaction transaction,
            List<string> savedFiles)
        {
            if (ackFile != null)
            {
                var ackPath = await UploadFileAsync(reqId, ackFile, "Ack");
                savedFiles.Add(ackPath);
                await _repository.UpdateFilePathAsync(reqId, "ACK", ackPath, connection, transaction);
            }

            if (checklistFile != null)
            {
                var checklistPath = await UploadFileAsync(reqId, checklistFile, "Checklist");
                savedFiles.Add(checklistPath);
                await _repository.UpdateFilePathAsync(reqId, "CHECKLIST", checklistPath, connection, transaction);
            }

            if (fcLetterFile != null)
            {
                var fcLetterPath = await UploadFileAsync(reqId, fcLetterFile, "FCLetter");
                savedFiles.Add(fcLetterPath);
                await _repository.UpdateFilePathAsync(reqId, "FCLETTER", fcLetterPath, connection, transaction);
            }
        }

        private void DeleteSavedFiles(IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Path.GetFileName(fileName) != fileName)
                    continue;

                try
                {
                    var fullPath = Path.Combine(_filePath, fileName);
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                catch
                {
                    // Keep the original upload/DB exception as the response cause.
                }
            }
        }

        private async Task<string> UploadFileAsync(int reqId, FileUploadBase64 fileDto, string fileType)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".zip" };

            var extension = Path.GetExtension(fileDto.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = GetExtensionFromDataUri(fileDto.Base64);

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("FileName with valid extension or data URI content type is required");

            extension = extension.ToLowerInvariant();
            var fileName = $"{reqId}_{fileType}{extension}";

            var result = await FileHelper.SaveBase64FileAsync(
                _filePath,
                fileName,
                fileDto.Base64,
                allowedExtensions
            );

            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            return fileName;
        }

        private static string GetPaymentFileType(string? paymentMode)
        {
            var mode = string.IsNullOrWhiteSpace(paymentMode)
                ? "PaymentProof"
                : SanitizeFileNamePart(paymentMode);

            return $"{mode}_{DateTime.Now:yyyyMMddHHmmss}";
        }

        private async Task SaveQuestionnaireUploadResponsesAsync(SubmitQuestionnaireDto dto)
        {
            if (dto.Responses == null || !dto.Responses.Any())
                return;

            var questionnaireMaster = await _repository.GetQuestionnaireMasterAsync();
            var uploadQuestionIds = questionnaireMaster
                .Where(q => string.Equals(q.InputType, "Upload", StringComparison.OrdinalIgnoreCase))
                .Select(q => q.Id)
                .ToHashSet();

            var uploadQuestionCodes = questionnaireMaster
                .Where(q => string.Equals(q.InputType, "Upload", StringComparison.OrdinalIgnoreCase))
                .Select(q => q.QuestionCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var savedFiles = new List<string>();

            try
            {
                foreach (var response in dto.Responses)
                {
                    var isUploadQuestion = uploadQuestionIds.Contains(response.QuestionId)
                        || uploadQuestionCodes.Contains(response.QuestionCode);

                    if (!isUploadQuestion || string.IsNullOrWhiteSpace(response.ResponseValue))
                        continue;

                    if (IsSavedFileName(response.ResponseValue))
                    {
                        response.FileName = response.ResponseValue;
                        continue;
                    }

                    if (!dto.RetentionId.HasValue || dto.RetentionId.Value <= 0)
                        throw new ArgumentException("RetentionId is required for questionnaire file upload");

                    var extension = Path.GetExtension(response.FileName);
                    if (string.IsNullOrWhiteSpace(extension))
                        extension = GetExtensionFromDataUri(response.ResponseValue);

                    if (string.IsNullOrWhiteSpace(extension))
                        throw new ArgumentException($"FileName or data URI content type is required for upload question {response.QuestionCode}");

                    var questionCode = SanitizeFileNamePart(response.QuestionCode);

                    var uploadFile = new FileUploadBase64
                    {
                        FileName = $"{questionCode}{extension}",
                        Base64 = response.ResponseValue
                    };

                    var savedFileName = await UploadFileAsync(dto.RetentionId.Value, uploadFile, questionCode);
                    savedFiles.Add(savedFileName);

                    response.ResponseValue = savedFileName;
                    response.FileName = savedFileName;
                }
            }
            catch
            {
                DeleteSavedFiles(savedFiles);
                throw;
            }
        }

        private static bool IsSavedFileName(string value)
        {
            if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return false;

            var extension = Path.GetExtension(value)?.ToLowerInvariant();
            return extension is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".zip";
        }

        private static string? GetExtensionFromDataUri(string base64)
        {
            if (!base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return null;

            var commaIndex = base64.IndexOf(',');
            if (commaIndex <= 5)
                return null;

            var header = base64[..commaIndex].ToLowerInvariant();

            if (header.Contains("application/pdf"))
                return ".pdf";
            if (header.Contains("image/jpeg") || header.Contains("image/jpg"))
                return ".jpg";
            if (header.Contains("image/png"))
                return ".png";
            if (header.Contains("application/zip"))
                return ".zip";

            return null;
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "File";

            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray();

            var sanitized = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "File" : sanitized;
        }

        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                return null;

            return await FileHelper.ReadFileAsync(_filePath, fileName);
        }
        public async Task<List<ClosureQuestionnaireMaster>> GetQuestionnaireMasterAsync()
        {
            return await _repository.GetQuestionnaireMasterAsync();
        }

        public async Task<List<ClosureQuestionnaireResponse>> GetQuestionnaireByRetentionIdAsync(int retentionId)
        {
            return await _repository.GetQuestionnaireAsync(null, null, retentionId);
        }

        public async Task<List<ClosureCamResponse>> GetCAMAsync(int retentionId)
        {
            return await _repository.GetCAMAsync(retentionId);
        }

        public async Task<List<ClosureCamMaster>> GetCAMMasterAsync()
        {
            return await _repository.GetCAMMasterAsync();
        }

        public async Task<int> SubmitCAMAsync(SubmitClosureCamDto dto)
        {
            return await _repository.SubmitCAMAsync(dto);
        }

        public async Task<int> CancelRequestAsync(CancelClosureRequestDto dto)
        {
            var result = await _repository.CancelRequestAsync(dto);

            if (result > 0)
            {
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = ClosureStatus.Cancelled,
                    Status = ClosureStatus.Cancelled,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.CancelledBy
                });
            }

            return result;
        }

        public static ClosureShortfallResultDto CalculateShortfall(CalculateShortfallDto dto)
        {
            // Shortfall = Live – Approved (only if Live > 0, matching the VB guard)
            decimal diffAmount = dto.InterestLive > 0m
                ? dto.InterestLive - dto.InterestApproved
                : 0m;

            // Updated Total (before waiver)
            decimal updatedTotal = dto.TotalProposedAmount + diffAmount;

            // Shortfall after waiver
            decimal diffAmountIncWaiver = diffAmount - (diffAmount * (dto.InterestWaiverPer / 100m));

            // Updated total after waiver
            decimal updatedTotalIncWaiver = dto.TotalProposedAmount + diffAmountIncWaiver;

            return new ClosureShortfallResultDto
            {
                DiffAmount = Math.Round(diffAmount, 2),
                UpdatedTotal = Math.Round(updatedTotal, 2),
                DiffAmountIncWaiver = Math.Round(diffAmountIncWaiver, 2),
                UpdatedTotalIncWaiver = Math.Round(updatedTotalIncWaiver, 2)
            };
        }

        public Task<PagedResult<ClosureTransaction>> GetAllAsync(PaginationRequestDto request)
            => _repository.GetAllAsync(request);
    }
}
