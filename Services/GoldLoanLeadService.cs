using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class GoldLoanLeadService
    {
        private readonly GoldLoanLeadRepository _goldLoanLeadRepository;

        public GoldLoanLeadService(GoldLoanLeadRepository goldLoanLeadRepository)
        {
            _goldLoanLeadRepository = goldLoanLeadRepository;
        }

        public GoldLoanLeadModel? GetGoldLoanLead(int id)
        {
            return _goldLoanLeadRepository.GetGoldLoanLead(id);
        }

        public List<GoldLoanLeadModel> GetAllGoldLoanLeads()
        {
            return _goldLoanLeadRepository.GetAllGoldLoanLeads();
        }

        public int AddGoldLoanLead(AddGoldLoanLeadDTO addGoldLoanLeadDTO)
        {
            return _goldLoanLeadRepository.AddGoldLoanLead(addGoldLoanLeadDTO);
        }
    }
}
