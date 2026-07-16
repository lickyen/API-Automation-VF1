using Azure;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Humanizer;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Globalization;
using System.Text.Json;

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

        public async Task<AxisResponseModel> OnlinePaymentsValidation(AxisModel dto)
        {
            string apiName = "Online Validation API";
            try
            {
                AxisResponseModel response = new AxisResponseModel();



            string loanAccountNumber = dto.Bene_acc_no?.Length > 4
              ? dto.Bene_acc_no.Substring(4)
              : string.Empty;

          
                if (string.IsNullOrWhiteSpace(dto.Pmode))
                {
                    response = new AxisResponseModel("F", "0002", "Pmode is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }


                var allowedModes = new[] { "NEFT", "RTGS", "Transfer", "IMPS" };
                if (!allowedModes.Contains(dto.Pmode.Trim()))
                {
                    response = new AxisResponseModel("F", "0003", "Invalid payment mode. Allowed values are NEFT, RTGS,Transfer or IMPS.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);


                }

                if (string.IsNullOrWhiteSpace(dto.UTR))
                {
                    response = new AxisResponseModel("F", "0004", "UTR is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }


                if (string.IsNullOrWhiteSpace(dto.Bene_acc_no))
                {
                    response = new AxisResponseModel("F", "0005", "Loan Account Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }


                if (string.IsNullOrWhiteSpace(dto.Req_type))
                {
                    response = new AxisResponseModel("F", "0006", "Req_type is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }

                if (!dto.Req_type.Trim().Equals("Validation"))
                {
                    response = new AxisResponseModel("F", "0007", "Req_type should be 'Validation'");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);


                }

                if (string.IsNullOrWhiteSpace(dto.Txn_amnt) ||
                   !decimal.TryParse(dto.Txn_amnt, out decimal txnAmount) ||
                   txnAmount <= 0)
                {
                    response = new AxisResponseModel("F", "0008", "Txn_amnt is mandatory and must be greater than zero.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }



                if (string.IsNullOrWhiteSpace(dto.Req_date))
                {
                    response = new AxisResponseModel("F", "0009", "Req_date is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }


                if (!DateTime.TryParseExact(dto.Req_date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None, out _))
                {
                    response = new AxisResponseModel("F", "0010", "Req_date should be in yyyy-MM-dd format.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }



                if (string.IsNullOrWhiteSpace(dto.Corp_code))
                {
                    response = new AxisResponseModel("F", "0011", "Corp_code is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                if (string.IsNullOrWhiteSpace(loanAccountNumber))
                {

                    response = new AxisResponseModel("F", "0012", "LoanAccountNumber cannot be empty after removing the first 4 digits from dto.Bene_acc_no.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }



                // Check PLF Loan Status
                var PlfloanStatus = await _pLFRepository.GetLoanStatusByLanAsync(loanAccountNumber);

            if (PlfloanStatus == null)
            {
                // Check Archived Status
                var Archivedstatus = await _repository.GetArchivedStatusByLanAsync(loanAccountNumber);
                if (Archivedstatus == null)
                {
                        response = new AxisResponseModel("F", "0013", $"Loan details not found for LAN {dto.Bene_acc_no}.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);


                }
                else
                {
                    var id = await _repository.AddPaymentDetailsAsync(dto,Archivedstatus.CUSTOMER_NAME);
                    if (id == -1)
                    {
                            response = new AxisResponseModel("F", "0014", $"Same UTR and Loan number already Exists {dto.Bene_acc_no} , {dto.UTR}.");
                            return await LogAndReturn(
                            apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                    }
                    else
                    {


                            response = new AxisResponseModel("S", "0000", "Success");
                            return await LogAndReturn(
                            apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);


                    }


                }

            }


            // Validate Loan Status

            var allowedstatus = new[] { "Active Loan", "Paid Off Settled" };
            if (string.IsNullOrWhiteSpace(PlfloanStatus.LoanStatus) ||
                !allowedstatus.Contains(PlfloanStatus.LoanStatus.Trim()))
            {

                    response = new AxisResponseModel("F", "0015", $"Loan Account number is closed - {loanAccountNumber}.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

            }
            else
            {

                var id = await _repository.AddPaymentDetailsAsync(dto, PlfloanStatus.customer_name);
                if (id == -1)
                {
                        response = new AxisResponseModel("F", "0014", $"Same UTR and Loan number already Exists {dto.Bene_acc_no}.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                else
                {
                        response = new AxisResponseModel("S", "0000", "Success");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }

            }
            }
            catch (Exception ex)
            {
                var response1 = new AxisResponseModel("F", "9999", "An unexpected error occurred while updating payment details.");
                return await LogAndReturn(
                apiName, dto, response1, dto.UTR, dto.Bene_acc_no, response1.Stts_flg, response1.Err_cd);


            }



        }

        public async Task<AxisResponseModel> OnlinePaymentsNotification(AxisModel dto)
        {
            string apiName = "Online Notification API";
            try
            {
                AxisResponseModel response = new AxisResponseModel();

                string loanAccountNumber = dto.Bene_acc_no?.Length > 4
                  ? dto.Bene_acc_no.Substring(4)
                  : string.Empty;

              


             
                if (string.IsNullOrWhiteSpace(dto.Pmode))
                {
                    response = new AxisResponseModel("F", "0002", "Pmode is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                   

                var allowedModes = new[] { "NEFT", "RTGS", "Transfer", "IMPS" };
                if (!allowedModes.Contains(dto.Pmode.Trim()))
                {
                    response = new AxisResponseModel("F", "0003", "Invalid payment mode. Allowed values are NEFT, RTGS,Transfer or IMPS.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                   
                }
               
                if (string.IsNullOrWhiteSpace(dto.UTR))
                {
                    response = new AxisResponseModel("F", "0004", "UTR is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }
               

                if (string.IsNullOrWhiteSpace(dto.Bene_acc_no))
                {
                    response = new AxisResponseModel("F", "0005", "Loan Account Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }
               

                if (string.IsNullOrWhiteSpace(dto.Req_type))
                {
                    response = new AxisResponseModel("F", "0006", "Req_type is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                    
                if (!dto.Req_type.Trim().Equals("Notification"))
                {
                    response = new AxisResponseModel("F", "0007", "Req_type should be 'Notification'");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                   
                }

               

                if (string.IsNullOrWhiteSpace(dto.Txn_amnt) ||
                    !decimal.TryParse(dto.Txn_amnt, out decimal txnAmount) ||
                    txnAmount <= 0)
                {
                    response = new AxisResponseModel(
                        "F",
                        "0008",
                        "Txn_amnt is mandatory and must be greater than zero.");

                    return await LogAndReturn(apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);     
                }

                if (string.IsNullOrWhiteSpace(dto.Req_date))
                {
                    response = new AxisResponseModel("F", "0009", "Req_date is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }


                if (!DateTime.TryParseExact(dto.Req_date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None, out _))
                {
                    response = new AxisResponseModel("F", "0010", "Req_date should be in yyyy-MM-dd format.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }

                if (string.IsNullOrWhiteSpace(dto.Corp_code))
                {
                    response = new AxisResponseModel("F", "0011", "Corp_code is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                if (string.IsNullOrWhiteSpace(loanAccountNumber))
                {

                    response = new AxisResponseModel("F", "0012", "LoanAccountNumber cannot be empty after removing the first 4 digits from dto.Bene_acc_no.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);



                }

                if (string.IsNullOrWhiteSpace(dto.Sndr_acnt))
                {
                    response = new AxisResponseModel("F", "0013", "Sndr_acnt Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                }
              

                if (string.IsNullOrWhiteSpace(dto.Sndr_nm))
                {
                    response = new AxisResponseModel("F", "0014", "Sndr_nm Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                    

                if (string.IsNullOrWhiteSpace(dto.Sndr_ifsc))
                {
                    response = new AxisResponseModel("F", "0015", "Sndr_ifsc Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                   

                if (string.IsNullOrWhiteSpace(dto.Tran_id))
                {
                    response = new AxisResponseModel("F", "0016", "Tran_id Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);
                }
                    
            

                var id = await _repository.UpdatePaymentDetailsAsync(dto);
                if (id == -1)
                {
                    response = new AxisResponseModel(  "F","0017",
                    $"No eligible record found. The record was either not found or the payment status has already been updated for Bene_acc_no - {dto.Bene_acc_no} and UTR - {dto.UTR}.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                    
                }
                else
                {
                    response = new AxisResponseModel("S", "0000", "Payment details updated successfully.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, loanAccountNumber, response.Stts_flg, response.Err_cd);

                  
                }


            }
            catch(Exception ex)
            {
                var response1 = new AxisResponseModel("F", "9999", "An unexpected error occurred while updating payment details.");
                return await LogAndReturn(
                apiName, dto, response1, dto.UTR, dto.Bene_acc_no, response1.Stts_flg, response1.Err_cd);


            }


        }


        public async Task<AxisofflinefetchResponseModel> OfflinePaymentsValidation(AxisOfflineReqModel dto)
        {
            string apiName = "Offline Validation API";
            try
            {
                AxisofflinefetchResponseModel response = new AxisofflinefetchResponseModel();
                // Validate mandatory fields

                if (string.IsNullOrWhiteSpace(dto.Pmode))
                {
                    response = new AxisofflinefetchResponseModel("F", "0002", "Pmode is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                var allowedModes = new[] { "CASH", "DD", "CHEQUE" };
                if (!allowedModes.Contains(dto.Pmode.Trim()))
                {
                    response = new AxisofflinefetchResponseModel("F", "0003", "Invalid payment mode. Allowed values are CASH, DD, or CHEQUE.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }

                if (string.IsNullOrWhiteSpace(dto.UTR))
                {
                    response = new AxisofflinefetchResponseModel("F", "0004", "UTR is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

                }
                if (string.IsNullOrWhiteSpace(dto.Loan_acc_no))
                {
                    response = new AxisofflinefetchResponseModel("F", "0005", "Loan Account Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                if (string.IsNullOrWhiteSpace(dto.Req_type))
                {
                    response = new AxisofflinefetchResponseModel("F", "0006", "Req_type is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }



                if (!dto.Req_type.Trim().Equals("Validation"))
                {
                    response = new AxisofflinefetchResponseModel("F", "0007", "Req_type should be 'Validation'");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
               

                if (string.IsNullOrWhiteSpace(dto.Txn_amnt) ||
                    !decimal.TryParse(dto.Txn_amnt, out decimal txnAmount) ||
                    txnAmount <= 0)
                {
                    response = new AxisofflinefetchResponseModel(
                        "F",
                        "0008",
                        "Txn_amnt is mandatory and must be greater than zero.");

                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                if (string.IsNullOrWhiteSpace(dto.Req_date))
                {
                    response = new AxisofflinefetchResponseModel("F", "0009", "Req_date is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                if (!DateTime.TryParseExact(dto.Req_date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None, out _))
                {
                    response = new AxisofflinefetchResponseModel("F", "0010", "Req_date should be in yyyy-MM-dd format.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }

              



                var plfLoanStatus = await _pLFRepository.GetLoanStatusByLanAsync(dto.Loan_acc_no);

                if (plfLoanStatus == null)
                {
                    var archivedStatus = await _repository.GetArchivedStatusByLanAsync(dto.Loan_acc_no);

                    if (archivedStatus == null)
                    {
                        response = new AxisofflinefetchResponseModel("F", "0011", $"Loan details not found for LAN {dto.Loan_acc_no}.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

                    }

                    return await InsertOfflinePayment(dto, "500000", archivedStatus.CUSTOMER_NAME);
                }

                

                var allowedstatus = new[] { "Active Loan", "Paid Off Settled" };
                if (string.IsNullOrWhiteSpace(plfLoanStatus.LoanStatus) ||
                    !allowedstatus.Contains(plfLoanStatus.LoanStatus.Trim()))
                {

                    response = new AxisofflinefetchResponseModel("F", "0012", $"Loan Account number is closed -{dto.Loan_acc_no}.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);


                }


                if (dto.Pmode.Equals("CASH", StringComparison.OrdinalIgnoreCase))
                {
                    var cashValidation = await ValidateCashLimit(dto);
                    if (cashValidation.Stts_flg == "F")
                    {
                       // response = new AxisofflinefetchResponseModel("F", cashValidation.Err_cd, cashValidation.message,cashValidation.AmountTobeAccepted,dto.UTR);

                        response = new AxisofflinefetchResponseModel
                        {
                            Stts_flg = "F",
                            Err_cd = cashValidation.Err_cd,
                            message = cashValidation.message,
                            AmountTobeAccepted = cashValidation.AmountTobeAccepted,
                            UTR = dto.UTR
                        };
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

                    }
                   

                    return await InsertOfflinePayment(dto, cashValidation.AmountTobeAccepted, plfLoanStatus.customer_name);
                }

                return await InsertOfflinePayment(dto, "500000" , plfLoanStatus.customer_name);
            }


            catch (Exception ex)
            {
                var response1 = new AxisofflinefetchResponseModel("F", "9999", "An unexpected error occurred while updating payment details.");
                return await LogAndReturn(
                apiName, dto, response1, dto.UTR, dto.Loan_acc_no, response1.Stts_flg, response1.Err_cd);



            }
        }
      
        private async Task<AxisofflinefetchResponseModel> ValidateCashLimit(AxisOfflineReqModel dto)
        {
            
            var (data, message) = await _sharedservice.GetLanDetailsAsync(dto.Loan_acc_no);

            if (data == null || string.IsNullOrWhiteSpace(data.CustId))
            {
                return new AxisofflinefetchResponseModel("F", "0013", $"Customer details not found for LAN {dto.Loan_acc_no}.");
           
            }

            var cashPayment = await _pLFRepository.GetCashPayemntsByCIFAsync(data.CustId);

            decimal PLFCashPaid = cashPayment?.AMOUNT ?? 0m;
            decimal totalCashPaid = PLFCashPaid + Convert.ToDecimal(dto.Txn_amnt);
            decimal acceptableCash = 199999m - PLFCashPaid;
            if (totalCashPaid > 199999m)
            {
                return new AxisofflinefetchResponseModel
                {
                    Stts_flg = "F",
                    Err_cd = "0014",
                    message = "Cash limit will be exceeded.",
                    AmountTobeAccepted = acceptableCash.ToString("0.00"),
                    UTR = ""
                };
            }

           


            return new AxisofflinefetchResponseModel
            {
                Stts_flg = "S",
                Err_cd = "000",
                message = "Success",
                AmountTobeAccepted = acceptableCash.ToString("0.00"),
                UTR = dto.UTR
            };
        }

        private async Task<AxisofflinefetchResponseModel> InsertOfflinePayment(AxisOfflineReqModel dto, string? amountToBeAccepted, string CustName)
        {
            string apiName = "Offline Validation API";
            var id = await _repository.AddOfflinePaymentDetailsAsync(dto, CustName);
            AxisofflinefetchResponseModel response = new AxisofflinefetchResponseModel();
            if (id == 0)
            {
                

                response = new AxisofflinefetchResponseModel("F", "0004", $"Same UTR and Loan number already Exists {dto.Loan_acc_no} , {dto.UTR}.");
                return await LogAndReturn(
                apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

            }

            response = new AxisofflinefetchResponseModel
            {
                Stts_flg = "S",
                Err_cd = "000",
                message = "Success",
                AmountTobeAccepted = amountToBeAccepted,
                UTR = dto.UTR,
                Name = CustName
            };
            return await LogAndReturn(
            apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
        }

    

        public async Task<AxisResponseModel> OfflineUpdatePaymentDetailsAsync( AxisOfflineNotifReqModel dto)
        {
            string apiName = "Offline Notification API";
            try
            {
                AxisResponseModel response = new AxisResponseModel() ;
                // Validate mandatory fields

                if (string.IsNullOrWhiteSpace(dto.Pmode))
                {
                    response = new AxisResponseModel("F", "0002", "Pmode is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                var allowedModes = new[] { "CASH", "DD", "CHEQUE" };
                if (!allowedModes.Contains(dto.Pmode.Trim()))
                {
                    response = new AxisResponseModel("F", "0003", "Invalid payment mode. Allowed values are CASH, DD, or CHEQUE.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }

                if (string.IsNullOrWhiteSpace(dto.UTR))
                {
                     response = new AxisResponseModel("F", "0004", "UTR is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

                }

                if (string.IsNullOrWhiteSpace(dto.Loan_acc_no))
                {
                     response = new AxisResponseModel("F", "0005", "Loan Account Number is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
                  

                if (string.IsNullOrWhiteSpace(dto.Req_type))
                {
                     response = new AxisResponseModel("F", "0006", "Req_type is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
                  


                if (!dto.Req_type.Trim().Equals("Notification"))
                {
                     response = new AxisResponseModel("F", "0007", "Req_type should be 'Notification'");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
              

                if (string.IsNullOrWhiteSpace(dto.Txn_amnt) ||
                    !decimal.TryParse(dto.Txn_amnt, out decimal txnAmount) ||
                    txnAmount <= 0)
                {
                    response = new AxisResponseModel(
                        "F",
                        "0008",
                        "Txn_amnt is mandatory and must be greater than zero.");

                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                if (string.IsNullOrWhiteSpace(dto.Req_date))
                {
                     response = new AxisResponseModel("F", "0009", "Req_date is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
                 

                if (!DateTime.TryParseExact(dto.Req_date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None,  out _))
                {
                     response = new AxisResponseModel("F", "0010", "Req_date should be in yyyy-MM-dd format.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }

             
                 

             

                if (string.IsNullOrWhiteSpace(dto.PayedBy))
                {
                    response = new AxisResponseModel("F", "0011", "PayedBy is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }


                if (string.IsNullOrWhiteSpace(dto.Tran_id))
                {
                     response = new AxisResponseModel("F", "0012", "Tran_id is mandatory.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                }
                

                // For DD and CHEQUE, Cheque/DD No and IFSC are mandatory
                if (!dto.Pmode.Equals("CASH", StringComparison.OrdinalIgnoreCase))
                {
                    


                    if (string.IsNullOrWhiteSpace(dto.PayedBy_Ac_No))
                    {
                        response = new AxisResponseModel("F", "0013", "PayedBy_Ac_No is mandatory.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                    }

                    if (string.IsNullOrWhiteSpace(dto.Cheque_DD_no))
                    {
                         response = new AxisResponseModel("F", "0014", "Cheque_DD_no is mandatory for DD/CHEQUE payments.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                    }
                        
                    

                    if (string.IsNullOrWhiteSpace(dto.ifsc))
                    {
                         response = new AxisResponseModel("F", "0015", "IFSC is mandatory for DD/CHEQUE payments.");
                        return await LogAndReturn(
                        apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);
                    }
                       
                  
                }

                var updatedRows =
                     await _repository.UpdateOfflinePaymentDetailsAsync(dto);

                if (updatedRows == 0)
                {
                     response = new AxisResponseModel("F", "0016", $"No payment record found for UTR and LAN / Payment status already Updated.");
                    response = new AxisResponseModel("F","0017",
                    $"No eligible record found. The record was either not found or the payment status has already been updated for Bene_acc_no - {dto.Loan_acc_no} and UTR - {dto.UTR}.");
                    return await LogAndReturn(
                    apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);


                
                }

                 response = new AxisResponseModel("S", "0000", "Payment details updated successfully.");
                return await LogAndReturn(
                apiName, dto, response, dto.UTR, dto.Loan_acc_no, response.Stts_flg, response.Err_cd);

               

            }
            catch (Exception ex)
            {
                var response1 = new AxisResponseModel("F", "9999", "An unexpected error occurred while updating payment details.");
                return await LogAndReturn(
                apiName, dto, response1, dto.UTR, dto.Loan_acc_no, response1.Stts_flg, response1.Err_cd);


              
            }
        }



        public async Task<TResponse> LogAndReturn<TRequest, TResponse>(
    string apiName,TRequest request,TResponse response,
    string? utr,string? loanAccNo,string? status, string? errorCode)
        {
            await _repository.SaveApiLogAsync(
                apiName,
                utr,
                loanAccNo,
                JsonSerializer.Serialize(request),
                JsonSerializer.Serialize(response),
                status,
                errorCode);

            return response;
        }
    }
}