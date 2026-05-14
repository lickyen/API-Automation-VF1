using DocumentFormat.OpenXml.Spreadsheet;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Constants;

namespace ProductivityApi.Services
{
    public class ClosureService
    {
        private readonly ClosureRepository _repository;

        public ClosureService(ClosureRepository repository)
        {
            _repository = repository;
        }

        public static bool IsCritical(string? action)
            => action == "Critical" || action == "High Critical";

        public static bool IsBucket(string? bucket)
            => bucket == "B1-B3" || bucket == "NPA";

        public async Task<int> AddRequestAsync(AddClosureRequestDto dto)
        {
            string status = string.Empty;

            if (IsCritical(dto.Action) || IsBucket(dto.Bucket))
            {
                if (dto.IsWaiver)
                {
                    status = ClosureStatus.Initiated;
                }
                else
                {
                    status = ClosureStatus.AutoApproved;
                }
            }
            else
            {
                status = ClosureStatus.New;
            }


            var id = await _repository.AddRequestAsync(dto, status);
            if (id > 0)
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    TotalOutstanding = dto.TotalOutstanding,
                    POS = dto.POS,
                    Interest = dto.Interest,
                    Charges = dto.Charges,
                    AmountProposed = dto.AmountProposed,
                    POSProposed = dto.POSProposed,
                    InterestProposed = dto.InterestProposed,
                    ChargesProposed = dto.ChargesProposed,
                    POSWaiverPer = dto.POSWaiverPer,
                    InterestWaiverPer = dto.InterestWaiverPer,
                    ChargesWaiverPer = dto.ChargesWaiverPer,
                    CreatedBy = dto.CreatedBy
                });
            return id;
        }

        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto)
        {
            string status = string.Empty;

            if ((IsCritical(dto.Action) || IsBucket(dto.Bucket)) && !dto.IsWaiver)
            {
                status = ClosureStatus.AutoApproved;
            }
            else
            {
                status = ClosureStatus.ReInitiated;
            }

            var id = await _repository.ReinitiateRequestAsync(dto, status);
            if (id > 0)
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    TotalOutstanding = dto.TotalOutstanding,
                    POS = dto.POS,
                    Interest = dto.Interest,
                    Charges = dto.Charges,
                    AmountProposed = dto.AmountProposed,
                    POSProposed = dto.POSProposed,
                    InterestProposed = dto.InterestProposed,
                    ChargesProposed = dto.ChargesProposed,
                    POSWaiverPer = dto.POSWaiverPer,
                    InterestWaiverPer = dto.InterestWaiverPer,
                    ChargesWaiverPer = dto.ChargesWaiverPer,
                    CreatedBy = dto.UpdatedBy
                });
            return id;
        }

        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto)
        {
            string status = ClosureStatus.AmountCollected;

            var id = await _repository.UpdateAmountCollectedAsync(dto, status);
            if (id > 0)
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            return id;
        }

        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto)
        {
            string status = ClosureStatus.ReSubmitted;

            var id = await _repository.ResubmitRequestAsync(dto, status);
            if (id > 0)
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            return id;
        }

        public async Task<int> ApproveAsync(ClosureApprovalDto dto)
        {
            var result = await _repository.ApproveAsync(dto);
            if (result > 0)
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = dto.Role,
                    Action = dto.Status,
                    Status = $"{dto.Role}-{dto.Status}",
                    Remarks = dto.Remarks,
                    TotalOutstanding = dto.TotalOutstanding,
                    POS = dto.POS,
                    Interest = dto.Interest,
                    Charges = dto.Charges,
                    AmountProposed = dto.AmountProposed,
                    POSProposed = dto.POSProposed,
                    InterestProposed = dto.InterestProposed,
                    ChargesProposed = dto.ChargesProposed,
                    POSWaiverPer = dto.POSWaiverPer,
                    InterestWaiverPer = dto.InterestWaiverPer,
                    ChargesWaiverPer = dto.ChargesWaiverPer,
                    CreatedBy = dto.UserId
                });
            return result;
        }

        public async Task<int> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto)
        {
            string status = ClosureStatus.Initiated;

            var result = await _repository.SubmitQuestionnaireAsync(dto, status);
            if (result == 0 && dto.Source == "FC")
                await _repository.AddLogAsync(new AddClosureLogDto
                {
                    ReqId = dto.Id,
                    Role = "OPS",
                    Action = status,
                    Status = status,
                    CreatedBy = dto.CreatedBy
                });
            return result;
        }

        public async Task<ClosureTransaction?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<List<ClosurePayment>> GetPaymentsAsync(int reqId)
            => await _repository.GetPaymentsAsync(reqId);

        public async Task<List<ClosureLog>> GetLogsAsync(int reqId)
            => await _repository.GetLogsAsync(reqId);

        public async Task<List<ClosureQuestionnaireResponse>> GetQuestionnaireAsync(int reqId, string reqSource)
            => await _repository.GetQuestionnaireAsync(reqId, reqSource);

        public async Task<List<ClosureTransaction>> GetListByUserAsync(string userId, string role)
            => await _repository.GetListByUserAsync(userId, role);

        public async Task<List<ClosureTransaction>> GetCompletedByUserAsync(string userId)
            => await _repository.GetCompletedByUserAsync(userId);

        public async Task<List<ClosureTransaction>> SearchByLanAsync(string lan)
            => await _repository.SearchByLanAsync(lan);

        public async Task<int> AddPaymentAsync(AddClosurePaymentDto dto)
            => await _repository.AddPaymentAsync(dto);

        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto)
            => await _repository.UpdatePaymentAsync(dto);

        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
            => await _repository.DeletePaymentAsync(id, deletedBy);

        public async Task<int> AddQuestionnaireResponseAsync(AddQuestionnaireResponseDto dto)
            => await _repository.AddQuestionnaireResponseAsync(dto);

    }
}