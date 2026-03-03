using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BranchVisitService
    {
        private readonly BranchVisitRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public BranchVisitService(BranchVisitRepository repository, EmailService emailService, SmsService smsService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public async Task<BranchVisitDto> GetVisitByIdAsync(int visitId)
        {
            return await _repository.GetVisitByIdAsync(visitId);
        }

        public async Task<List<BranchVisitChecklist>> GetChecklistAsync(string section)
        {
            return await _repository.GetChecklistAsync(section);
        }

        public async Task<List<BranchVisit>> GetVisitsByUserAsync( string userId,  string month, string? status)
        {
            return await _repository.GetVisitsByUserAsync(userId, month, status);
        }

        public async Task<int> UpdateBranchVisitAsync(BranchVisitSubmitDto dto)
        {
            var result = await _repository.UpdateBranchVisitAsync(dto);
            return result;
        }
    }
}
