using ProductivityApi.Models;
using ProductivityApi.Repositories;
using Serilog;
using System.Runtime.InteropServices;
using System.Data;
using System.Net.Mail;
using System.Text;
using ProductivityApi.Models.PLF;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Reflection;
using Azure;
using DocumentFormat.OpenXml.Wordprocessing;
using ProductivityApi.Repository;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Office2010.Excel;
using iTextSharp.text.pdf;
using System.Globalization;
using Humanizer;
using Quartz.Util;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Bibliography;

namespace ProductivityApi.Services
{
    public class SOALODFCLService
    {
        private readonly SOALODFCLRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private readonly AppSettings _appSettings;
        private readonly PLFService _plfService;
        private readonly SharedService _sharedservice;
        private readonly SharedRepository _sharedRepository;


        public SOALODFCLService(SOALODFCLRepository repository, EmailService emailService, TemplateService templateService, UserService userService, AppSettings appSettings, PLFService plfService, SharedService sharedservice, SharedRepository sharedRepository)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
            _appSettings = appSettings;
            _plfService = plfService;
            _sharedservice = sharedservice;
            _sharedRepository = sharedRepository;
        }
        public async Task<IEnumerable<SOADashboard>> GetDashboardByUserAsync(SOADashboardRequestDto dto)
        {
            return await _repository.GetDashboardByUserAsync(dto);
        }
        public async Task<SOADashboard?> GetSettlementByIdAsync(int id)
        {
            var result = await _repository.GetSettlementByIdAsync(id);
            return result;
        }
        //public async Task<SOACategoryResponseDto?> GetLoanCategoryAsync(SOACategoryRequestDto dto)
        //{
        //    var result = await _repository.GetLoanCategoryAsync(dto);
        //    return result;
        //}
        public async Task<SOABaseChargeResponseDto?> getBaseChargesAsyc(string type)
        {
            var result = await _repository.GetBaseChargesAsync(type);
            return result;
        }
        public async Task<IEnumerable<ActiverequestsResponseDto>> GetActiverequestsAsync(string Lan)
        {
            return await _repository.GetActiveRequestsByLANAsync(Lan);
        }
        public async Task<int> CancelExistingRequestAsync(CancelrequestDto dto)
        {
            var result = await _repository.CancelExistingRequestAsync(dto);
            return result;
        }
        // Create a new SOALODFCL request
        public async Task<int> CreateSOALODFCLAsync(CreateSOALODDto dto)
        {

            bool isWaiver = false;


            isWaiver = dto.charges.Any(x => x.IsWaiver == 1);


            if ((dto.Category == "Critical" || dto.Category == "High Critical" || dto.Bucket == "B1-B3" || dto.Bucket == "NPA")) //Critical / High Critical / B1-B3 / NPA
            {
                if (!isWaiver) //No Waiver
                {
                    dto.status = "Auto-Approved";
                }
                else // Waiver
                {
                    dto.status = "Initiated";
                }

            }
            else if (dto.SOA == 1 && dto.LOD == 0 && dto.FCL == 0) // ONLY SOA Selected
            {
                dto.status = "Auto-Approved";
            }

            else if (dto.FCL == 1 || dto.LOD == 1)  //FCL or LOD Selected
            {
                dto.status = "New";
            }

            else //default
            {
                dto.status = "Initiated";
            }

            var result = await _repository.CreateSOALODFCLAsync(dto);

            return result;
        }

        public async Task<int> AddChargesAsync(AddChargesDto dto)
        {
            var result = await _repository.AddChargesAsync(dto);

            return result;
        }

        public async Task<int> ApproveSOALODAsync(ApproveSOALODDto dto)
        {
            var result = await _repository.ApproveSOALODAsync(dto);

            return result;
        }
        public async Task<int> CheckIfApproved(int id)
        {
            var result = await _repository.CheckIfApproved(id);

            return result;
        }

