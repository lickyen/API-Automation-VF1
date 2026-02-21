using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NOCController : ControllerBase
    {
        private readonly NOCService _service;
        public NOCController(NOCService service)
        {
            _service = service;
        }
    }
}
