using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class CersaiService
    {
        private readonly CersaiRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public CersaiService(CersaiRepository repository, EmailService emailService, SmsService smsService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public async Task<IEnumerable<CersaiModel>?> GetPendingAsync(string type)
        {
            return type.ToUpper() switch
            {
                "CC" => await _repository.GetPendingCCAsync(),
                "CS" => await _repository.GetPendingCSAsync(),
                _ => throw new ArgumentException("Invalid Input", nameof(type))
            };
        }

        public async Task<int> ChargeCreationAsync(ChargeCreationDto dto)
        {
            var result = await _repository.ChargeCreationAsync(dto);
            return result;
        }

        public async Task<int> ChargeSatisfactionAsync(ChargeSatisfactionDto dto)
        {
            var result = await _repository.ChargeSatisfactionAsync(dto);
            return result;
        }
    }
}