        public async Task<int> NOCSubmitAsync(SOALODNOCSubmitDto dto)
        {
            var basePath = _appSettings.FilePaths.SOALODFilePath;

            // var record = await _repository.GetByLanAsync(dto.Lan);
            //if (record == null) return -1;

            string? LODFilePath = "";
            int returnId;


            if (!string.IsNullOrWhiteSpace(basePath))
            {
                Directory.CreateDirectory(basePath);

                // Bank Proof
                if (dto.LODDoc != null && !string.IsNullOrEmpty(dto.LODDoc.Base64))
                {
                    var bankProofExt = Path.GetExtension(dto.LODDoc.FileName);
                    var bankProofName = $"{dto.Id}_NOCLOD{bankProofExt}";

                    await File.WriteAllBytesAsync(
                        Path.Combine(basePath, bankProofName),
                        Convert.FromBase64String(dto.LODDoc.Base64)
                    );

                    LODFilePath = bankProofName;
                }


            }

            returnId = await _repository.NOCSubmitAsync(dto, LODFilePath);
            if (returnId > 0)
            {

            }
            return returnId;
        }
        public async Task<TransferLetterResponse?> GetSOA(GenerateStatementRequest request, int Id)
        {
            var result = await _plfService.GenerateStatement(request);
            if (result == null || result?.ReturnStatus?.returnCode != "0000")
            {
                throw new Exception("LAN not found or InActive");
            }
            var letterId = result.LetterID;
            if (letterId.ToString() == null || letterId == 0)
            {
                throw new Exception("API Not working");
            }
            TransferLetterRequest data = new TransferLetterRequest();
            data.ServiceName = "Annual Loan Statement";
            data.RequestID = letterId.ToString();
            var SOA = await _plfService.TransferLetter(data);

            // save SOA to Local Path

            var basePath = _appSettings.FilePaths.SOALODFilePath;
            var fileName = "";
            if (!string.IsNullOrWhiteSpace(SOA.DocContent) && !string.IsNullOrEmpty(SOA.DocContent))
            {
                Directory.CreateDirectory(basePath);
                fileName = $"{Id}_SOA.pdf";

                await File.WriteAllBytesAsync(
                    Path.Combine(basePath, fileName),
                    Convert.FromBase64String(SOA.DocContent)
                );
            }

            var db = _repository.SaveSOALODFCLDocsAsync(Id, "SOA", fileName);

            return SOA;
        }
        public async Task<PaymentLinkResponseDto> GeneratePaymentLink(GenPaymentLinkDto dto)
        {
            PaymentLinkResponseDto response = new PaymentLinkResponseDto();
            var soalod = await _repository.GetSettlementByIdAsync(dto.Id);
            if (soalod != null)
            {
                string Refid = await _sharedRepository.GenerateReferenceNoAsync("CHG");

                long expireBy = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
                string description = $"{soalod.Lan} - Payment for SOA/LOD/FCL";
                int amount = Convert.ToInt16(soalod.Total_Charges);
                var request = new PaymentLinkRequestDto
                {
                    Amount = (int)(amount * 100), // Razorpay accepts amount in paise
                    Currency = "INR",
                    AcceptPartial = false,
                    ExpireBy = expireBy,
                    ReferenceId = Refid,
                    Description = description,

                    Customer = new PaymentCustomerDto
                    {
                        Name = soalod.Cust_Name,
                        Contact = soalod.Mobile,
                        Email = soalod.Email
                    },

                    Notify = new NotifyDto
                    {
                        Sms = true,
                        Email = true
                    },

                    ReminderEnable = true,

                    Notes = new NotesDto
                    {
                        PolicyName = "SOA/LOD/FCL"
                    },

                    CallbackUrl = "https://vistaarfinance.com/",
                    CallbackMethod = "get"
                };

                response = await _sharedservice.PaymentLink(request);
                if (response.Error == null)
                //&& !string.IsNullOrEmpty(response.ReferenceId))
                {
                    int Pid = await _repository.AddPayment_Online(response, dto.Id, soalod.Lan, dto.UserId);
                }
            }
            return response;
        }
        public async Task<IEnumerable<PaymentLinkTransaction>> GetPaymentLinkstatus(int Id)
        {
            return await _repository.GetPaylinkdetails(Id);

        }
        public async Task<int> AddCashPayment(SOALODCashDto dto)
        {
            var result = await _repository.AddPayment_cash(dto);

            return result;
        }

