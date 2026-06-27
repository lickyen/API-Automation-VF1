using ProductivityApi.Models;
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
            if (string.IsNullOrWhiteSpace(dto.Req_type) || dto.Req_type != "Validation")
            {
                response.Stts_flg = "F";
                response.Err_cd = "0002";
                response.message = "Req_type should not be empty and must be set to 'Validation'.";
                return response;
            }

            // Check PLF Loan Status
            var PlfloanStatus = await _pLFRepository.GetLoanStatusByLanAsync(dto.Loan_acc_no);

            if (PlfloanStatus == null) 
            {
                // Check Archived Status
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

          
                // Validate Loan Status

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
               if (dto.Pmode== "CASH")
                {
                    //Get Customer id
                    var (data, message) = await _sharedservice.GetLanDetailsAsync(dto.Loan_acc_no);
                    var custid = data.CustId;
                    var cashpayment = await _pLFRepository.GetCashPayemntsByCIFAsync(custid);

                }
                

                response.Stts_flg = "S";
                    response.Err_cd = "000";
                    response.message = "Success";
                    return response;
                    
                }


               
        }

        public async Task<AxisResponseModel> OnlinePaymentsValidation(AxisModel dto)
        {
            AxisResponseModel response = new AxisResponseModel();
            var allowedModes = new[] { "NEFT", "RTGS", "Transfer", "IMPS" };
            if (string.IsNullOrWhiteSpace(dto.Pmode) ||
       !allowedModes.Contains(dto.Pmode.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0001";
                response.message = "Invalid payment mode. Allowed values are NEFT, RTGS,Transfer or IMPS.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(dto.Req_type) || dto.Req_type != "Validation")
            {
                response.Stts_flg = "F";
                response.Err_cd = "0002";
                response.message = "Req_type should not be empty and must be set to 'Validation'.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(dto.Bene_acc_no) || string.IsNullOrWhiteSpace(dto.UTR) || string.IsNullOrWhiteSpace(dto.Req_date) || string.IsNullOrWhiteSpace(dto.Txn_amnt)
                )
            {
                response.Stts_flg = "F";
                response.Err_cd = "0003";
                response.message = "Bene_acc_no / UTR / Req_date / Txn_amnt should not be empty.";
                return response;
            }

            string loanAccountNumber = dto.Bene_acc_no?.Length > 4
    ? dto.Bene_acc_no.Substring(4)
    : string.Empty;

            if (string.IsNullOrWhiteSpace(loanAccountNumber))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0004";
                response.message = "LoanAccountNumber cannot be empty after removing the first 4 digits from dto.Bene_acc_no.";
                return response;
            }


            // Check PLF Loan Status
            var PlfloanStatus = await _pLFRepository.GetLoanStatusByLanAsync(loanAccountNumber);

            if (PlfloanStatus == null)
            {
                // Check Archived Status
                var Archivedstatus = await _repository.GetArchivedStatusByLanAsync(loanAccountNumber);
                if (Archivedstatus == null)
                {
                    response.Stts_flg = "F";
                    response.Err_cd = "0005";
                    response.message = $"Loan details not found for LAN {dto.Bene_acc_no}.";
                    return response;


                }
                else
                {
                    var id = await _repository.AddPaymentDetailsAsync(dto);
                    if (id == -1)
                    {
                        response.Stts_flg = "F";
                        response.Err_cd = "0006";
                        response.message = $"Same UTR and Loan number already Exists {dto.Bene_acc_no} , {dto.UTR}.";
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


            // Validate Loan Status

            var allowedstatus = new[] { "Active Loan", "Paid Off Settled" };
            if (string.IsNullOrWhiteSpace(PlfloanStatus.LoanStatus) ||
  !allowedstatus.Contains(PlfloanStatus.LoanStatus.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0003";
                response.message = $"Loan Account number is closed -{loanAccountNumber}.";
                return response;


            }
            else
            {

                var id = await _repository.AddPaymentDetailsAsync(dto);
                if (id == -1)
                {
                    response.Stts_flg = "F";
                    response.Err_cd = "0006";
                    response.message = $"Same UTR and Loan number already Exists {dto.Bene_acc_no}.";
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

        public async Task<AxisResponseModel> OnlinePaymentsNotification(AxisModel dto)
        {
            AxisResponseModel response = new AxisResponseModel();
            var allowedModes = new[] { "NEFT", "RTGS", "Transfer", "IMPS" };
            if (string.IsNullOrWhiteSpace(dto.Pmode) ||
       !allowedModes.Contains(dto.Pmode.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0001";
                response.message = "Invalid payment mode. Allowed values are NEFT, RTGS,Transfer or IMPS.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(dto.Req_type) || dto.Req_type != "Notification")
            {
                response.Stts_flg = "F";
                response.Err_cd = "0002";
                response.message = "Req_type should not be empty and must be set to 'Notification'.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(dto.Bene_acc_no) || string.IsNullOrWhiteSpace(dto.UTR) || string.IsNullOrWhiteSpace(dto.Req_date) || string.IsNullOrWhiteSpace(dto.Txn_amnt) || string.IsNullOrWhiteSpace(dto.Sndr_acnt) || string.IsNullOrWhiteSpace(dto.Sndr_nm) || string.IsNullOrWhiteSpace(dto.Sndr_ifsc) || string.IsNullOrWhiteSpace(dto.Corp_code) || string.IsNullOrWhiteSpace(dto.Tran_id)
                )
            {
                response.Stts_flg = "F";
                response.Err_cd = "0003";
                response.message = "Bene_acc_no / UTR / Req_date / Txn_amnt /Sndr_acnt / Sndr_nm / Sndr_ifsc/ Corp_code/ Tran_id should not be empty.";
                return response;
            }

            string loanAccountNumber = dto.Bene_acc_no?.Length > 4
    ? dto.Bene_acc_no.Substring(4)
    : string.Empty;

            if (string.IsNullOrWhiteSpace(loanAccountNumber))
            {
                response.Stts_flg = "F";
                response.Err_cd = "0004";
                response.message = "LoanAccountNumber cannot be empty after removing the first 4 digits from dto.Bene_acc_no.";
                return response;
            }


            var id = await _repository.UpdatePaymentDetailsAsync(dto);
            if (id==-1)
            {
                response.Stts_flg = "F";
                response.Err_cd = "0005";
                response.message = $"Record not found for mentioned Bene_acc_no- {dto.Bene_acc_no} and UTR- {dto.UTR}";
                return response;
            }
            else
            {
                response.Stts_flg = "S";
                response.Err_cd = "0000";
                response.message = "Success";
                return response;
            }





        }

    }
}
