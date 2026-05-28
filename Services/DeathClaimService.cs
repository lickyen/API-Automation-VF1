using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class DeathClaimService
    {
        private readonly DeathClaimRepository _repository;
        private readonly ILogger<DeathClaimService> _logger;

        public DeathClaimService(DeathClaimRepository repository, ILogger<DeathClaimService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DeathClaimResponseDto?> GetByIdAsync(int id)
        {
            var response = new DeathClaimResponseDto();
            response.Transaction = await _repository.GetByIdAsync(id);
            response.Logs = await _repository.GetLogsAsync(id);
            return response;
        }

        public async Task<int> IntimationAsync(IntimationRequestDto dto)
        {
            string status = "New";
            var result = await _repository.IntimationAsync(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = result,
                    Status = status,
                    CreatedBy = dto.CreatedBy
                });
            }

            return result;
        }

        public async Task<int> UploadDocsAsync(UploadDocsRequestDto dto)
        {
            string status = "BR_1";
            var result = await _repository.UploadDocsAsync(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitBranch2Async(SubmitBranch2RequestDto dto)
        {
            string status = "BR_2";

            var result = await _repository.SubmitBranch2Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitBranch3Async(SubmitBranch3RequestDto dto)
        {
            string status = "BR_3";
            var result = await _repository.SubmitBranch3Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitBranch5Async(SubmitBranch5RequestDto dto)
        {
            string status = "BR_5";
            var result = await _repository.SubmitBranch5Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops1Async(SubmitCops1RequestDto dto)
        {
            var result = await _repository.SubmitCops1Async(dto);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = dto.Status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops2Async(SubmitCops2RequestDto dto)
        {
            var result = await _repository.SubmitCops2Async(dto);

            if (result > 0 && !string.IsNullOrWhiteSpace(dto.Status))
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = dto.Status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        //public async Task<int> SaveCops2Async(SaveCops2RequestDto dto)
        //{
        //    return await _repository.SaveCops2Async(dto);
        //}

        public async Task<int> SubmitCops3Async(SubmitCops3RequestDto dto)
        {
            var result = await _repository.SubmitCops3Async(dto);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = dto.Status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops4Async(SubmitCops4RequestDto dto)
        {
            var result = await _repository.SubmitCops4Async(dto);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = dto.Status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops5Async(SubmitCops5RequestDto dto)
        {
            var result = await _repository.SubmitCops5Async(dto);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = dto.Status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<byte[]?> GetReportExcelAsync()
        {
            var data = await _repository.GetReportAsync();
            return ExcelHelper.GenerateExcel(data);
        }
    }
}