        public async Task<PaymentLinkTransaction> UpdatePaymenttoPLF(GenPaymentLinkDto dto)
        {
            bool Chargestatus = false;
            var soalod = await _repository.GetSettlementByIdAsync(dto.Id);

            var chargestatus = await CreateAdviceinPLF(dto);
            if (chargestatus)
            {
                // PaymentLinkTransaction pl = new PaymentLinkTransaction();
                var pl = (await _repository.GetPaylinkdetails(dto.Id)).FirstOrDefault();
                if (pl is not null)
                {
                    if (pl.Payment_Link_Status == "paid")
                    {
                        if (string.IsNullOrEmpty(pl.Pennant_Response) || pl.Pennant_Response != "success")
                        {
                            bool isCash = pl.Payment_Type.ToLower() == "cash";
                            var request = new PaymentRequest
                            {

                                finReference = pl.Lan,
                                amount = pl.Amount * 100,
                                paymentMode = pl.Payment_Type.ToUpper(),
                                subReceiptMode = isCash ? "CASH" : "PAYMENTGATEWAY",
                                allocationType = "A",
                                excessAdjustTo = "A",
                                panNumber = "",
                                reqType = "Post",
                                stpProcess = true,
                                processStage = "",
                                receiptDetail = new ReceiptDetail
                                {
                                    paymentRef = isCash ? "" : pl.Payment_Id,
                                    bankCode = "",
                                    favourName = "",
                                    favourNumber = "",
                                    fundingAccount = "223",
                                    transactionRef = pl.Reference_Id,
                                    depositDate = pl.UpdatedDate,
                                    receivedDate = pl.UpdatedDate,
                                    remarks = ""
                                },
                                fees = new List<Fee>()
                            };
                            var plfResponse = await _plfService.ManualPayment(request);
                            await _repository.UpdatePLFPaymentResponse(dto.Id, plfResponse, pl.Reference_Id, dto.UserId);



                        }

                    }
                }
            }
            else
            {

            }
            var Paymentdtl = (await _repository.GetPaylinkdetails(dto.Id)).FirstOrDefault();
            return Paymentdtl;
        }

        public async Task<Boolean> CreateAdviceinPLF(GenPaymentLinkDto dto)
        {
            bool Chargestatus = false;
            var soalod = await _repository.GetSettlementByIdAsync(dto.Id);
            var Charges = await _repository.GetChargesById(dto.Id);
            CreateAdviceResponse? adviseResponse = null;
            int adviseid;
            foreach (var charge in Charges)
            {
                if (charge.Charge_Amount > 0 && string.IsNullOrEmpty(charge.Charge_Id))
                {
                    // Your logic here
                    var request = new CreateAdviceRequest
                    {
                        stp = true,
                        finReference = soalod.Lan,
                        adviseType = 1,
                        valueDate = DateTime.Today,
                        feeTypeCode = charge.Charge_Code,
                        adviseAmount = charge.Charge_Amount * 100  //minor currency
                    };
                    adviseResponse = await _plfService.CreateAdvice(request);
                    if (adviseResponse.returnStatus is not null)
                    {
                        var returnstatus = adviseResponse.returnStatus.returnCode + "-" + adviseResponse.returnStatus.returnText;
                        adviseid = await _repository.UpdateAdviseID(dto.Id, charge.Charge_Code, "", returnstatus, dto.UserId);
                    }
                    else
                    {
                        adviseid = await _repository.UpdateAdviseID(dto.Id, charge.Charge_Code, adviseResponse.adviseId, adviseResponse.adviseId, dto.UserId);
                    }
                }
            }
            var Charges1 = await _repository.GetChargesById(dto.Id);

            foreach (var charge1 in Charges)
            {
                if (charge1.Charge_Amount > 0 && string.IsNullOrEmpty(charge1.Charge_Id))
                {
                    Chargestatus = false;
                }
                else
                {
                    Chargestatus = true;
                }
            }
            return Chargestatus;
        }

