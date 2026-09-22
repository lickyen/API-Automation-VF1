using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CrifController : ControllerBase
    {
        private readonly CRIFService _service;

        public CrifController(CRIFService service)
        {
            _service = service;
        }

        [HttpPost]
        [Consumes("application/xml", "text/xml")]
        [Produces("application/xml")]
        public async Task<IActionResult> GetRiskAlert()
        {
            using var reader = new StreamReader(Request.Body);
            var requestXml = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestXml))
                return Content("<CIRResponse><Status>FAILED</Status><Message>Empty request body</Message></CIRResponse>", "application/xml");

            var responseXml = await _service.GetRiskAlertAsync(requestXml);
            return Content(responseXml, "application/xml");
        }
    }
}