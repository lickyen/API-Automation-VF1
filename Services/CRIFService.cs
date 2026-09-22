using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using ProductivityApi.Models.XmlWrapper;

using ProductivityApi.Repositories;

namespace ProductivityApi.Services
{
    public class CRIFService
    {
        private readonly HttpClient _httpClient;
        private readonly CrifWrapperRepository _repository;
        private readonly IConfiguration _configuration;

        public CRIFService(HttpClient httpClient, CrifWrapperRepository repository, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<string> GetRiskAlertAsync(string requestXml)
        {
            LegacyApplicant applicant;
            LegacyApplicationSegment appSegment;
            string responseXml;

            try
            {
                requestXml = SanitizeXml(requestXml);
                var legacyRequest = DeserializeXml<CrifLegacyXmlRequest>(requestXml);
                appSegment = legacyRequest.Inquiry.ApplicationSegment;
                applicant = appSegment.Applicants.FirstOrDefault()
                            ?? throw new Exception("No APPLICANT found in request XML");
            }
            catch (Exception ex)
            {
                responseXml = BuildErrorXml($"Invalid XML request: {ex.Message}");
                _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, ex.Message);
                return responseXml;
            }

            try
            {
                var crifRequest = MapToCrifRequest(applicant, appSegment);
                var jsonBody = JsonSerializer.Serialize(crifRequest);

                var crifUrl = _configuration["CrifApi:BaseUrl"];
                var request = new HttpRequestMessage(HttpMethod.Post, crifUrl)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "text/xml") // CRIF expects Content-Type text/xml even though body is JSON
                };
                request.Headers.Add("userId", _configuration["CrifApi:UserId"]);
                request.Headers.Add("password", _configuration["CrifApi:Password"]);
                request.Headers.Add("CUSTOMER-ID", _configuration["CrifApi:CustomerId"]);
                request.Headers.Add("PRODUCT-TYPE", _configuration["CrifApi:ProductType"]);
                request.Headers.Add("PRODUCT-VER", _configuration["CrifApi:ProductVer"]);
                request.Headers.Add("REQ-VOL-TYPE", _configuration["CrifApi:ReqVolType"]);

