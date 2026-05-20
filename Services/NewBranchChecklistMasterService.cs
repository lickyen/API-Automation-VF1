using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NewBranchChecklistMasterServices
    {
        private readonly NewBranchChecklistMasterRepository _repository;

        public NewBranchChecklistMasterServices(
            NewBranchChecklistMasterRepository repository)
        {
            _repository = repository;
        }

        #region GET CHECKLIST MASTER

        public List<ChecklistMasterDto> GetChecklistMaster()
        {
            return _repository.GetChecklistMaster();
        }

        #endregion
    }
}