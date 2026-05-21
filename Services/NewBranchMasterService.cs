using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NewBranchMasterServices
    {
        private readonly NewBranchMasterRepository _repository;

        public NewBranchMasterServices(NewBranchMasterRepository repository)
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
                Message = "Branch created successfully"
            };
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
                throw new ArgumentException("Branch Id must be greater than 0.", nameof(id));

            return _repository.GetBranchById(id);
        }

        #endregion

        #region CHECK BRANCH COMPLETED

        public bool IsBranchCompleted(int tid)
        {
            if (tid <= 0)
                throw new ArgumentException("Transaction Id must be greater than 0.", nameof(tid));

            return _repository.IsBranchCompleted(tid);
        }

        #endregion
    }
}