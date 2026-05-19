using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly VendorService _vendorService;

        public VendorsController(VendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpGet("{vendorId}")]
        public IActionResult GetVendor(int vendorId)
        {
            if (vendorId <= 0)
                return BadRequest(new Response(false, null, "Vendor ID is required"));

            var vendor = _vendorService.GetVendor(vendorId);

            if (vendor == null)
                return NotFound(new Response(false, null, "Vendor not found"));

            return Ok(new Response(true, vendor, "Success"));
        }

        [HttpGet]
        public IActionResult GetAllVendors()
        {
            var vendors = _vendorService.GetAllVendors() ?? new List<VendorModel>();

            return Ok(new Response(true, vendors, "Success"));
        }

        [HttpPost("Add")]
        public IActionResult AddVendor([FromBody] AddVendorDTO addVendorDTO)
        {
            int result = _vendorService.AddVendor(addVendorDTO);

            return result switch
            {
                > 0 => Ok(new Response(true, result, "Success")),
                -1 => Conflict(new Response(false, null, "Vendor already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Update")]
        public IActionResult UpdateVendor([FromBody] UpdateVendorDTO updateVendorDTO)
        {
            int result = _vendorService.UpdateVendor(updateVendorDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Vendor not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Disable")]
        public IActionResult DisableVendor([FromBody] DisableVendorDTO disableVendorDTO)
        {
            int result = _vendorService.DisableVendor(disableVendorDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Vendor not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Activate")]
        public IActionResult ActivateVendor([FromBody] ActivateVendorDTO activateVendorDTO)
        {
            int result = _vendorService.ActivateVendor(activateVendorDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Vendor not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }
    }
}