        public async Task<SOALODFileResponseDto?> GenerateReceipt(SOALODReceiptRequestDto dto)
        {
            var soalod = await _repository.GetSettlementByIdAsync(dto.Id);
            var basePath = _appSettings.FilePaths.SOALODFilePath;
            SOALODFileResponseDto responsedto = null;
            try
            {
                if (!dto.ReceiptNo.IsNullOrWhiteSpace())
                {


                    var TemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "PaymentReceipt.pdf");
                    if (!File.Exists(TemplatePath))
                        throw new FileNotFoundException($"template file not found: {TemplatePath}");

                    var outputFileName = $"{soalod.Lan}_{dto.Id}_Receipt.pdf";
                    var outputFilePath = Path.Combine(basePath, outputFileName);

                    if (File.Exists(outputFilePath))
                    {
                        responsedto = new SOALODFileResponseDto
                        {
                            Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                            FileName = outputFileName
                        };

                    }
                    else
                    {

                        string today = DateTime.Today.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                        string time = DateTime.Now.ToString("hh:mm tt");

                        int integerPart = (int)Math.Floor(Convert.ToDecimal(soalod.Total_Charges));
                        string amountInWords = integerPart.ToWords().Transform(To.TitleCase) + " Rupees";

                        // --- Using blocks for PDF handling ---
                        using (PdfReader reader = new PdfReader(TemplatePath))
                        {
                            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                            {
                                using (PdfStamper stamper = new PdfStamper(reader, fs))
                                {
                                    var fields = stamper.AcroFields;

                                    fields.SetField("text_Date", today);
                                    fields.SetFieldProperty("text_Date", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    fields.SetField("text_Time", time);
                                    fields.SetFieldProperty("text_Time", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    fields.SetField("text_No", dto.ReceiptNo);
                                    fields.SetFieldProperty("text_No", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    fields.SetField("text_Customer Name", soalod.Cust_Name);
                                    fields.SetFieldProperty("text_Customer", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    fields.SetField("text_Rupees in words", amountInWords);
                                    fields.SetFieldProperty("text_Rupees in words", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    fields.SetField("text_Rupees", soalod.Total_Charges.ToString());
                                    fields.SetFieldProperty("text_Rupees", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                    stamper.FormFlattening = true; // Make fields read-only
                                }
                            }
                        }
                        var id = await _repository.UpdateReceiptPath(dto.Id, outputFileName, dto.UpdatedBy);
                        if (File.Exists(outputFilePath))
                        {
                            //return (false, "File not found on server", null);

                            responsedto = new SOALODFileResponseDto
                            {
                                Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                                FileName = outputFileName
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine(ex.Message);
            }
            return responsedto;
        }

        public async Task<(SOALODFileResponseDto res, string Message)> GetLod(LODRequestDto req)
        {
            var soalod = await _repository.GetSettlementByIdAsync(req.Id);
            LODResponse lodResponse=null;
            SOALODFileResponseDto responsedto = null;
            if (!soalod.NOCLOD_FileName.IsNullOrWhiteSpace())
            {
                var basePath = _appSettings.FilePaths.SOALODFilePath;
                var fileName = soalod.NOCLOD_FileName;
                var outputFilePath = Path.Combine(basePath, fileName);
                if (File.Exists(outputFilePath))
                {
                    responsedto = new SOALODFileResponseDto
                    {
                        Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                        FileName = fileName
                    };
                }
                return (responsedto ,"Success");
            }
            else if (!soalod.LOD_FileName.IsNullOrWhiteSpace())
            {
                var basePath = _appSettings.FilePaths.SOALODFilePath;
                var fileName = soalod.LOD_FileName;
                var outputFilePath = Path.Combine(basePath, fileName);
                if (File.Exists(outputFilePath))
                {
                    responsedto = new SOALODFileResponseDto
                    {
                        Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                        FileName = fileName
                    };
                }
                return (responsedto, "Success");

            }

            else{
                    LODRequest lr = new LODRequest();
                    lr.finReference = soalod.Lan;
                    lodResponse = await _plfService.GetLOD(lr);
                    if (lodResponse.returnStatus.returnCode == "0000")
                    {
                        //SAVE lod
                        var basePath = _appSettings.FilePaths.SOALODFilePath;
                        var fileName = "";
                        if (!string.IsNullOrWhiteSpace(lodResponse.data) && !string.IsNullOrEmpty(lodResponse.data))
                        {
                            Directory.CreateDirectory(basePath);
                            fileName = $"{req.Id}_LOD.pdf";

                            await File.WriteAllBytesAsync(
                                Path.Combine(basePath, fileName),
                                Convert.FromBase64String(lodResponse.data)
                            );
                        }

                        var db = _repository.SaveSOALODFCLDocsAsync(req.Id, "LOD", fileName);

                        var outputFilePath = Path.Combine(basePath, fileName);
                        if (File.Exists(outputFilePath))
                        {
                            responsedto = new SOALODFileResponseDto
                            {
                                Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                                FileName = fileName
                            };
                        }
                        return (responsedto, "Success");
                    }
                    else
                    {
                        return (responsedto, lodResponse.returnStatus.returnCode + "-" + lodResponse.returnStatus.returnText);
                    }
                
            }
            
           // return responsedto;
        }

        public async Task<(SOALODFileResponseDto res, string Message)> GetFCL(LODRequestDto req)
        {
            var soalod = await _repository.GetSettlementByIdAsync(req.Id);
            LODResponse lodResponse = null;
            SOALODFileResponseDto responsedto = null;
            if (!soalod.FCL_FileName.IsNullOrWhiteSpace())
            {
                var basePath = _appSettings.FilePaths.SOALODFilePath;
                var fileName = soalod.FCL_FileName;
                var outputFilePath = Path.Combine(basePath, fileName);
                if (File.Exists(outputFilePath))
                {
                    responsedto = new SOALODFileResponseDto
                    {
                        Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                        FileName = fileName
                    };
                }
                return (responsedto, "Success");
            }
            else
            {
                var (report, msg) = await _sharedservice.GetForeclosureReportAsync(soalod.Lan);
                if (report == null)
                {
                    return (null, msg);

                }
                else
                {


                    var basePath = _appSettings.FilePaths.SOALODFilePath;
                    var TemplatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "ForeclosureLetter.pdf");
                    if (!File.Exists(TemplatePath))
                        throw new FileNotFoundException($"template file not found: {TemplatePath}");

                    var outputFileName = $"{soalod.Id}_FCL.pdf";
                    var outputFilePath = Path.Combine(basePath, outputFileName);

                    string today = DateTime.Today.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
                    using (PdfReader reader = new PdfReader(TemplatePath))
                    {
                        using (FileStream fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                        {
                            using (PdfStamper stamper = new PdfStamper(reader, fs))
                            {
                                var fields = stamper.AcroFields;

                                fields.SetField("Name", report.CustName);
                                fields.SetFieldProperty("Name", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Loan Account No", report.FinReference);
                                fields.SetFieldProperty("Loan Account No", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Branch Name", soalod.Branch_Name);
                                fields.SetFieldProperty("Branch Name", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("LAN Name", report.FinReference);
                                fields.SetFieldProperty("LAN", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Date", today);
                                fields.SetFieldProperty("Date", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Financed Amount", report.FinCurrAssetValue.ToString());
                                fields.SetFieldProperty("Financed Amount", "setfflags", PdfFormField.FF_READ_ONLY, null);



                                fields.SetField("Customer No/Name", report.CustName + "/" + report.CustCIF);
                                fields.SetFieldProperty("Customer No/Name", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Principal Outstanding", report.OutstandingPri.ToString());
                                fields.SetFieldProperty("Principal Outstanding", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Principal Due", report.PrincipalAmt.ToString());
                                fields.SetFieldProperty("Principal Due", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Interest Due", report.InterestAmt.ToString());
                                fields.SetFieldProperty("Interest Due", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("GST", report.ForeClosFees.ToString());
                                fields.SetFieldProperty("GST", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("BPI", report.InstForTheMonth.ToString());
                                fields.SetFieldProperty("BPI", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("LPI", report.LatePayInterestAmt.ToString());
                                fields.SetFieldProperty("LPI", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Other Charges", report.ChargesIncGST.ToString());
                                fields.SetFieldProperty("GST", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Excess Amount", report.TotalRefunds.ToString());
                                fields.SetFieldProperty("Excess Amount", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("Grand Total", report.NetReceivable.ToString());
                                fields.SetFieldProperty("Grand Total", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                fields.SetField("BPI Per Day", report.IntPerDay.ToString());
                                fields.SetFieldProperty("BPI Per Day", "setfflags", PdfFormField.FF_READ_ONLY, null);

                                stamper.FormFlattening = true; // Make fields read-only
                            }
                        }
                    }
                    var db = _repository.SaveSOALODFCLDocsAsync(req.Id, "FCL", outputFileName);
                    if (File.Exists(outputFilePath))
                    {
                        //return (false, "File not found on server", null);

                        responsedto = new SOALODFileResponseDto
                        {
                            Base64 = Convert.ToBase64String(File.ReadAllBytes(outputFilePath)),
                            FileName = outputFileName
                        };
                    }


                    return (responsedto, "Success");
                }
            }
        }

    }
}
