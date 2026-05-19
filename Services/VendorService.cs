using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class VendorService
    {
        private readonly VendorRepository _vendorRepository;

        public VendorService(VendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public VendorModel? GetVendor(int vendorId)
        {
            return _vendorRepository.GetVendor(vendorId);
        }

        public List<VendorModel> GetAllVendors()
        {
            return _vendorRepository.GetAllVendors();
        }

        public int AddVendor(AddVendorDTO addVendorDTO)
        {
            return _vendorRepository.AddVendor(addVendorDTO);
        }

        public int UpdateVendor(UpdateVendorDTO updateVendorDTO)
        {
            return _vendorRepository.UpdateVendor(updateVendorDTO);
        }

        public int DisableVendor(DisableVendorDTO disableVendorDTO)
        {
            return _vendorRepository.DisableVendor(disableVendorDTO);
        }

        public int ActivateVendor(ActivateVendorDTO activateVendorDTO)
        {
            return _vendorRepository.ActivateVendor(activateVendorDTO);
        }
    }
}