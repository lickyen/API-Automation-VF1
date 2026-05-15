using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ProductivityApi.Constants;
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

        public ClosureService(ClosureRepository repository, EmailService emailService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public static bool IsCritical(string? action)
            => action == "Critical" || action == "High Critical";

        public static bool IsBucket(string? bucket)
            => bucket == "B1-B3" || bucket == "NPA";

        public async Task<int> AddRequestAsync(AddClosureRequestDto dto)
        {
            string status = string.Empty;

            if (IsCritical(dto.Action) || IsBucket(dto.Bucket))
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

            if ((IsCritical(dto.Action) || IsBucket(dto.Bucket)) && !dto.IsWaiver)
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
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = dto.Role,
                    Action = dto.Status,
                    Status = $"{dto.Role}-{dto.Status}",
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
                    CreatedBy = dto.UserId
                });

                await NotifyBranchAsync(dto.Id, dto.Status);

                await NotifyPendingUserAsync(dto.Id);

            }
            return result;
        }

        public async Task<int> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto)
        {
            string status = ClosureStatus.Initiated;

            var result = await _repository.SubmitQuestionnaireAsync(dto, status);
            if (result == 0 && dto.Source == REQ_SOURCE)
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

            var user = _userService.GetUser(transaction.PendingUser);

            if (user == null || string.IsNullOrEmpty(user.Email))
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

        public async Task<List<ClosureTransaction>> GetListByUserAsync(string userId, string role, string status)
        {
            var s = (status ?? string.Empty).Trim().ToUpper();

            if (s == "P")
            {
                return await _repository.GetPendingByUserAsync(userId, role);
            }
            else if (s == "C")
            {
                return await _repository.GetCompletedByUserAsync(userId, role);
            }

            return await _repository.GetPendingByUserAsync(userId, role);
        }

        public async Task<List<ClosureTransaction>> SearchByLanAsync(string lan)
            => await _repository.SearchByLanAsync(lan);

        public async Task<int> AddPaymentAsync(AddClosurePaymentDto dto)
            => await _repository.AddPaymentAsync(dto);

        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto)
            => await _repository.UpdatePaymentAsync(dto);

        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
            => await _repository.DeletePaymentAsync(id, deletedBy);

        public async Task<int> AddQuestionnaireResponseAsync(AddQuestionnaireResponseDto dto)
            => await _repository.AddQuestionnaireResponseAsync(dto);

        public async Task<string?> GetWaiverApproverAsync(GetWaiverApproverDto dto)
            => await _repository.GetWaiverApproverAsync(dto);

    }
}