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
        public List<BranchModel> GetBranchesByZone(string zone)
        {
            return _branchRepository.GetBranchesByZone(zone);
        }
        public BranchModel? GetBranchByID(string BranchCode)
        {
            return _branchRepository.GetBranchByID(BranchCode);
        }

        public List<BranchAddressModel> GetBranchesAddress()
        {
            return _branchRepository.GetBranchesAddress();
        }

    }
}
