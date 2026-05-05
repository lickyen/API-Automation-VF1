using Azure;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class LoanCancellationService
    {
        private readonly LoanCancellationRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private readonly PLFRepository _plfRepository;
        private readonly PLFService _plfService;
        private readonly string _filePath;

        public LoanCancellationService(LoanCancellationRepository repository, EmailService emailService, TemplateService templateService, UserService userService, AppSettings appSettings, PLFRepository plfRepository, PLFService plfService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
            _plfRepository = plfRepository;
            _plfService = plfService;
            _filePath = appSettings.FilePaths.LegalBillingFilePath;
        }

        public async Task<LoanCancelDetailsDto?> GetByIdAsync(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null) return null;

            var logs = await _repository.GetLogsAsync(id);

            var payments = await _plfRepository.GetPaymentsByLan(transaction.Lan);

            return new LoanCancelDetailsDto
            {
                Transaction = transaction,
                Logs = logs,
                Payments = payments
            };
        }

        public async Task<List<LoanCancelRequest>> GetByUserAsync(LoanCancelGetByUserDto dto)
        {
            return await _repository.GetByUserAsync(dto);
        }

        public async Task<int> CreateAsync(LoanCancelCreateDto dto)
        {
            var id = await _repository.CreateAsync(dto);

            if (id > 0)
            {
                if (dto.ReqForm != null)
                {
                    await UploadBase64FileAsync(id, dto.ReqForm, "ReqForm");
                }

                if (dto.EmiProof != null)
                {
                    await UploadBase64FileAsync(id, dto.EmiProof, "EmiProof");
                }

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
            var result = await _repository.UpdateAsync(dto);

            if (result > 0)
            {
                if (dto.ReqForm != null)
                {
                    await UploadBase64FileAsync(dto.Id, dto.ReqForm, "ReqForm");
                }

                if (dto.EmiProof != null)
                {
                    await UploadBase64FileAsync(dto.Id, dto.EmiProof, "EmiProof");
                }

                var record = await GetByIdAsync(dto.Id);
                if (record != null)
                {
                    await NotifyPendingUserAsync(record);
                }
            }

            return result;
        }


        public async Task<int> DeleteAsync(LoanCancelDeleteDto dto)
        {
            var result = await _repository.DeleteAsync(dto);

            return result;
        }
        public async Task<int> TakeActionAsync(LoanCancelActionDto dto)
        {
            var result = await _repository.TakeActionAsync(dto);
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

        public async Task<(int result, string message)> CopsActionAsync(LoanCancelCopsDto dto)
        {
            var result = await _repository.CopsActionAsync(dto);
            if (result < 1)
                return (result, "Error");

            var record = await GetByIdAsync(dto.Id);

            if (record == null)
                return (result, "Response submitted successfully");

            var hasCashPayment = record.Payments?
                .Any(p => string.Equals(p.ReceiptMode, "Cash", StringComparison.OrdinalIgnoreCase)) == true;

            var plfStatus = new PlfStatusUpdateDto
            {
                Id = dto.Id
            };

            string message;

            if (string.Equals(dto.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasCashPayment)
                {
                    try
                    {
                        var cancelRequest = BuildCancelLoanRequest(record);
                        var response = await _plfService.CancelLoan(cancelRequest);

                        if (response.returnCode == "0000")
                        {
                            plfStatus.PlfStatus = "Success";
                            plfStatus.PlfResponse = response.returnText;

                            message = "Loan cancelled successfully in Pennant";
                        }
                        else
                        {
                            plfStatus.PlfStatus = "Failed";
                            plfStatus.PlfResponse = response.returnText;

                            message = "Loan cancellation failed in Pennant";
                        }
                    }
                    catch (Exception ex)
                    {
                        plfStatus.PlfStatus = "Failed";
                        plfStatus.PlfResponse = ex.Message;

                        message = "Response submitted, but Pennant cancellation failed.";
                    }
                }
                else
                {
                    plfStatus.PlfStatus = "Skipped";
                    plfStatus.PlfRemarks = "Cash payment found. Manual cancellation required.";

                    message = plfStatus.PlfRemarks;
                }

                await _repository.UpdatePlfStatusAsync(plfStatus);

                await NotifyBranchAsync(record, dto.Status);

                return (result, message);
            }
            else if (string.Equals(dto.Status, "SentBack", StringComparison.OrdinalIgnoreCase))
            {
                await NotifyPendingUserAsync(record);
                message = "Request sent back successfully";
            }
            else if (string.Equals(dto.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                await NotifyBranchAsync(record, dto.Status);
                message = "Request rejected successfully";
            }
            else
            {
                message = "Response submitted successfully";
            }

            return (result, message);
        }
        private CancelLoanRequest BuildCancelLoanRequest(LoanCancelDetailsDto record)
        {
            var txn = record.Transaction;

            return new CancelLoanRequest
            {
                financeSchedule = new financeSchedule
                {
                    finReference = txn.Lan 
                },
                reasonHeader = new ReasonHeader
                {
                    remarks = txn.ReasonForCancellation ?? "Loan cancelled via Productivity App",
                    cancelType = "C",
                    reasonDetails = new List<ReasonDetail>
                    {
                        new ReasonDetail
                        {
                            reasonCode = "LOANCANC"
                        }
                    }
                }
            };
        }

        public async Task<byte[]?> GetReportExcelAsync(LoanCancelReportDto dto)
        {
            var data = await _repository.GetReportAsync(dto);
            return ExcelHelper.GenerateExcel(data, "Data");
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
        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            return await FileHelper.ReadFileAsync(
                _filePath,
                fileName
            );
        }

        public async Task<int> UploadBase64FileAsync(int reqId, FileUploadBase64 fileDto, string fileType)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".zip" };

            var extension = Path.GetExtension(fileDto.FileName);

            var fileName = $"{reqId}_{fileType}{extension}";
            var fullPathFileName = fileName;

            var result = await FileHelper.SaveBase64FileAsync(
                _filePath,
                fullPathFileName,
                fileDto.Base64,
                allowedExtensions
            );

            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            var dbResult = await _repository.UpdateFilePathAsync(reqId, fileType, fileName);

            if (dbResult <= 0)
                throw new InvalidOperationException("Failed to update file path in DB");

            return dbResult;
        }
    }
}
