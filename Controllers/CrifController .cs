using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models.XmlWrapper;
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
        [Consumes("application/json")]
        [Produces("application/xml")]
        public async Task<IActionResult> GetRiskAlert([FromBody] ReqXmlWrapper request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ReqXml))
                return Content("<CIRResponse><Status>FAILED</Status><Message>Empty reqXml</Message></CIRResponse>", "application/xml");

            var responseXml = await _service.GetRiskAlertAsync(request.ReqXml);
            return Content(responseXml, "application/xml");
        }
    }
}