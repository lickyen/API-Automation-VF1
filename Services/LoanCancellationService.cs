using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class LoanCancellationService
    {
        private readonly LoanCancellationRepository _repo;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public LoanCancellationService(LoanCancellationRepository repo, EmailService emailService, TemplateService templateService, UserService userService)
        {
            _repo = repo;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public async Task<LoanCancelDetailsDto?> GetByIdAsync(int id)
        {
            var transaction = await _repo.GetByIdAsync(id);
            if (transaction == null) return null;

            var logs = await _repo.GetLogsAsync(id);

            return new LoanCancelDetailsDto
            {
                Transaction = transaction,
                Logs = logs
            };
        }

        public async Task<List<LoanCancelListDto>> GetByUserAsync(LoanCancelGetByUserDto dto)
        {
            return await _repo.GetByUserAsync(dto);
        }

        public async Task<int> CreateAsync(LoanCancelCreateDto dto)
        {
            var isCash = IsCashPayment(dto.Lan);

            var id = await _repo.CreateAsync(dto, isCash);

            if (id > 0)
            {
                var record = await GetByIdAsync(id);
                if (record != null)
                {
                    await NotifyPendingUserAsync(record);
                }
            }

            return id;
        }

        public async Task<int> UpdateAsync(LoanCancelUpdateDto dto)
        {
            var result = await _repo.UpdateAsync(dto);

            if (result > 0)
            {
                var record = await GetByIdAsync(result);
                if (record != null)
                {
                    await NotifyPendingUserAsync(record);
                }
            }

            return result;
        }


        public async Task<int> DeleteAsync(LoanCancelDeleteDto dto)
        {
            var result = await _repo.DeleteAsync(dto);

            return result;
        }

        public async Task<int> TakeActionAsync(LoanCancelActionDto dto)
        {
            var result = await _repo.TakeActionAsync(dto);
            if (result < 1) return result;

            var record = await GetByIdAsync(dto.Id);
            if (record != null)
            {
                if (dto.Action == "Approved")
                {
                    var isApproved = record.Transaction.IsApproved;

                    if (isApproved)
                    {
                        await NotifyBranchAsync(record, "Approved");
                    }
                    else
                    {
                        await NotifyPendingUserAsync(record);
                    }
                }

                else if (dto.Action == "SentBack")
                {
                    await NotifyPendingUserAsync(record);
                }

                else if (dto.Action == "Rejected")
                {
                    await NotifyBranchAsync(record, "Rejected");
                }
            }

            return result;
        }

        public async Task<int> CopsActionAsync(LoanCancelCopsDto dto)
        {
            var result = await _repo.CopsActionAsync(dto);
            if (result < 1) return result;

            var record = await GetByIdAsync(dto.Id);
            if (record != null)
            {
                if (dto.Status == "Loan-Cancelled")
                {
                    await NotifyBranchAsync(record, "Completed");

                }
                else if (dto.Status == "SentBack")
                {
                    await NotifyPendingUserAsync(record);
                }

                else if (dto.Status == "Rejected")
                {
                    await NotifyBranchAsync(record, "Rejected");
                }
            }

            return result;
        }

        public async Task<byte[]?> GetReportExcelAsync(LoanCancelReportDto dto)
        {
            var data = await _repo.GetReportAsync(dto);
            return ExcelHelper.GenerateExcel(data, "Data");
        }

        public bool IsCashPayment(string lan)
        {
            return false;
        }

        private async Task NotifyPendingUserAsync(LoanCancelDetailsDto record)
        {
            var transaction = record.Transaction;

            var user = _userService.GetUser(transaction.PendingUser);

            if (user == null || string.IsNullOrEmpty(user.Email))
                return;

            var placeholders = new Dictionary<string, string>
            {
                { "Module", "Loan Cancellation" },
                { "LAN", transaction.Lan }
            };

            var template = _templateService.GetEmailTemplate("MovedToQEmail");

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            await _emailService.SendEmail(
                "Action Required - Loan Cancellation",
                body,
                new List<string> { user.Email },
                new List<string>()
            );
        }

        private async Task NotifyBranchAsync(LoanCancelDetailsDto record, string action)
        {
            var transaction = record.Transaction;

            var placeholders = new Dictionary<string, string>
            {
                { "Module", "Loan Cancellation" },
                { "LAN", transaction.Lan }
            };

            // ---------------------------
            // APPROVED FLOW
            // ---------------------------
            if (action == "Approved")
            {
                var isApproved = transaction.IsApproved;

                if (isApproved)
                {
                    // ✅ FINAL → BRANCH
                    var user = _userService.GetUser(transaction.CreatedBy);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var template = _templateService.GetEmailTemplate("LoanCancelApprovedEmail");
                        var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                        await _emailService.SendEmail(
                            "Loan Cancellation - Approved",
                            body,
                            new List<string> { user.Email },
                            new List<string>()
                        );
                    }
                }
            }

            // ---------------------------
            // REJECTED
            // ---------------------------
            else if (action == "Rejected")
            {
                var user = _userService.GetUser(transaction.CreatedBy);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var template = _templateService.GetEmailTemplate("LoanCancelRejectedEmail");
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    await _emailService.SendEmail(
                        "Loan Cancellation - Rejected",
                        body,
                        new List<string> { user.Email },
                        new List<string>()
                    );
                }
            }

            // ---------------------------
            // COMPLETED
            // ---------------------------
            else if (action == "Completed")
            {
                var user = _userService.GetUser(transaction.CreatedBy);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var template = _templateService.GetEmailTemplate("LoanCancelCompletedEmail");
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    await _emailService.SendEmail(
                        "Loan Cancellation Completed",
                        body,
                        new List<string> { user.Email },
                        new List<string>()
                    );
                }
            }
        }
    }
}
