using ProductivityApi.Repositories;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public NOCService(NOCRepository repository, EmailService emailService, SmsService smsService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

    }
}
