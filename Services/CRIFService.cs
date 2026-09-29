using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            LegacyHeaderSegment headerSegment;
            LegacyApplicant applicant;
            LegacyApplicationSegment appSegment;
            string responseXml;

            try
            {
                requestXml = TrimNoise(requestXml);
                requestXml = EscapeStrayAmpersands(requestXml);
                requestXml = SanitizeXml(requestXml);
                var legacyRequest = DeserializeXml<CrifLegacyXmlRequest>(requestXml);
                headerSegment = legacyRequest.HeaderSegment;
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

            string jsonBody = null;
            string requestHeadersJson = null;

            try
            {
                var crifRequest = MapToCrifRequest(headerSegment, applicant, appSegment);
                jsonBody = JsonSerializer.Serialize(crifRequest);
                Console.WriteLine("CRIF Request JSON: " + jsonBody);

                var crifUrl = _configuration["CrifApi:BaseUrl"];
                var request = new HttpRequestMessage(HttpMethod.Post, crifUrl)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "text/xml")
                };
                request.Headers.Add("userId", _configuration["CrifApi:UserId"]);
                request.Headers.Add("password", _configuration["CrifApi:Password"]);
                request.Headers.Add("CUSTOMER-ID", _configuration["CrifApi:CustomerId"]);
                request.Headers.Add("PRODUCT-TYPE", _configuration["CrifApi:ProductType"]);
                request.Headers.Add("PRODUCT-VER", _configuration["CrifApi:ProductVer"]);
                request.Headers.Add("REQ-VOL-TYPE", _configuration["CrifApi:ReqVolType"]);

                requestHeadersJson = JsonSerializer.Serialize(
                    request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)));

                var httpResponse = await _httpClient.SendAsync(request);
                var rawResponseBody = await httpResponse.Content.ReadAsStringAsync();
               // Console.WriteLine("CRIF Response JSON: " + rawResponseBody);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    responseXml = BuildErrorXml($"CRIF returned {(int)httpResponse.StatusCode} {httpResponse.StatusCode}: {rawResponseBody}");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, rawResponseBody,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                if (string.IsNullOrWhiteSpace(rawResponseBody))
                {
                    responseXml = BuildErrorXml("CRIF returned an empty response body");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, "Empty body from CRIF",
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                var trimmedBody = TrimNoise(rawResponseBody);

                // CRIF returns native XML for SHERLOCK / SHERLOCK-HTML response formats — pass it straight through
                // (this already includes PRINTABLE-RPRT natively, so it is NOT stripped or modified here).
                if (trimmedBody.StartsWith("<"))
                {
                    responseXml = Regex.Replace(
                        trimmedBody,
                        @"<PRINTABLE-RPRT>\s*<!\[CDATA\[",
                        "<PRINTABLE-RPRT><![CDATA[");

                    responseXml = Regex.Replace(
                        responseXml,
                        @"\s*\]\]>\s*</PRINTABLE-RPRT>",
                        "]]></PRINTABLE-RPRT>");

                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, true, null,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                // Otherwise this is a JSON response — could be CIR-REPORT-FILE (success/business-error)
                // or ERROR-FILE (technical error).
                CrifJsonEnvelope envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize<CrifJsonEnvelope>(trimmedBody);
                }
                catch (JsonException jex)
                {
                    responseXml = BuildErrorXml($"Could not parse CRIF response: {jex.Message}");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, trimmedBody,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                // Technical-error shape: {"ERROR-FILE": {...}}
                var techInquiry = envelope?.ErrorFile?.InquiryStatus?.FirstOrDefault()?.Inquiry;
                if (techInquiry != null)
                {
                    responseXml = BuildTechnicalErrorXml(techInquiry);
                    var techErrorSummary = string.Join("; ", (techInquiry.Errors ?? new List<CrifTechErrorWrapperDto>())
                        .Select(w => $"{w.Error?.Code}: {w.Error?.Description}"));
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, techErrorSummary,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                var reportFile = envelope?.ReportFile;

                if (reportFile?.Errors != null && reportFile.Errors.Count > 0)
                {
                    responseXml = BuildCrifErrorXml(reportFile.Errors);
                    var errorSummary = string.Join("; ", reportFile.Errors.Select(e => $"{e.ErrorCode}: {e.ErrorDesc?.Trim()}"));
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, errorSummary,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }

                if (reportFile?.HeaderSegment == null)
                {
                    responseXml = BuildErrorXml("Unrecognized CRIF response shape");
                    _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, rawResponseBody,
                        requestHeadersJson, jsonBody, rawResponseBody);
                    return responseXml;
                }
                Console.WriteLine("DEBUG trimmedBody.Length = " + trimmedBody.Length);
                var alertsIdx = trimmedBody.IndexOf("\"ALERTS\"", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine("DEBUG ALERTS index = " + alertsIdx);
                Console.WriteLine("DEBUG ALERTS snippet: " + (alertsIdx >= 0 ? trimmedBody.Substring(alertsIdx, Math.Min(300, trimmedBody.Length - alertsIdx)) : "NOT FOUND IN trimmedBody"));

                


                responseXml = BuildSuccessXml(reportFile);
                _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, true, null,
                    requestHeadersJson, jsonBody, rawResponseBody);
                return responseXml;
            }
            catch (Exception ex)
            {
                responseXml = BuildErrorXml($"Unexpected error calling CRIF: {ex.Message}");
                _repository.LogXmlTransaction("CrifRiskAlert", requestXml, responseXml, false, ex.Message,
                    requestHeadersJson, jsonBody, null);
                return responseXml;
            }
        }

        private CrifJsonRequest MapToCrifRequest(LegacyHeaderSegment headerSegment, LegacyApplicant applicant, LegacyApplicationSegment appSegment)
        {
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
                        InqDtTm = !string.IsNullOrWhiteSpace(headerSegment?.DateOfRequest)
                            ? headerSegment.DateOfRequest
                            : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
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
                            FirstName = applicant.ApplicantName?.Name1 ?? "",
                            MiddleName = applicant.ApplicantName?.Name2 ?? "",
                            LastName = applicant.ApplicantName?.Name3 ?? "",
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
                                Type = r.RelationType ?? "",
                                Value = r.RelativeName ?? ""
                            }).ToList(),
                            Ids = applicant.Ids.Select(i => new CrifIdDto
                            {
                                Type = i.Type,
                                Value = i.Value
                            }).ToList(),
                            Addresses = applicant.Addresses.Select(a => new CrifAddressDto
                            {
                                Type = a.AddressType,
                                AddressText = a.Line1 ?? "",
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
                            BranchId = appSegment.BranchId ?? "",
                            AcntOpenDt = appSegment.AcctOpenDate,
                            LoanAmt = appSegment.ApplnValue,
                            Ltv = appSegment.Ltv,
                            Term = appSegment.Term,
                            LoanType = "A12",                  // TODO: hardcoded for now — replace with a lookup once loan-type codes are confirmed
                            LoanTypeDesc = "Personal Loan",    // TODO: hardcoded for now — same as above
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
      
        private static string BuildSuccessXml(CrifReportFileDto reportFile)
        {
            var header = reportFile?.HeaderSegment;
            var requestData = reportFile?.RequestData;
            var alerts = reportFile?.ReportData?.Alerts ?? new List<CrifAlertDto>();
            Console.WriteLine($"DEBUG: alerts.Count = {alerts.Count}, ReportData null? {reportFile?.ReportData == null}, StandardData null? {reportFile?.ReportData?.StandardData == null}");
            Console.WriteLine("DEBUG StandardData (deserialized back to JSON): " + JsonSerializer.Serialize(reportFile?.ReportData?.StandardData));

            Console.WriteLine("DEBUG CrifStandardDataDto assembly: " + typeof(CrifStandardDataDto).Assembly.FullName);
            Console.WriteLine("DEBUG CrifStandardDataDto location: " + typeof(CrifStandardDataDto).Assembly.Location);
            Console.WriteLine("DEBUG CrifStandardDataDto properties: " + string.Join(", ", typeof(CrifStandardDataDto).GetProperties().Select(p => $"{p.Name}:{p.PropertyType.Name}")));

            var alertsProp = typeof(CrifStandardDataDto).GetProperty("Alerts");
            var attrs = alertsProp?.GetCustomAttributes(true) ?? Array.Empty<object>();
            foreach (var a in attrs)
            {
                Console.WriteLine("DEBUG Alerts attribute: " + a.GetType().FullName);
            }
            var stjAttr = attrs.FirstOrDefault(a => a.GetType().FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
            if (stjAttr != null)
            {
                var nameProp = stjAttr.GetType().GetProperty("Name");
                Console.WriteLine("DEBUG JsonPropertyName value = '" + nameProp?.GetValue(stjAttr) + "'");
            }
            else
            {
                Console.WriteLine("DEBUG No System.Text.Json.Serialization.JsonPropertyNameAttribute found on Alerts property!");
            }



            var firstRequestStatus = reportFile?.RequestStatus?.FirstOrDefault();
            var firstRequestedService = reportFile?.ReportData?.RequestedServices?.FirstOrDefault();

            var applnId = requestData?.ApplicationSegment?.InquiryUniqueRefNo ?? "";
            var applicantId = requestData?.ApplicantSegment?.ApplicantId ?? "";
            var dateOfRequest = header?.DateOfRequest ?? "";
            var dateOfIssue = header?.DateOfIssue ?? "";
            var customerId = header?.PreparedForId ?? "";
            var caseId = header?.ReportId ?? "";
            var requestStatus = firstRequestStatus?.Status ?? "";
            var applicantStatus = alerts.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Description))?.Description ?? "";
            var scoreValue = firstRequestedService?.Score?.ScoreValue ?? "";

            // Only real rule hits (those carrying a RULE-CODE/RULE-DESCRIPTION) populate RULE-FULLBASE;
            // the placeholder "No Risk Alerts Found" entry does not.
            var rulesWithData = alerts
                .Where(a => !string.IsNullOrWhiteSpace(a.RuleCode) || !string.IsNullOrWhiteSpace(a.RuleDescription))
                .ToList();

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
           // sb.Append("<RESPONSE>");
            sb.Append("<INQUIRY-STATUS><INQUIRY>");
            sb.Append($"<APPLN-ID>{Escape(applnId)}</APPLN-ID>");
            sb.Append($"<DATE-OF-REQUEST>{Escape(dateOfRequest)}</DATE-OF-REQUEST>");
            sb.Append($"<RESPONSE-DATE>{Escape(dateOfIssue)}</RESPONSE-DATE>");
            sb.Append("</INQUIRY></INQUIRY-STATUS>");

            sb.Append("<SHERLOCK-REPORT><HEADER>");
            sb.Append("<REQUEST-TYPE>SHERLOCK|SHERLOCK-HTML</REQUEST-TYPE>");
            sb.Append($"<CUSTOMER-ID>{Escape(customerId)}</CUSTOMER-ID>");
            sb.Append($"<CASE-ID>{Escape(caseId)}</CASE-ID>");
            sb.Append("<SHERLOCK-DECISION></SHERLOCK-DECISION>");
            sb.Append($"<REQUEST-STATUS>{Escape(requestStatus)}</REQUEST-STATUS>");
            sb.Append($"<DATE-OF-REQUEST>{Escape(dateOfRequest)}</DATE-OF-REQUEST>");
            sb.Append("<RESPONSE-FORMAT>HTML</RESPONSE-FORMAT>");
            sb.Append("</HEADER>");

            sb.Append("<SHERLOCK-DETAILS><APPLICANT>");
            sb.Append("<APPLICANT-TYPE></APPLICANT-TYPE>");
            sb.Append($"<APPLICANT-ID>{Escape(applicantId)}</APPLICANT-ID>");
            sb.Append($"<STATUS>{Escape(applicantStatus)}</STATUS>");
            sb.Append($"<SCORE>{Escape(scoreValue)}</SCORE>");

            sb.Append("<RULES>");
            sb.Append("<RULE-TYPE-FRiDAI/>");
            if (rulesWithData.Count == 0)
            {
                sb.Append("<RULE-TYPE-FULLBASE/>");
            }
            else
            {
                sb.Append("<RULE-TYPE-FULLBASE>");
                foreach (var rule in rulesWithData)
                {
                    sb.Append("<RULE-FULLBASE>");
                    sb.Append($"<RULE-ID>{Escape(rule.RuleCode)}</RULE-ID>");
                    sb.Append($"<DESCRIPTION>{Escape(rule.RuleDescription)}</DESCRIPTION>");
                    sb.Append($"<SEVERITY>{Escape(rule.RuleCategory)}</SEVERITY>");
                    sb.Append("</RULE-FULLBASE>");
                }
                sb.Append("</RULE-TYPE-FULLBASE>");
            }
            sb.Append("</RULES>");

            sb.Append("<VERIFICATION-SUMMARY>");
            sb.Append("<ID-VERIFICATION-SUMMARY/>");
            sb.Append("<LITIGATION-VERIFICATION-SUMMARY/>");
            sb.Append("<AML-VERIFICATION-SUMMARY/>");
            sb.Append("</VERIFICATION-SUMMARY>");

            var printableContent = reportFile?.PrintableReport?.Content ?? "";
            sb.Append("<PRINTABLE-RPRT><![CDATA[");
            sb.Append(printableContent.Replace("]]>", "]]]]><![CDATA[>"));
            sb.Append("]]></PRINTABLE-RPRT>");

            sb.Append("</APPLICANT></SHERLOCK-DETAILS>");
            sb.Append("</SHERLOCK-REPORT>");
           // sb.Append("</RESPONSE>");

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
        private static string TrimNoise(string input)
        {
            return input?.TrimStart('\uFEFF', '\u200B', ' ', '\r', '\n', '\t') ?? string.Empty;
        }
        private static string EscapeStrayAmpersands(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return xml;
            // Matches '&' NOT already followed by a valid entity pattern (amp;, lt;, gt;, apos;, quot;, or #digits;)
            return Regex.Replace(xml, "&(?!amp;|lt;|gt;|apos;|quot;|#\\d+;|#x[0-9A-Fa-f]+;)", "&amp;");
        }

        private static string BuildErrorXml(string message, string code = "ERR_INV_REQ_RCVD")
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<ERROR-FILE>");
            sb.Append("<INQUIRY-STATUS><INQUIRY>");
            sb.Append($"<RESPONSE-DT>{DateTime.Now:dd-MM-yyyy}</RESPONSE-DT>");
            sb.Append("<RESPONSE-TYPE>ERROR</RESPONSE-TYPE>");
            sb.Append("<ERRORS><ERROR>");
            sb.Append($"<CODE>{Escape(code)}</CODE>");
            sb.Append($"<DESCRIPTION>{Escape(message)}</DESCRIPTION>");
            sb.Append("</ERROR></ERRORS>");
            sb.Append("</INQUIRY></INQUIRY-STATUS>");
            sb.Append("</ERROR-FILE>");
            return sb.ToString();
        }

        private static string BuildCrifErrorXml(List<CrifErrorDto> errors)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<ERROR-FILE>");
            sb.Append("<INQUIRY-STATUS><INQUIRY>");
            sb.Append($"<RESPONSE-DT>{DateTime.Now:dd-MM-yyyy}</RESPONSE-DT>");
            sb.Append("<RESPONSE-TYPE>ERROR</RESPONSE-TYPE>");
            sb.Append("<ERRORS>");
            foreach (var err in errors ?? new List<CrifErrorDto>())
            {
                sb.Append("<ERROR>");
                sb.Append($"<CODE>{Escape(err.ErrorCode)}</CODE>");
                sb.Append($"<DESCRIPTION>{Escape(err.ErrorDesc)}</DESCRIPTION>");
                sb.Append("</ERROR>");
            }
            sb.Append("</ERRORS>");
            sb.Append("</INQUIRY></INQUIRY-STATUS>");
            sb.Append("</ERROR-FILE>");
            return sb.ToString();
        }

        private static string BuildTechnicalErrorXml(CrifTechInquiryDto inquiry)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<ERROR-FILE>");
            sb.Append("<INQUIRY-STATUS><INQUIRY>");
            sb.Append($"<RESPONSE-DT>{Escape(inquiry?.ResponseDt ?? DateTime.Now.ToString("dd-MM-yyyy"))}</RESPONSE-DT>");
            sb.Append($"<RESPONSE-TYPE>{Escape(inquiry?.ResponseType ?? "ERROR")}</RESPONSE-TYPE>");
            sb.Append("<ERRORS>");
            foreach (var wrapper in inquiry?.Errors ?? new List<CrifTechErrorWrapperDto>())
            {
                var err = wrapper?.Error;
                if (err == null) continue;
                sb.Append("<ERROR>");
                sb.Append($"<CODE>{Escape(err.Code)}</CODE>");
                sb.Append($"<DESCRIPTION>{Escape(err.Description)}</DESCRIPTION>");
                sb.Append("</ERROR>");
            }
            sb.Append("</ERRORS>");
            sb.Append("</INQUIRY></INQUIRY-STATUS>");
            sb.Append("</ERROR-FILE>");
            return sb.ToString();
        }
    }
}