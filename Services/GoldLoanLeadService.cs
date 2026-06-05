using ProductivityApi.Helpers;
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

        public bool UpdateGoldLoanLead(
            int id,
            UpdateGoldLoanLeadDTO dto)
        {
            return _goldLoanLeadRepository.UpdateGoldLoanLead(id, dto);
        }
        public async Task<byte[]?> GetReportExcelAsync(GoldLeadReportFilterDto dto)
        {
            var data = await _goldLoanLeadRepository.GetGoldLeadReportAsync(dto);

            return ExcelHelper.GenerateExcel(data, "Data");
        }
        public async Task<IEnumerable<dynamic>> GetReportDataAsync(GoldLeadReportFilterDto dto)
        {
            return await _goldLoanLeadRepository.GetGoldLeadReportAsync(dto);
        }

    }
}