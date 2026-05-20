using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;
using System.Net.Mail;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(EmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromForm] SendEmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response(false, null, "Invalid request."));

            var attachments = new List<Attachment>();

            try
            {
                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var file in request.Attachments)
                    {
                        if (file.Length > 0)
                        {
                            var stream = new MemoryStream();
                            await file.CopyToAsync(stream);
                            stream.Position = 0;
                            attachments.Add(new Attachment(stream, file.FileName, file.ContentType));
                        }
                    }
                }

                var result = await _emailService.SendEmail(
                    request.Subject,
                    request.Body,
                    request.To,
                    request.Cc ?? new List<string>(),
                    attachments.Any() ? attachments : null,
                    request.Source
                );

                if (result == "Success")
                    return Ok(new Response(true, null, "Email sent successfully."));

                return StatusCode(500, new Response(false, null, result));
            }
            finally
            {
                foreach (var attachment in attachments)
                    attachment.Dispose();
            }
        }
    }
}