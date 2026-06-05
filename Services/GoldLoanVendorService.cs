using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using QRCoder;

namespace ProductivityApi.Services
{
    public class GoldLoanVendorService
    {
        private readonly GoldLoanVendorRepository _vendorRepository;
        private readonly string _filePath;
        public GoldLoanVendorService(
            GoldLoanVendorRepository vendorRepository, AppSettings appSettings)
        {
            _vendorRepository = vendorRepository;
            _filePath = appSettings.FilePaths.GoldLoanFilePath;
        }

        public GoldLoanVendorModel? GetVendor(int vendorId)
        {
            return _vendorRepository.GetVendor(vendorId);
        }

        public List<GoldLoanVendorModel> GetAllVendors()
        {
            return _vendorRepository.GetAllVendors();
        }

        public int AddVendor(AddVendorDTO addVendorDTO)
        {
            int id= _vendorRepository.AddVendor(addVendorDTO);

            if (id>0) // QR Generation
            {
                
                 string qrUrl =
               $"https://uatproductivity.vistaarfinance.com/gold-loan-enquiry?vendorCode={id}";

                using var qrGenerator = new QRCodeGenerator();

                using var qrCodeData =
                qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
                var pngQrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = pngQrCode.GetGraphic(20);
                string filePath = Path.Combine(_filePath, $"VendorId_{id}.png");
                File.WriteAllBytes(filePath, qrCodeBytes);

            }

            return id;
        }

        public int UpdateVendor(UpdateVendorDTO updateVendorDTO)
        {
            return _vendorRepository.UpdateVendor(updateVendorDTO);
        }

        public int DisableVendor(
            DisableVendorDTO disableVendorDTO)
        {
            return _vendorRepository.DisableVendor(disableVendorDTO);
        }

        public int ActivateVendor(
            ActivateVendorDTO activateVendorDTO)
        {
            return _vendorRepository.ActivateVendor(activateVendorDTO);
        }

        /* =========================================
           VALIDATE VENDOR EXISTS
           ========================================= */

        public bool VendorExists(int vendorId)
        {
            return _vendorRepository.VendorExists(vendorId);
        }

        /* =========================================
           GET QR LINK
           ========================================= */

        public string GetVendorQRCodeLink(int vendorId)
        {
            return
                $"https://uatproductivity.vistaarfinance.com/gold-loan-enquiry?vendorCode={vendorId}";
        }

        /* =========================================
           GET QR IMAGE API URL
           ========================================= */

        public string GetVendorQRCodeImageUrl(int vendorId)
        {
            return
                $"/api/GoldLoanVendor/{vendorId}/QRCode";
        }

        public async Task<(QRCodeResponseDto res, string Message)> GetQRCode(int VendorId)
        {
            var Vendor= _vendorRepository.GetVendor(VendorId);
            
            if (Vendor!= null)
            {
                var basePath = _filePath;
                var fileName = $"VendorId_{VendorId}.png";
                var outputFilePath = Path.Combine(basePath, fileName);
                if (File.Exists(outputFilePath))
                {
                   var responsedto = new QRCodeResponseDto
                    {
                        Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                        FileName = fileName
                    };
                    return (responsedto, "Success");
                }
                else
                {
                    return (null, "Document not found");
                }

            }
        
            else
            {
                return (null, "Invalid Vendor ID");
            }

           
        }
        public async Task<byte[]?> GetReportExcelAsync(GoldLeadReportFilterDto dto)
        {
            var data = await _vendorRepository.GetLegalBillingReportAsync(dto);

            return ExcelHelper.GenerateExcel(data, "Data");
        }
        public async Task<IEnumerable<dynamic>> GetReportDataAsync(GoldLeadReportFilterDto dto)
        {
            return await _vendorRepository.GetLegalBillingReportAsync(dto);
        }
    }
}