using ProductivityApi.Models;
using ProductivityApi.Repositories;
using Serilog;
using System.Runtime.InteropServices;


namespace ProductivityApi.Services
{
    public class SOALODFCLService
    {
        private readonly SOALODFCLRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public SOALODFCLService(SOALODFCLRepository repository, EmailService emailService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
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
        public async Task<SOACategoryResponseDto?> GetLoanCategoryAsync(SOACategoryRequestDto dto)
        {
            var result = await _repository.GetLoanCategoryAsync(dto);
            return result;
        }
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


    }
}
