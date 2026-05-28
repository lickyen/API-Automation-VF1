using Microsoft.Data.SqlClient;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NewBranchServices
    {
        private readonly NewBranchRepository _repository;

        public NewBranchServices(NewBranchRepository repository)
        {
            _repository = repository;
        }

        #region ADD NEW BRANCH

        public AddNewBranchResponseDto AddNewBranch(AddNewBranchRequestDto model)
        {
            int branchId = _repository.AddNewBranch(model);

            return new AddNewBranchResponseDto
            {
                BranchId = branchId,
                Message = "Branch created successfully",
            };
        }

        #endregion

        #region GET ALL BRANCHES

        public List<NewBranchMasterModel> GetAllBranches()
        {
            return _repository.GetAllBranches();
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

        public UpdateBranchTransactionsResponseDto UpdateTransactions(
            UpdateBranchTransactionsRequestDto model
        )
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Tid <= 0)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Transaction Id. Must be greater than 0.",
                };
            }

            try
            {
                // Optional: branch-level lock check (keep if still needed)
                bool isCompleted = _repository.IsBranchCompleted(model.Tid);

                if (isCompleted)
                {
                    return new UpdateBranchTransactionsResponseDto
                    {
                        IsSuccess = false,
                        Message = "Branch already completed. Update not allowed.",
                    };
                }

                // Main update (SQL will now enforce Pending rule)
                bool result = _repository.UpdateTransactions(model);

                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = true,
                    Message = "Transactions updated successfully",
                };
            }
            catch (SqlException ex)
            {
                // This will now catch your SQL THROW:
                // "Update not allowed. Transaction is already processed."
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message,
                };
            }
            catch (InvalidOperationException ex)
            {
                // In case repository throws business validation error
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message,
                };
            }
            catch (Exception ex)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message,
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
    }
}
