using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class AxisService
    {
        private readonly AxisRepository _repository;
        private readonly PLFService _plfService;
        private readonly PLFRepository _pLFRepository;
        private readonly SharedService _sharedservice;

        public AxisService(AxisRepository axisRepository, PLFService plfService, SharedService sharedservice, PLFRepository pLFRepository)
        {
            _repository = axisRepository;
            _plfService = plfService;
            _sharedservice = sharedservice;
            _pLFRepository = pLFRepository;
        }
        public async Task<AxisofflinefetchResponseModel> OfflinePaymentsValidation(AxisOfflineReqModel dto)
        {
            AxisofflinefetchResponseModel response = new AxisofflinefetchResponseModel();
             var allowedModes = new[] { "CASH", "DD", "CHEQUE" };
            if (string.IsNullOrWhiteSpace(dto.Pmode) ||
       !allowedModes.Contains(dto.Pmode.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0001";
                response.message = "Invalid payment mode. Allowed values are CASH, DD, or CHEQUE.";
                return response;
            }

            // Get loan status
            var PlfloanStatus = await _pLFRepository.GetLoanStatusByLanAsync(dto.Loan_acc_no);

            if (PlfloanStatus == null) // if Lan not availble in PLF then check Archived
            {
                var Archivedstatus = await _repository.GetArchivedStatusByLanAsync(dto.Loan_acc_no);
                if (Archivedstatus==null)
                {
                    response.Stts_flg = "F";
                    response.Err_cd = "0002";
                    response.message = $"Loan details not found for LAN {dto.Loan_acc_no}.";
                    return response;

                 
                }
                else
                {
                    response.Stts_flg = "S";
                    response.Err_cd = "000";
                    response.message = "Success";
                    return response;
                  
                }
                    
            }
            else
            {
                var allowedstatus = new[] { "Active Loan", "Paid Off Settled" };
                if (string.IsNullOrWhiteSpace(PlfloanStatus.LoanStatus) ||
      !allowedstatus.Contains(PlfloanStatus.LoanStatus.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    response.Stts_flg = "F";
                    response.Err_cd = "0003";
                    response.message = $"Loan Account number is closed -{dto.Loan_acc_no}.";
                    return response;

                  
                }
                else
                {
                    response.Stts_flg = "S";
                    response.Err_cd = "000";
                    response.message = "Success";
                    return response;
                    
                }


                  

            }



               
        }
    }
}
