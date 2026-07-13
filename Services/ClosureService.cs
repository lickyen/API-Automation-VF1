using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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

            if (IsCritical(dto.Criticality) || IsBucket(dto.Bucket))
            {
                if (dto.IsWaiver)
                {
                    status = ClosureStatus.Initiated;
                }
                else
                {
                    status = ClosureStatus.AutoApproved;
                }
            }
            else
            {
                status = ClosureStatus.New;
            }

            var id = await _repository.AddRequestAsync(dto, status);
            if (id > 0)
            {

                if (dto.AckFile != null)
                {
                    var ackPath = await UploadFileAsync(id, dto.AckFile, "Ack");
                    await _repository.UpdateFilePathAsync(id, "ACK", ackPath);
                }

                if (dto.ChecklistFile != null)
                {
                    var checklistPath = await UploadFileAsync(id, dto.ChecklistFile, "Checklist");
                    await _repository.UpdateFilePathAsync(id, "CHECKLIST", checklistPath);
                }

                if (dto.FCLetterFile != null)
                {
                    var fcLetterPath = await UploadFileAsync(id, dto.FCLetterFile, "FCLetter");
                    await _repository.UpdateFilePathAsync(id, "FCLETTER", fcLetterPath);
                }

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = id,
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
                });

                if (status == ClosureStatus.AutoApproved)
                {
                    await NotifyBranchAsync(id, "Approved");
                }

                else if (status == ClosureStatus.Initiated)
                {
                    await NotifyPendingUserAsync(id);
                }
            }

            return id;
        }

        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto)
        {
            string status = string.Empty;

            if ((IsCritical(dto.Criticality) || IsBucket(dto.Bucket)) && !dto.IsWaiver)
            {
                status = ClosureStatus.AutoApproved;
            }
            else
            {
                status = ClosureStatus.ReInitiated;
            }

            var result = await _repository.ReinitiateRequestAsync(dto, status);
            if (result > 0)
            {

                if (dto.AckFile != null)
                {
                    var ackPath = await UploadFileAsync(dto.Id, dto.AckFile, "Ack");
                    await _repository.UpdateFilePathAsync(dto.Id, "ACK", ackPath);
                }

                if (dto.ChecklistFile != null)
                {
                    var checklistPath = await UploadFileAsync(dto.Id, dto.ChecklistFile, "Checklist");
                    await _repository.UpdateFilePathAsync(dto.Id, "CHECKLIST", checklistPath);
                }

                if (dto.FCLetterFile != null)
                {
                    var fcLetterPath = await UploadFileAsync(dto.Id, dto.FCLetterFile, "FCLetter");
                    await _repository.UpdateFilePathAsync(dto.Id, "FCLETTER", fcLetterPath);
                }


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
                });

                if (status == ClosureStatus.AutoApproved)
                {
                    await NotifyBranchAsync(dto.Id, "Approved");
                }

                await NotifyPendingUserAsync(dto.Id);

            }

            return result;
        }

        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto)
        {
            string status = ClosureStatus.AmountCollected;

            var result = await _repository.UpdateAmountCollectedAsync(dto, status);
            if (result > 0)
            {

                if (dto.AckFile != null)
                {
                    var ackPath = await UploadFileAsync(dto.Id, dto.AckFile, "Ack");
                    await _repository.UpdateFilePathAsync(dto.Id, "ACK", ackPath);
                }

                if (dto.ChecklistFile != null)
                {
                    var checklistPath = await UploadFileAsync(dto.Id, dto.ChecklistFile, "Checklist");
                    await _repository.UpdateFilePathAsync(dto.Id, "CHECKLIST", checklistPath);
                }

                if (dto.FCLetterFile != null)
                {
                    var fcLetterPath = await UploadFileAsync(dto.Id, dto.FCLetterFile, "FCLetter");
                    await _repository.UpdateFilePathAsync(dto.Id, "FCLETTER", fcLetterPath);
                }

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });

                await NotifyPendingUserAsync(dto.Id);
            }

            return result;
        }

        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto)
        {
            string status = ClosureStatus.ReSubmitted;

            var result = await _repository.ResubmitRequestAsync(dto, status);
            if (result > 0)
            {

                if (dto.AckFile != null)
                {
                    var ackPath = await UploadFileAsync(dto.Id, dto.AckFile, "Ack");
                    await _repository.UpdateFilePathAsync(dto.Id, "ACK", ackPath);
                }

                if (dto.ChecklistFile != null)
                {
                    var checklistPath = await UploadFileAsync(dto.Id, dto.ChecklistFile, "Checklist");
                    await _repository.UpdateFilePathAsync(dto.Id, "CHECKLIST", checklistPath);
                }

                if (dto.FCLetterFile != null)
                {
                    var fcLetterPath = await UploadFileAsync(dto.Id, dto.FCLetterFile, "FCLetter");
                    await _repository.UpdateFilePathAsync(dto.Id, "FCLETTER", fcLetterPath);
                }

                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });

                await NotifyPendingUserAsync(dto.Id);
            }

            return result;
        }

        public async Task<int> ApproveAsync(ClosureApprovalDto dto)
        {
            var result = await _repository.ApproveAsync(dto);
            if (result > 0)
            {
                //logs added from sp

                //await _repository.AddLogAsync(new AddClosureLogDto
                //{
                //    ReqId = dto.Id,
                //    Role = dto.Role,
                //    Action = dto.Status,
                //    Status = $"{dto.Role}-{dto.Status}",
                //    Remarks = dto.Remarks,
                //    TotalOutstanding = dto.TotalOutstanding,
                //    POS = dto.POS,
                //    Interest = dto.Interest,
                //    Charges = dto.Charges,
                //    AmountProposed = dto.AmountProposed,
                //    POSProposed = dto.POSProposed,
                //    InterestProposed = dto.InterestProposed,
                //    ChargesProposed = dto.ChargesProposed,
                //    POSWaiverPer = dto.POSWaiverPer,
                //    InterestWaiverPer = dto.InterestWaiverPer,
                //    ChargesWaiverPer = dto.ChargesWaiverPer,
                //    CreatedBy = dto.UserId
                //});

                await NotifyBranchAsync(dto.Id, dto.Status);

                await NotifyPendingUserAsync(dto.Id);

            }
            return result;
        }

        public async Task<int> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto)
        {
            string status = ClosureStatus.Initiated;

            // First, save any questionnaire responses provided in the request
            if (dto.Responses != null && dto.Responses.Any())
            {
                await _repository.AddQuestionnaireResponseAsync(
                    dto.Id,
                    dto.Source,
                    dto.Responses,
                    dto.CreatedBy
                );
            }

            var result = await _repository.SubmitQuestionnaireAsync(dto, status);

            if (result > 0 && dto.Source == REQ_SOURCE)
            {
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    CreatedBy = dto.CreatedBy
                });

                await NotifyPendingUserAsync(dto.Id);
            }

            return result;
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
            txn.Questionnaire = await _repository.GetQuestionnaireAsync(id, "FC");

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
            if (dto.ProofFile != null)
                proofPath = await UploadFileAsync(dto.ReqId, dto.ProofFile, $"Payment_{DateTime.Now:yyyyMMddHHmmss}");

            return await _repository.AddPaymentAsync(dto, proofPath);
        }

        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto)
        {
            string? proofPath = null;
            if (dto.ProofFile != null)
                proofPath = await UploadFileAsync(dto.Id, dto.ProofFile, "Payment");

            return await _repository.UpdatePaymentAsync(dto, proofPath);
        }

        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
            => await _repository.DeletePaymentAsync(id, deletedBy);

        public async Task<string?> GetWaiverApproverAsync(GetWaiverApproverDto dto)
            => await _repository.GetWaiverApproverAsync(dto);

        // ── File Upload / Download ──────────────────────────────────────────

        private async Task<string> UploadFileAsync(int reqId, FileUploadBase64 fileDto, string fileType)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".zip" };

            var extension = Path.GetExtension(fileDto.FileName);
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

        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            return await FileHelper.ReadFileAsync(_filePath, fileName);
        }
        public async Task<List<ClosureQuestionnaireMaster>> GetQuestionnaireMasterAsync()
        {
            return await _repository.GetQuestionnaireMasterAsync();
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
    }
}