                var httpResponse = await _httpClient.SendAsync(request);
                var rawResponseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    responseXml = BuildErrorXml($"CRIF returned HTTP {(int)httpResponse.StatusCode}: {rawResponseBody}");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, rawResponseBody);
                    return responseXml;
                }

                CrifReportEnvelope envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<CrifReportEnvelope>(rawResponseBody);
                }
                catch (Exception ex)
                {
                    responseXml = BuildErrorXml($"Could not parse CRIF response: {ex.Message}");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, rawResponseBody);
                    return responseXml;
                }

                var reportFile = envelope?.ReportFile;

                if (reportFile?.Errors != null && reportFile.Errors.Count > 0)
                {
                    responseXml = BuildCrifErrorXml(reportFile.Errors);
                    var errorSummary = string.Join("; ", reportFile.Errors.Select(e => $"{e.ErrorCode}: {e.ErrorDesc?.Trim()}"));
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, errorSummary);
                    return responseXml;
                }

                if (reportFile?.HeaderSegment == null)
                {
                    responseXml = BuildErrorXml("Unrecognized CRIF response shape");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, rawResponseBody);
                    return responseXml;
                }

                responseXml = BuildSuccessXml(reportFile);
                _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, true, null);
                return responseXml;
            }
            catch (Exception ex)
            {
                responseXml = BuildErrorXml($"Unexpected error calling CRIF: {ex.Message}");
                _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, ex.Message);
                return responseXml;
            }
        }
        
        private CrifJsonRequest MapToCrifRequest(LegacyApplicant applicant, LegacyApplicationSegment appSegment)
        {
            var (firstName, lastName) = SplitName(applicant.ApplicantName?.Name1);

            return new CrifJsonRequest
            {
                RequestFile = new CrifRequestFileDto
                {
                    HeaderSegment = new CrifHeaderSegmentDto
                    {
                        ProductType = _configuration["CrifApi:ProductType"],
                        ProductVer = _configuration["CrifApi:ProductVer"],
                        UserId = _configuration["CrifApi:HeaderUserId"],
                        UserPwd = _configuration["CrifApi:HeaderUserPwd"],
                        ReqMbr = _configuration["CrifApi:ReqMbr"],
                        InqDtTm = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        ReqVolType = _configuration["CrifApi:ReqVolType"],
                        ReqActnType = _configuration["CrifApi:ReqActnType"],
                        AuthFlg = "Y",
                        AuthTitle = "USER",
                        ResFrmt = "html",
                        MemberPrefOverride = "N",
                        ResFrmtEmbd = "Y",
                        LosName = "INHOUSE",
                        LosVendor = "",
                        LosVersion = "",
                        ReqServicesType = "ONLY_RISK_ALERT_VARIATION"
                    },
                    Inquiry = new CrifInquiryDto
                    {
                        ApplicantSegment = new CrifApplicantSegmentDto
                        {
                            ApplicantId = applicant.ApplicantId,
                            FirstName = firstName,
                            MiddleName = "",
                            LastName = lastName,
                            Gender = applicant.Gender,
                            AccountNumber = applicant.AccVerification?.Verification?.AcctNumber ?? "",
                            Dob = new CrifDobDto
                            {
                                DobDt = applicant.Dob?.DobDate,
                                Age = applicant.Dob?.Age,
                                AgeAsOn = applicant.Dob?.AgeAsOn
                            },
                            Relations = applicant.Relations.Select(r => new CrifRelationDto
                            {
                                Type = r.RelationType,
                                Value = r.RelativeName
                            }).ToList(),
                            Ids = applicant.Ids.Select(i => new CrifIdDto
                            {
                                Type = i.Type,
                                Value = i.Value
                            }).ToList(),
                            Addresses = applicant.Addresses.Select(a => new CrifAddressDto
                            {
                                Type = a.AddressType,
                                AddressText = string.Join(" ", new[] { a.Line1, a.Line2, a.Line3, a.Line4, a.Line5 }
                                    .Where(l => !string.IsNullOrWhiteSpace(l))),
                                City = a.City,
                                State = a.State,
                                Locality = "",
                                Pin = a.Pin,
                                Country = a.Country
                            }).ToList(),
                            Phones = applicant.Phones.Select(p => new CrifPhoneDto
                            {
                                Type = p.TeleNoType,
                                Value = p.TeleNo
                            }).ToList(),
                            Emails = applicant.Emails.Select(e => new CrifEmailDto
                            {
                                Email = e.EmailId
                            }).ToList()
                        },
                        ApplicationSegment = new CrifApplicationSegmentDto
                        {
                            InquiryUniqueRefNo = appSegment.ApplnId,
                            ApplicationId = appSegment.ApplnId,
                            BranchId = !string.IsNullOrWhiteSpace(appSegment.BranchId) ? appSegment.BranchId : appSegment.BranchRegion,
                            AcntOpenDt = appSegment.AcctOpenDate,
                            LoanAmt = appSegment.ApplnValue,
                            Ltv = appSegment.Ltv,
                            Term = appSegment.Term,
                            LoanType = appSegment.ApplnType,
                            LoanTypeDesc = appSegment.OtherDesc,
                            CreditRptTrnDtTm = DateTime.Now.ToString("HH:mm"),
                            CreditInqPurpsType = _configuration["CrifApi:DefaultCreditInqPurpsType"],
                            CreditInquiryStage = _configuration["CrifApi:DefaultCreditInquiryStage"],
                            ClientContributorId = _configuration["CrifApi:ClientContributorId"]
                        }
                    }
                }
            };
        }

        private static (string firstName, string lastName) SplitName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return ("", "");
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.FirstOrDefault() ?? "";
            var last = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
            return (first, last);
        }

        private static T DeserializeXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader);
        }
        private static string BuildCrifErrorXml(List<CrifErrorDto> errors)
        {
            var sb = new StringBuilder();
            sb.Append("<CIRResponse>");
            sb.Append("<Status>FAILED</Status>");
            sb.Append("<Errors>");
            foreach (var err in errors)
            {
                sb.Append("<Error>");
                sb.Append($"<Code>{Escape(err.ErrorCode)}</Code>");
                sb.Append($"<Description>{Escape(err.ErrorDesc?.Trim())}</Description>");
                sb.Append("</Error>");
            }
            sb.Append("</Errors>");
            sb.Append("</CIRResponse>");
            return sb.ToString();
        }
        private static string BuildErrorXml(string message)
        {
            return $@"<CIRResponse>
  <Status>FAILED</Status>
  <Message>{System.Security.SecurityElement.Escape(message)}</Message>
</CIRResponse>";
        }
        private static string BuildSuccessXml(CrifReportFileDto reportFile)
        {
            var header = reportFile.HeaderSegment;
            var cirStatus = reportFile.RequestStatus?.FirstOrDefault(s => s.Type == "CIR")?.Status ?? header?.Status;

            var sb = new StringBuilder();
            sb.Append("<CIRResponse>");
            sb.Append($"<Status>{Escape(cirStatus)}</Status>");
            sb.Append($"<ReportId>{Escape(header?.ReportId)}</ReportId>");
            sb.Append($"<BatchId>{Escape(header?.BatchId)}</BatchId>");
            sb.Append($"<DateOfRequest>{Escape(header?.DateOfRequest)}</DateOfRequest>");
            sb.Append($"<DateOfIssue>{Escape(header?.DateOfIssue)}</DateOfIssue>");

            sb.Append("<Alerts>");
            foreach (var alert in reportFile.ReportData?.Alerts ?? new List<CrifAlertDto>())
            {
                foreach (var rule in alert.Rules ?? new List<CrifRuleDto>())
                {
                    sb.Append("<Alert>");
                    sb.Append($"<Category>{Escape(rule.RuleCategory)}</Category>");
                    sb.Append($"<Code>{Escape(rule.RuleCode)}</Code>");
                    sb.Append($"<Description>{Escape(rule.RuleDescription)}</Description>");
                    sb.Append("</Alert>");
                }
            }
            sb.Append("</Alerts>");

            sb.Append($"<PrintableReport><![CDATA[{reportFile.PrintableReport?.Content}]]></PrintableReport>");
            sb.Append("</CIRResponse>");
            return sb.ToString();
        }

        private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? "";
        private static string SanitizeXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return xml;

            xml = xml.TrimStart('\uFEFF', '\u200B', '\u200C', '\u200D');
            var firstTag = xml.IndexOf('<');
            if (firstTag > 0)
                xml = xml.Substring(firstTag);

            return xml.Trim();
        }
    }
}