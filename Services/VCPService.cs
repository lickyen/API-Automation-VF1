using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class VCPService
    {
        private readonly VCPRepository _repository;
        private readonly string _filePath;

        public VCPService(VCPRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.VCPFilePath;
        }

        public async Task<VCPDetailDto?> GetByIdAsync(int id)
        {
            var vcp = await _repository.GetByIdAsync(id);

            if (vcp == null)
                return null;

            var dto = new VCPDetailDto { VCP = vcp };

            dto.Logs = (await _repository.GetLogsAsync(id)).ToList();

            if (!string.IsNullOrEmpty(vcp.HUBStatus))
                dto.CheckList = await _repository.GetCheckListAsync(id);

            if (vcp.NBFCDetailsBankPartners == "Yes")
                dto.PartnerDetails = (await _repository.GetPartnerDetailsAsync(id)).ToList();

            if (vcp.AreYouRelatedToEmployeeOfVistaar == "Yes")
                dto.EmployeeRelations = (await _repository.GetEmployeeRelationsAsync(id)).ToList();

            return dto;
        }

        public async Task<List<VCPListItem>> GetByUserAsync(VCPGetByUserRequestDto dto)
        {
            return await _repository.GetByUserAsync(dto);
        }

        private async Task<string> UploadFileAsync(int reqId, FileUploadBase64 fileDto, string fileType)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".zip" };
            var extension = Path.GetExtension(fileDto.FileName);
            var fileName = $"{reqId}_{fileType}{extension}";

            var result = await FileHelper.SaveBase64FileAsync(_filePath, fileName, fileDto.Base64, allowedExtensions);

            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            return fileName;
        }

        private async Task AddLogAsync(int reqId, string finalStatus, string remarks, string updatedBy)
        {
            await _repository.AddLogAsync(new VCPAddLogRequest
            {
                ReqId = reqId,
                Status = finalStatus,
                Remarks = remarks,
                CreatedBy = updatedBy
            });
        }

        public async Task<int> SubmitRCUAsync(VCPSubmitRCURequest request)
        {
            string? savedFileName = null;

            if (request.File != null && !string.IsNullOrWhiteSpace(request.File.Base64))
                savedFileName = await UploadFileAsync(request.Id, request.File, "RCU_Report");

            var finalStatus = request.Status switch
            {
                "Positive" => "RCUPositive_RBHPending",
                "Referred" => "RCUReferred_RBHPending",
                "Negative" => "RCUNegative_RBHPending",
                "Rejected" => "RCU_Rejected",
                "Exception" => "RCU_Pending",
                "SentBack" => "RCU_SentBack",
                _ => throw new ArgumentException("Invalid RCU status")
            };

            var output = await _repository.SubmitRCUAsync(request, finalStatus, savedFileName);

            if (output > 0)
                await AddLogAsync(request.Id, finalStatus, request.Remarks, request.UpdatedBy);

            return output;
        }

        public async Task<int> SubmitBHAsync(VCPSubmitBHRequest request)
        {
            var rcuStatus = await _repository.GetRCUStatusAsync(request.Id);

            if (string.IsNullOrEmpty(rcuStatus))
                return -1;

            string finalStatus;

            switch (request.Role.ToUpper())
            {
                case "RBH":
                    finalStatus = (rcuStatus, request.Status) switch
                    {
                        ("Positive", "Approved") => "COPS_Pending",
                        ("Positive", "SentBack") => "RCUPositive_RBHSentBack",
                        ("Referred", "Approved") => "RCUReferred_ZBHPending",
                        ("Referred", "SentBack") => "RCUReferred_RBHSentBack",
                        ("Negative", "Approved") => "RCUNegative_ZBHPending",
                        ("Negative", "SentBack") => "RCUNegative_RBHSentBack",
                        _ => throw new ArgumentException("Invalid RBH status")
                    };
                    break;

                case "ZBH":
                    finalStatus = (rcuStatus, request.Status) switch
                    {
                        ("Referred", "Approved") => "COPS_Pending",
                        ("Referred", "SentBack") => "RCUReferred_ZBHSentBack",
                        ("Negative", "Approved") => "RCUNegative_MZBHPending",
                        ("Negative", "SentBack") => "RCUNegative_ZBHSentBack",
                        _ => throw new ArgumentException("Invalid ZBH status")
                    };
                    break;

                case "MZBH":
                    finalStatus = request.Status switch
                    {
                        "Approved" => "COPS_Pending",
                        "SentBack" => "RCUReferred_MZBHSentBack",
                        _ => throw new ArgumentException("Invalid MZBH status")
                    };
                    break;

                default:
                    return -1;
            }

            var output = await _repository.SubmitBHAsync(request, finalStatus);

            if (output > 0)
                await AddLogAsync(request.Id, finalStatus, request.Remarks, request.UpdatedBy);

            return output;
        }

        public async Task<int> SubmitCOPSAsync(VCPSubmitCOPSRequest request)
        {
            var finalStatus = request.Status switch
            {
                "Approved" => "ChannelSpoc_Pending",
                "SentBack" => "COPS_SentBack",
                "Rejected" => "COPS_Rejected",
                "Exception" => "COPS_Pending",
                _ => throw new ArgumentException("Invalid COPS status")
            };

            var output = await _repository.SubmitCOPSAsync(request, finalStatus);

            if (output > 0)
                await AddLogAsync(request.Id, finalStatus, request.Remarks, request.UpdatedBy);

            return output;
        }

        public async Task<int> SubmitChannelSPOCAsync(VCPSubmitChannelSPOCRequest request)
        {
            var finalStatus = request.Status switch
            {
                "Approved" => "Completed",
                "SentBack" => "ChannelSpoc_SentBack",
                "Rejected" => "ChannelSpoc_Rejected",
                _ => throw new ArgumentException("Invalid ChannelSPOC status")
            };

            var output = await _repository.SubmitChannelSPOCAsync(request, finalStatus);

            if (output > 0)
                await AddLogAsync(request.Id, finalStatus, request.Remarks, request.UpdatedBy);

            return output;
        }
    }
}