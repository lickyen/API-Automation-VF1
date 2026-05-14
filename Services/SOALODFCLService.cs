using ProductivityApi.Models;
using ProductivityApi.Repositories;
using Serilog;
using System.Runtime.InteropServices;
using System.Data;
using System.Net.Mail;
using System.Text;
using ProductivityApi.Models.PLF;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Reflection;
using Azure;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ProductivityApi.Services
{
    public class SOALODFCLService
    {
        private readonly SOALODFCLRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private readonly AppSettings _appSettings;
        private readonly PLFService _plfService;
        private readonly SharedService _sharedservice;


        public SOALODFCLService(SOALODFCLRepository repository, EmailService emailService, TemplateService templateService, UserService userService, AppSettings appSettings, PLFService plfService, SharedService sharedservice)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
            _appSettings = appSettings;
            _plfService = plfService;
            _sharedservice = sharedservice;
        }
        public async Task<IEnumerable<SOADashboard>> GetDashboardByUserAsync(SOADashboardRequestDto dto)
        {
            return await _repository.GetDashboardByUserAsync(dto);
        }
        public async Task<SOADashboard?> GetSettlementByIdAsync(int id)
        {
            var result = await _repository.GetSettlementByIdAsync(id);
            return result;
        }
        //public async Task<SOACategoryResponseDto?> GetLoanCategoryAsync(SOACategoryRequestDto dto)
        //{
        //    var result = await _repository.GetLoanCategoryAsync(dto);
        //    return result;
        //}
        public async Task<SOABaseChargeResponseDto?> getBaseChargesAsyc(string type)
        {
            var result = await _repository.GetBaseChargesAsync(type);
            return result;
        }
        public async Task<IEnumerable<ActiverequestsResponseDto>> GetActiverequestsAsync(string Lan)
        {
            return await _repository.GetActiveRequestsByLANAsync(Lan);
        }
        public async Task<int> CancelExistingRequestAsync(CancelrequestDto dto)
        {
            var result = await _repository.CancelExistingRequestAsync(dto);
            return result;
        }
        // Create a new SOALODFCL request
        public async Task<int> CreateSOALODFCLAsync(CreateSOALODDto dto)
        {
           
            bool isWaiver = false;

          
            isWaiver = dto.charges.Any(x => x.IsWaiver == 1);
          

            if ((dto.Category== "Critical" || dto.Category== "High Critical" || dto.Bucket== "B1-B3" || dto.Bucket== "NPA")) //Critical / High Critical / B1-B3 / NPA
            {
                if (!isWaiver) //No Waiver
                {
                    dto.status = "Auto-Approved";
                }
                else // Waiver
                {
                    dto.status = "Initiated";
                }
                
            }
            else if (dto.SOA == 1 && dto.LOD == 0 && dto.FCL == 0) // ONLY SOA Selected
            {
                dto.status = "Auto-Approved";
            }

            else if(dto.FCL==1 || dto.LOD==1)  //FCL or LOD Selected
            {
                dto.status = "New";
            }
            
            else //default
            {
                dto.status = "Initiated";
            }

                var result = await _repository.CreateSOALODFCLAsync(dto);

            return result;
        }

        public async Task<int> AddChargesAsync(AddChargesDto dto)
        {
            var result = await _repository.AddChargesAsync(dto);

            return result;
        }

        public async Task<int> ApproveSOALODAsync(ApproveSOALODDto dto)
        {
            var result = await _repository.ApproveSOALODAsync(dto);

            return result;
        }
        public async Task<int> CheckIfApproved(int id)
        {
            var result = await _repository.CheckIfApproved(id);

            return result;
        }

        public async Task<int> NOCSubmitAsync(SOALODNOCSubmitDto dto)
        {
            var basePath = _appSettings.FilePaths.SOALODFilePath;

           // var record = await _repository.GetByLanAsync(dto.Lan);
            //if (record == null) return -1;

            string? LODFilePath = "";
            int returnId;
            

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                Directory.CreateDirectory(basePath);

                // Bank Proof
                if (dto.LODDoc != null && !string.IsNullOrEmpty(dto.LODDoc.Base64))
                {
                    var bankProofExt = Path.GetExtension(dto.LODDoc.FileName);
                    var bankProofName = $"{dto.Id}_NOCLOD{bankProofExt}";

                    await File.WriteAllBytesAsync(
                        Path.Combine(basePath, bankProofName),
                        Convert.FromBase64String(dto.LODDoc.Base64)
                    );

                    LODFilePath = bankProofName;
                }

                
            }

            returnId = await _repository.NOCSubmitAsync(dto, LODFilePath);
            if(returnId>0)
            {

            }
            return returnId;
        }
        public async Task<TransferLetterResponse?> GetSOA (GenerateStatementRequest request, int Id)
        {
            var result = await _plfService.GenerateStatement(request);
            if (result == null || result?.ReturnStatus?.returnCode != "0000")
            {
                throw new Exception("LAN not found or InActive");
            }
            var letterId = result.LetterID;
            if(letterId.ToString() ==null || letterId==0)
            {
                throw new Exception("API Not working");
            }
            TransferLetterRequest data = new TransferLetterRequest();
            data.ServiceName = "Annual Loan Statement";
            data.RequestID = letterId.ToString();
            var SOA = await _plfService.TransferLetter(data);

            // save SOA to Local Path

            var basePath = _appSettings.FilePaths.SOALODFilePath;
            var fileName="";
            if (!string.IsNullOrWhiteSpace(SOA.DocContent) && !string.IsNullOrEmpty(SOA.DocContent))
            {
                Directory.CreateDirectory(basePath);
                 fileName = $"{Id}_SOA.pdf";

                    await File.WriteAllBytesAsync(
                        Path.Combine(basePath, fileName),
                        Convert.FromBase64String(SOA.DocContent)
                    );
            }

            var db = _repository.SaveSOALODFCLDocsAsync(Id, "SOA", fileName);

            return SOA;
        }
        public async Task<PaymentLinkResponseDto> GeneratePaymentLink(int id)
        {
            PaymentLinkResponseDto response= new PaymentLinkResponseDto();
            var settelment = await _repository.GetSettlementByIdAsync(id);
            if (settelment != null)
            {
                string Refid = await _repository.GetPaymentRefID();
                long expireBy = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
                string description = $"{settelment.Lan} - Payment for SOA/LOD/FCL";
                int amount = Convert.ToInt16(settelment.Total_Charges);
                var request = new PaymentLinkRequestDto
                {
                    Amount = (int)(amount * 100), // Razorpay accepts amount in paise
                    Currency = "INR",
                    AcceptPartial = false,
                    ExpireBy = expireBy,
                    ReferenceId = Refid,
                    Description = description,

                    Customer = new PaymentCustomerDto
                    {
                        Name = settelment.Cust_Name,
                        Contact = settelment.Mobile,
                        Email = settelment.Email
                    },

                    Notify = new NotifyDto
                    {
                        Sms = true,
                        Email = true
                    },

                    ReminderEnable = true,

                    Notes = new NotesDto
                    {
                        PolicyName = "SOA/LOD/FCL"
                    },

                    CallbackUrl = "https://vistaarfinance.com/",
                    CallbackMethod = "get"
                };
                response = await _sharedservice.PaymentLink(request);
                if (response.Error != null && !string.IsNullOrEmpty(response.ReferenceId))
                {
                    int Pid = await _repository.AddPayment_Online(response, id, settelment.Lan, "");
                }
            }
            return response;
        }
        public async Task<IEnumerable<PaymentLinkTransaction>> GetPaymentLinkstatus(int Id)
        { 
            return await _repository.GetPaylinkdetails(Id);
            
        }
    }
}
