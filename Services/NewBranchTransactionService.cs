using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NewBranchTransactionServices
    {
        private readonly NewBranchTransactionRepository _transactionRepository;
        private readonly NewBranchMasterRepository _masterRepository;

        public NewBranchTransactionServices(
            NewBranchTransactionRepository transactionRepository,
            NewBranchMasterRepository masterRepository)
        {
            _transactionRepository = transactionRepository;
            _masterRepository = masterRepository;
        }

        #region GET BRANCH TRANSACTIONS

        public List<ChecklistTransactionDto> GetBranchTransactions(int tid)
        {
            if (tid <= 0)
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));

            return _transactionRepository.GetBranchTransactions(tid);
        }

        #endregion

        #region GET BRANCH DETAILS

        public NewBranchDetailsResponseDto? GetBranchDetails(int tid)
        {
            if (tid <= 0)
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));

            var branch = _masterRepository.GetBranchById(tid);

            if (branch == null)
                return null;

            var transactions = _transactionRepository.GetBranchTransactions(tid);

            return new NewBranchDetailsResponseDto
            {
                Branch = branch,
                Transactions = transactions
            };
        }

        #endregion

        #region UPDATE TRANSACTIONS

        public UpdateBranchTransactionsResponseDto UpdateTransactions(
            UpdateBranchTransactionsRequestDto model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.Tid <= 0)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Transaction Id. Must be greater than 0."
                };
            }

            bool isCompleted = _masterRepository.IsBranchCompleted(model.Tid);

            if (isCompleted)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = false,
                    Message = "Branch already completed. Update not allowed."
                };
            }

            bool result = _transactionRepository.UpdateTransactions(model);

            return new UpdateBranchTransactionsResponseDto
            {
                IsSuccess = result,
                Message = result
                    ? "Transactions updated successfully"
                    : "Failed to update transactions"
            };
        }

        #endregion
    }
}