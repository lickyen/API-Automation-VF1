using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BranchServices
    {
        private readonly BranchRepository _branchRepository;

        public BranchServices(BranchRepository branchRepository)
        {
            _branchRepository = branchRepository;
        }
        public List<BranchModel> GetBranches()
        {
            return _branchRepository.GetBranches();
        }
        public BranchModel? GetBranchByID(string BranchCode)
        {         
            return _branchRepository.GetBranchByID(BranchCode);
        }

    }
}
