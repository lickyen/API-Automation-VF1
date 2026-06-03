using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Models.ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class DeathClaimService
    {
        private readonly DeathClaimRepository _repository;
        private readonly string _basePath;

        public DeathClaimService(DeathClaimRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _basePath = appSettings.FilePaths.DeathClaimFilePath;
        }

        public async Task<DeathClaimResponseDto?> GetByIdAsync(int id)
        {
            var response = new DeathClaimResponseDto();
            response.Transaction = await _repository.GetByIdAsync(id);
            response.Logs = await _repository.GetLogsAsync(id);
            return response;
        }

        public async Task<int> IntimationAsync(IntimationRequestDto dto)
        {
            string status = "New";
            var result = await _repository.IntimationAsync(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = result,
                    Status = status,
                    CreatedBy = dto.CreatedBy
                });
            }

            return result;
        }

        public async Task<int> UpdateIntimationAsync(UpdateIntimationRequestDto dto)
        {
            var result = await _repository.UpdateIntimationAsync(dto);

            return result;
        }
        public async Task<int> SubmitDocsAsync(SubmitDocsRequestDto dto)
        {
            string status = "BR_1";
            var result = await _repository.SubmitDocsAsync(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }
        public async Task<int> SubmitBranch2Async(SubmitBranch2RequestDto dto)
        {
            string status = "BR_2";

            var result = await _repository.SubmitBranch2Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitBranch3Async(SubmitBranch3RequestDto dto)
        {
            string status = "BR_3";
            var result = await _repository.SubmitBranch3Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitBranch5Async(SubmitBranch5RequestDto dto)
        {
            string status = "BR_5";
            var result = await _repository.SubmitBranch5Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops1Async(SubmitCops1RequestDto dto)
        {
            string status = "";
            if (dto.Action == "Approved")
                status = "CO_A_1";
            else if (dto.Action == "Rejected")
                status = "CO_R_1";
            else if (dto.Action == "SentBack")
                status = "CO_SB_1";

            var result = await _repository.SubmitCops1Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops2Async(SubmitCops2RequestDto dto)
        {
            string status = "";

            if (dto.Action == "Save")
                status = "";
            else if (dto.Action == "Submit")
                if (dto.AdditionalReq == "Y")
                    status = "CO_A_2";
                else if (dto.AdditionalReq == "N")
                    status = "CO_A_3";
                else if (dto.Action == "SentBack")
                    status = "CO_SB_2";

            var result = await _repository.SubmitCops2Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops3Async(SubmitCops3RequestDto dto)
        {
            string status = "";

            if (dto.Action == "Approved")
                status = "CO_A_3";
            else if (dto.Action == "SentBack")
                status = "CO_SB_3";
            
            var result = await _repository.SubmitCops3Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops4Async(SubmitCops4RequestDto dto)
        {
            string status = "";

            if (dto.ClaimStatus == "Rejected")
                status = "CO_R_4";
            else if (dto.ClaimStatus == "Received" && dto.Deficit <= 0)
                status = "CO_A_5";
            else if (dto.ClaimStatus == "Received" && dto.Deficit > 0)
                status = "CO_A_4";

            var result = await _repository.SubmitCops4Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<int> SubmitCops5Async(SubmitCops5RequestDto dto)
        {
            string status = "";

            if (dto.Action == "Approved")
                status = "CO_A_5";
            else if (dto.Action == "SentBack")
                status = "CO_SB_5";

            var result = await _repository.SubmitCops5Async(dto, status);

            if (result > 0)
            {
                await _repository.InsertLogAsync(new InsertLogRequestDto
                {
                    ReqId = dto.Id,
                    Status = status,
                    Remarks = dto.Remarks,
                    CreatedBy = dto.UpdatedBy
                });
            }

            return result;
        }

        public async Task<(byte[]? bytes, string? lan)> GetClaimFormAsync(int id)
        {
            var data = await _repository.GetClaimFormDataAsync(id);
            if (data == null) return (null, null);

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "DeathClaim", "KotakClaimForm.pdf");

            var bytes = FillClaimForm(templatePath, data);
            return (bytes, data.LAN);
        }

        public async Task<(byte[]? bytes, string? lan)> GetDischargeFormAsync(int id)
        {
            var data = await _repository.GetClaimFormDataAsync(id);
            if (data == null) return (null, null);

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "DeathClaim", "DischargeForm.pdf");

            var bytes = FillDischargeForm(templatePath, data);
            return (bytes, data.LAN);
        }

        private static string S(object? val) =>
            val == null || val is DBNull ? "" : val.ToString()!;

        private static string D(DateTime? val, string format = "dd-MM-yyyy") =>
            val.HasValue ? val.Value.ToString(format) : "";

        private static byte[] FillClaimForm(string templatePath, ClaimFormDataDto d)
        {
            using var ms = new MemoryStream();
            using (var reader = new iTextSharp.text.pdf.PdfReader(templatePath))
            using (var stamper = new iTextSharp.text.pdf.PdfStamper(reader, ms))
            {

                var f = stamper.AcroFields;
                void Set(string name, string value)
                {
                    f.SetField(name, value);
                    f.SetFieldProperty(name, "setfflags", iTextSharp.text.pdf.PdfFormField.FF_READ_ONLY, null);
                }

                Set("Policy No", S(d.Policy_Number));
                Set("Loan ID", S(d.Loan_A_C_ID));
                Set("System_Generated", S(d.System_generated_No));
                Set("Policyholders Name & Company Address", "Vistaar Financial Services Pvt Ltd");
                Set("Date of Risk Commencement", D(d.Disb_Date));
                Set("Original Sum Assured A", S(d.BLC_cover));
                Set("Original Loan Amt B", S(d.Loan_Amount));
                Set("Outstanding Loan Amount as on the Date of Death D", S(d.Claim_amount_DOD));
                Set("Recoveries made till Date of Death C", "NA");
                Set("Name of Insured Member", S(d.Full_Name));
                Set("Date of Birth", D(d.Date_of_Birth));
                Set("Address of Member with Pin Code", S(d.Address));
                Set("Contact No", S(d.Mobile));
                Set("Date of Death", D(d.Death_Date));
                Set("Cause of Death mention complete details", "Natural");
                Set("Place of Death hospital home any other place", "Home");
                Set("Name of Nominee", S(d.Nominee_Name));
                Set("Address of Nominee with pin code", S(d.Nominee_Address));
                Set("Date of Birth_2", D(d.Nominee_DOB));
                Set("Relation with Member", S(d.Nominee_Relationship));
                Set("Mobile No", S(d.Nominee_Mobile));
                Set("Name of the Account Holder", S(d.Nominee_Ac_Name));
                Set("Bank Name", S(d.Nominee_BankName));
                Set("Account No", S(d.Nominee_Ac_No));
                Set("IFSC", S(d.Nominee_IFSC));
                Set("I", S(d.Nominee_Name));
                Set("Policy No_2", S(d.Policy_Number));
                Set("Policyholders Name  Company Address_2", "Vistaar finance services pvt ltd");
                Set("Loan ID_2", S(d.Loan_A_C_ID));
                Set("System Generated No as Provided by KLI in Premium Summary to Policyholder", S(d.System_generated_No));
                Set("Name of Insured Member_2", S(d.Full_Name));
                Set("of its offices orin any court of law I", S(d.Nominee_Name));

                stamper.FormFlattening = true;
            }
            return ms.ToArray();
        }

        private static byte[] FillDischargeForm(string templatePath, ClaimFormDataDto d)
        {
            using var ms = new MemoryStream();
            using (var reader = new iTextSharp.text.pdf.PdfReader(templatePath))
            using (var stamper = new iTextSharp.text.pdf.PdfStamper(reader, ms))
            {

                var f = stamper.AcroFields;
                void Set(string name, string value)
                {
                    f.SetField(name, value);
                    f.SetFieldProperty(name, "setfflags", iTextSharp.text.pdf.PdfFormField.FF_READ_ONLY, null);
                }

                Set("Mr  Ms", S(d.Full_Name));
                Set("member name who was insured under credit policy no", S(d.Policy_Number));
                Set("nos", S(d.Loan_A_C_ID));
                Set("with", "Vistaar finance services pvt ltd");
                Set("I", S(d.Nominee_Name));
                Set("2 Over and above amount of", "0");
                Set("This is with reference to the death claim of our above mentioned customer who was covered under the Credit Policy No", S(d.Policy_Number));
                Set("MrMs", S(d.Full_Name));
                Set("residing at", S(d.Address));
                Set("Tel Nos", S(d.Mobile));
                Set("We hereby certify that the Nominee  beneficiary mentioned above who has submitted the Claim Discharge Form is registered with us as the Nominee", S(d.Loan_A_C_ID));
                Set("Dated", D(d.Disb_Date));
                Set("We hereby confirm receipt of", S(d.Claim_amount_DOD));

                stamper.FormFlattening = true;
            }
            return ms.ToArray();
        }

        public async Task<(bool Success, string? FilePath, string? Error)> UploadFileAsync(UploadFileRequestDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return (false, null, "File is empty");

            var record = await _repository.GetByIdAsync(dto.ReqId);

            if (record == null)
                return (false, null, "Record not found");

            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var fileName = $"{record.Lan}_{dto.ReqId}_{dto.FileType}{ext}";
            var fullPath = Path.Combine(_basePath, fileName);

            Directory.CreateDirectory(_basePath);
            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await dto.File.CopyToAsync(stream);

            // update filename in DB
            var updateResult = await _repository.UpdateFileNameAsync(dto.ReqId, dto.FileType, fileName, dto.CreatedBy);
            if (updateResult <= 0)
                return (false, null, "Failed to update file name in database");

            return (true, fileName, null);
        }


        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            return await FileHelper.ReadFileAsync(
                _basePath,
                fileName
            );
        }

        public async Task<byte[]?> GetReportExcelAsync()
        {
            var data = await _repository.GetReportAsync();
            return ExcelHelper.GenerateExcel(data);
        }
        public async Task<IEnumerable<DeathClaimMaster>> GetByUserAsync(GetByUserRequestDto dto)
        {
            return await _repository.GetByUserAsync(dto);
        }
    }
}