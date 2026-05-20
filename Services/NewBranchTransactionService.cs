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
            return _transactionRepository.GetBranchTransactions(tid);
        }

        #endregion


        #region GET BRANCH DETAILS

        public NewBranchDetailsResponseDto? GetBranchDetails(int tid)
        {
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

            if (result)
            {
                return new UpdateBranchTransactionsResponseDto
                {
                    IsSuccess = true,
                    Message = "Transactions updated successfully"
                };
            }

            return new UpdateBranchTransactionsResponseDto
            {
                IsSuccess = false,
                Message = "Failed to update transactions"
            };
        }

        #endregion
    }
}