using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly VendorService _vendorService;

        public VendorController(VendorService vendorService)
        {
            _vendorService = vendorService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request,string createdBy)
        {
            var result = await _vendorService.CreateVendorAsync(request,createdBy);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    responseString = result.ResponseString
                });
            }

            return Ok(new
            {
                success = true,
                responseString = result.ResponseString,
                vendorId = result.VendorId
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetActiveVendors()
        {
            var result = await _vendorService.GetActiveVendorsAsync();

            return Ok(new
            {
                success = true,
                responseString = result.ResponseString,
                count = result.Data.Count,
                data = result.Data
            });
        }


        // GET /api/v1/vendors/{vendorId}
        [HttpGet("{vendorId:long}")]
        public async Task<IActionResult> GetVendorById(long vendorId)
        {
            var result =await _vendorService.GetVendorDetailsByIdAsync(vendorId);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    responseString = result.ResponseString
                });
            }

            return Ok(new
            {
                success = true,
                responseString = result.ResponseString,
                data = result.Data
            });
        }

        // PATCH /api/v1/vendors/{vendorId}/disable
        [HttpPatch("{vendorId:long}/disable")]
        public async Task<IActionResult> DisableVendor(long vendorId,string modifiedBy)
        {
            if (vendorId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    responseString = "Invalid vendor ID."
                });
            }

          

            var result = await _vendorService.DisableVendorAsync( vendorId,modifiedBy);

            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    responseString = result.ResponseString
                });
            }

            return Ok(new
            {
                success = true,
                responseString = result.ResponseString
            });
        }

        [HttpPost("{vendorId:long}/addresses")]
        public async Task<IActionResult> AddAddress(long vendorId, [FromBody] CreateVendorAddressRequest request, string createdBy)
        {
            var result = await _vendorService.AddAddressAsync(vendorId, request, createdBy);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    responseString = result.ResponseString
                });
            }

            return Ok(new
            {
                success = true,
                responseString = result.ResponseString,
                addressId = result.AddressId
            });
        }


    }
}
