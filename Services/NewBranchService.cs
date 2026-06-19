using Microsoft.Data.SqlClient;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;

namespace ProductivityApi.Services
{
    public class NewBranchServices
    {
        private readonly NewBranchRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly ILogger<NewBranchServices> _logger;

        public NewBranchServices(
            NewBranchRepository repository,
            EmailService emailService,
            TemplateService templateService,
            ILogger<NewBranchServices> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
        }

        #region ADD NEW BRANCH

        public AddNewBranchResponseDto AddNewBranch(AddNewBranchRequestDto model)
        {
            int branchId = _repository.AddNewBranch(model);

            // Fire creation emails without blocking the response
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleCreationEmailsAsync(branchId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NewBranch Creation Email] Background error: {ex.Message}");
                }
            });

            return new AddNewBranchResponseDto
            {
                BranchId = branchId,
                Message = "Branch created successfully",
            };
        }

        #endregion

        #region GET ALL BRANCHES

        public List<NewBranchMasterModel> GetAllBranches(string? role, string? userId)
        {
            return _repository.GetAllBranches(role, userId);
        }

        #endregion


        #region GET BRANCH LIST

        public List<NewBranchListItemDto> GetBranches(GetNewBranchListRequestDto model)
        {
            return _repository.GetBranches(model);
        }

        #endregion


        #region GET BRANCH BY ID

        public NewBranchMasterModel? GetBranchById(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Branch Id must be greater than 0.", nameof(id));
            }

            return _repository.GetBranchById(id);
        }

        #endregion


        #region GET BRANCH TRANSACTIONS

        public List<ChecklistTransactionDto> GetBranchTransactions(int tid)
        {
            if (tid <= 0)
            {
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));
            }

            return _repository.GetBranchTransactions(tid);
        }

        #endregion


        #region GET BRANCH DETAILS

        public NewBranchDetailsResponseDto? GetBranchDetails(int tid)
        {
            if (tid <= 0)
            {
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));
            }

            var branch = _repository.GetBranchById(tid);

            if (branch == null)
                return null;

            var transactions = _repository.GetBranchTransactions(tid);

            return new NewBranchDetailsResponseDto { Branch = branch, Transactions = transactions };
        }

        #endregion


        #region UPDATE TRANSACTIONS

        // Modified UpdateTransactions:
        public async Task<UpdateBranchTransactionsResponseDto> UpdateTransactionsAsync(
            UpdateBranchTransactionsRequestDto model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Tid <= 0)
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Transaction Id. Must be greater than 0."
                };

            try
            {
                bool isCompleted = _repository.IsBranchCompleted(model.Tid);
                if (isCompleted)
                    return new UpdateBranchTransactionsResponseDto
                    {
                        IsSuccess = false,
                        Message = "Branch already completed. Update not allowed."
                    };

                // Collect checklist IDs that are being set to non-Pending BEFORE the update
                // (We send emails for items that transition away from Pending)
                var nonPendingChecklistIds = model.Transactions
                    .Where(t => !string.Equals(t.CurrentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.ChecklistId)
                    .ToList();

                // Main update
                bool result = _repository.UpdateTransactions(model);

                // Fire-and-forget email handling (don't let email errors break the response)
                if (nonPendingChecklistIds.Any())
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandlePostUpdateEmailsAsync(model.Tid, nonPendingChecklistIds);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[NewBranch Email] Background error: {ex.Message}");
                        }
                    });
                }

                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = true,
                    Message = "Transactions updated successfully"
                };
            }
            catch (SqlException ex)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        #endregion


        #region CHECK BRANCH COMPLETED

        public bool IsBranchCompleted(int tid)
        {
            if (tid <= 0)
            {
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));
            }

            return _repository.IsBranchCompleted(tid);
        }

        #endregion

        public async Task<byte[]?> GetNewBranchReportAsync()
        {
            var (master, transactions) = await _repository.GetNewBranchReportAsync();

            if ((master == null || !master.Any()) && (transactions == null || !transactions.Any()))
                return null;

            return ExcelHelper.GenerateExcel(
                (master, "NewBranchMaster"),
                (transactions, "NewBranchTransaction")
            );
        }
        #region UPDATE BRANCH CODE

        public UpdateBranchCodeResponseDto UpdateBranchCode(
            UpdateBranchCodeRequestDto model
        )
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Id <= 0)
            {
                return new UpdateBranchCodeResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Branch Id."
                };
            }

            if (string.IsNullOrWhiteSpace(model.BranchCode))
            {
                return new UpdateBranchCodeResponseDto
                {
                    IsSuccess = false,
                    Message = "Branch Code is required."
                };
            }

            _repository.UpdateBranchCode(model);

            return new UpdateBranchCodeResponseDto
            {
                IsSuccess = true,
                Message = "Branch code updated successfully."
            };
        }

        #endregion
        #region ADD BRANCH USER

        public AddBranchUserResponseDto AddBranchUser(
            AddBranchUserRequestDto model
        )
        {
            var id = _repository.AddBranchUser(model);

            return new AddBranchUserResponseDto
            {
                Id = id,
                Message = "Branch user added successfully"
            };
        }

        #endregion

        #region GET BRANCH USERS BY BID

        public List<BranchUserDto> GetBranchUsersByBid(int bid)
        {
            if (bid <= 0)
            {
                throw new ArgumentException(
                    "Branch Id must be greater than 0.",
                    nameof(bid)
                );
            }

            return _repository.GetBranchUsersByBid(bid);
        }

        #endregion

        public async Task HandlePostUpdateEmailsAsync(int tid, IEnumerable<int> updatedChecklistIds)
        {
            var checklistIdList = updatedChecklistIds.ToList();
            if (!checklistIdList.Any()) return;

            // 1. Get triggers — two result sets: grouped (per dept) + individual (for logging)
            List<NewBranchEmailDeptTriggerDto> grouped;
            List<NewBranchEmailLogItemDto> individual;

            try
            {
                (grouped, individual) = await _repository.GetEmailTriggersAsync(tid, checklistIdList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch email triggers for Tid={Tid}", tid);
                return;
            }

            if (!grouped.Any()) return;

            // 2. Get branch master info once
            NewBranchMasterInfoDto? branchInfo = null;
            try
            {
                branchInfo = await _repository.GetBranchMasterByTidAsync(tid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch branch master for Tid={Tid}", tid);
                return;
            }

            if (branchInfo == null)
            {
                _logger.LogWarning("No branch master found for Tid={Tid}", tid);
                return;
            }

            // 3. One email per department group
            var processed = new HashSet<string>();

            foreach (var deptTrigger in grouped)
            {
                if (processed.Contains(deptTrigger.NotifiedDepartmentGroup)) continue;
                processed.Add(deptTrigger.NotifiedDepartmentGroup);

                await SendDeptEmailAsync(tid, deptTrigger, branchInfo);

                // 4. Log each individual checklist item to prevent re-sending
                var itemsForThisDept = individual
                    .Where(i => i.NotifiedDepartmentGroup == deptTrigger.NotifiedDepartmentGroup)
                    .ToList();

                foreach (var item in itemsForThisDept)
                {
                    try
                    {
                        await _repository.LogEmailSentAsync(
                            tid,
                            item.TriggerChecklistId,
                            item.NotifiedChecklistId,
                            item.NotifiedDepartmentGroup);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to log email for Tid={Tid} CL={CL}", tid, item.NotifiedChecklistId);
                    }
                }
            }
        }

        private async Task SendDeptEmailAsync(
            int tid,
            NewBranchEmailDeptTriggerDto deptTrigger,
            NewBranchMasterInfoDto branchInfo)
        {
            try
            {
                var recipients = await _repository.GetEmailRecipientsAsync(
                    tid, deptTrigger.NotifiedDepartmentGroup);

                if (!recipients.Any())
                {
                    _logger.LogWarning(
                        "No recipients for Dept={DG} Tid={Tid} — logging to suppress retry",
                        deptTrigger.NotifiedDepartmentGroup, tid);
                    return; // logging already done in caller loop
                }

                var toEmails = recipients.Select(r => r.EmailId).Distinct().ToList();

                var template = _templateService.GetEmailTemplate("NewBranchMovedToQEmail");

                var placeholders = new Dictionary<string, string>
        {
            { "<<BranchName>>",    branchInfo.Name },
            // Pass all checklist names as a readable list
            { "<<ChecklistName>>", deptTrigger.NotifiedChecklistNames },
            { "<<Team>>",          deptTrigger.NotifiedDepartmentGroup }
        };

                var body = ReplacePlaceholders(template.Body, placeholders);
                var subject = $"New Branch Tasks Assigned – {deptTrigger.NotifiedDepartmentGroup} ({branchInfo.Name})";

                await _emailService.SendEmail(subject, body, toEmails, new List<string>(), source: "NewBranch");

                _logger.LogInformation(
                    "Email sent: Tid={Tid} Dept={DG} Checklists=[{CLs}] Recipients={Count}",
                    tid,
                    deptTrigger.NotifiedDepartmentGroup,
                    deptTrigger.NotifiedChecklistIds,
                    toEmails.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending email for Tid={Tid} Dept={DG}",
                    tid, deptTrigger.NotifiedDepartmentGroup);
            }
        }

        private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
        {
            foreach (var kv in placeholders)
                template = template.Replace(kv.Key, kv.Value ?? string.Empty);
            return template;
        }

        public async Task HandleCreationEmailsAsync(int tid)
        {
            List<NewBranchEmailDeptTriggerDto> grouped;
            List<NewBranchEmailLogItemDto> individual;

            try
            {
                (grouped, individual) = await _repository.GetCreationEmailTriggersAsync(tid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch creation email triggers for Tid={Tid}", tid);
                return;
            }

            if (!grouped.Any()) return;

            NewBranchMasterInfoDto? branchInfo = null;
            try
            {
                branchInfo = await _repository.GetBranchMasterByTidAsync(tid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch branch master for Tid={Tid}", tid);
                return;
            }

            if (branchInfo == null)
            {
                _logger.LogWarning("No branch master found for Tid={Tid}", tid);
                return;
            }

            var processed = new HashSet<string>();

            foreach (var deptTrigger in grouped)
            {
                if (processed.Contains(deptTrigger.NotifiedDepartmentGroup)) continue;
                processed.Add(deptTrigger.NotifiedDepartmentGroup);

                await SendDeptEmailAsync(tid, deptTrigger, branchInfo);

                // Log each individual item to prevent duplicate on future updates
                var itemsForThisDept = individual
                    .Where(i => i.NotifiedDepartmentGroup == deptTrigger.NotifiedDepartmentGroup)
                    .ToList();

                foreach (var item in itemsForThisDept)
                {
                    try
                    {
                        await _repository.LogEmailSentAsync(
                            tid,
                            item.TriggerChecklistId,  // 0 for creation
                            item.NotifiedChecklistId,
                            item.NotifiedDepartmentGroup);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to log creation email for Tid={Tid} CL={CL}",
                            tid, item.NotifiedChecklistId);
                    }
                }
            }
        }
    }
}
