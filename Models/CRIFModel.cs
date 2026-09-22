using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ProductivityApi.Models.XmlWrapper
{
    public class CRIFModel
    { }
    public class CrifJsonRequest
    {
        [JsonPropertyName("REQUEST-FILE")]
        public CrifRequestFileDto RequestFile { get; set; }
    }

    public class CrifRequestFileDto
    {
        [JsonPropertyName("HEADER-SEGMENT")]
        public CrifHeaderSegmentDto HeaderSegment { get; set; }

        [JsonPropertyName("INQUIRY")]
        public CrifInquiryDto Inquiry { get; set; }
    }

    public class CrifHeaderSegmentDto
    {
        [JsonPropertyName("PRODUCT-TYPE")] public string ProductType { get; set; }
        [JsonPropertyName("PRODUCT-VER")] public string ProductVer { get; set; }
        [JsonPropertyName("USER-ID")] public string UserId { get; set; }
        [JsonPropertyName("USER-PWD")] public string UserPwd { get; set; }
        [JsonPropertyName("REQ-MBR")] public string ReqMbr { get; set; }
        [JsonPropertyName("INQ-DT-TM")] public string InqDtTm { get; set; }
        [JsonPropertyName("REQ-VOL-TYPE")] public string ReqVolType { get; set; }
        [JsonPropertyName("REQ-ACTN-TYPE")] public string ReqActnType { get; set; }
        [JsonPropertyName("AUTH-FLG")] public string AuthFlg { get; set; }
        [JsonPropertyName("AUTH-TITLE")] public string AuthTitle { get; set; }
        [JsonPropertyName("RES-FRMT")] public string ResFrmt { get; set; }
        [JsonPropertyName("MEMBER-PREF-OVERRIDE")] public string MemberPrefOverride { get; set; }
        [JsonPropertyName("RES-FRMT-EMBD")] public string ResFrmtEmbd { get; set; }
        [JsonPropertyName("LOS-NAME")] public string LosName { get; set; }
        [JsonPropertyName("LOS-VENDOR")] public string LosVendor { get; set; }
        [JsonPropertyName("LOS-VERSION")] public string LosVersion { get; set; }
        [JsonPropertyName("REQ-SERVICES-TYPE")] public string ReqServicesType { get; set; }
    }

    public class CrifInquiryDto
    {
        [JsonPropertyName("APPLICANT-SEGMENT")]
        public CrifApplicantSegmentDto ApplicantSegment { get; set; }

        [JsonPropertyName("APPLICATION-SEGMENT")]
        public CrifApplicationSegmentDto ApplicationSegment { get; set; }
    }

    public class CrifApplicantSegmentDto
    {
        [JsonPropertyName("APPLICANT-ID")] public string ApplicantId { get; set; }
        [JsonPropertyName("FIRST-NAME")] public string FirstName { get; set; }
        [JsonPropertyName("MIDDLE-NAME")] public string MiddleName { get; set; }
        [JsonPropertyName("LAST-NAME")] public string LastName { get; set; }
        [JsonPropertyName("DOB")] public CrifDobDto Dob { get; set; }
        [JsonPropertyName("RELATIONS")] public List<CrifRelationDto> Relations { get; set; } = new();
        [JsonPropertyName("IDS")] public List<CrifIdDto> Ids { get; set; } = new();
        [JsonPropertyName("ADDRESSES")] public List<CrifAddressDto> Addresses { get; set; } = new();
        [JsonPropertyName("PHONES")] public List<CrifPhoneDto> Phones { get; set; } = new();
        [JsonPropertyName("EMAILS")] public List<CrifEmailDto> Emails { get; set; } = new();
        [JsonPropertyName("ACCOUNT-NUMBER")] public string AccountNumber { get; set; }
        [JsonPropertyName("GENDER")] public string Gender { get; set; }
    }

    public class CrifDobDto
    {
        [JsonPropertyName("DOB-DT")] public string DobDt { get; set; }
        [JsonPropertyName("AGE")] public string Age { get; set; }
        [JsonPropertyName("AGE-AS-ON")] public string AgeAsOn { get; set; }
    }

    public class CrifRelationDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("VALUE")] public string Value { get; set; }
    }

    public class CrifIdDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("VALUE")] public string Value { get; set; }
    }

    public class CrifAddressDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("ADDRESS-TEXT")] public string AddressText { get; set; }
        [JsonPropertyName("CITY")] public string City { get; set; }
        [JsonPropertyName("STATE")] public string State { get; set; }
        [JsonPropertyName("LOCALITY")] public string Locality { get; set; }
        [JsonPropertyName("PIN")] public string Pin { get; set; }
        [JsonPropertyName("COUNTRY")] public string Country { get; set; }
    }

    public class CrifPhoneDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("VALUE")] public string Value { get; set; }
    }

    public class CrifEmailDto
    {
        [JsonPropertyName("EMAIL")] public string Email { get; set; }
    }

    public class CrifApplicationSegmentDto
    {
        [JsonPropertyName("INQUIRY-UNIQUE-REF-NO")] public string InquiryUniqueRefNo { get; set; }
        [JsonPropertyName("CREDIT-RPT-TRN-DT-TM")] public string CreditRptTrnDtTm { get; set; }
        [JsonPropertyName("CREDIT-INQ-PURPS-TYPE")] public string CreditInqPurpsType { get; set; }
        [JsonPropertyName("CREDIT-INQUIRY-STAGE")] public string CreditInquiryStage { get; set; }
        [JsonPropertyName("CLIENT-CONTRIBUTOR-ID")] public string ClientContributorId { get; set; }
        [JsonPropertyName("BRANCH-ID")] public string BranchId { get; set; }
        [JsonPropertyName("APPLICATION-ID")] public string ApplicationId { get; set; }
        [JsonPropertyName("ACNT-OPEN-DT")] public string AcntOpenDt { get; set; }
        [JsonPropertyName("LOAN-AMT")] public string LoanAmt { get; set; }
        [JsonPropertyName("LTV")] public string Ltv { get; set; }
        [JsonPropertyName("TERM")] public string Term { get; set; }
        [JsonPropertyName("LOAN-TYPE")] public string LoanType { get; set; }
        [JsonPropertyName("LOAN-TYPE-DESC")] public string LoanTypeDesc { get; set; }
    }
        [XmlRoot("REQUEST-FILE")]
        public class CrifLegacyXmlRequest
        {
            [XmlElement("HEADER-SEGMENT")]
            public LegacyHeaderSegment HeaderSegment { get; set; }

            [XmlElement("INQUIRY")]
            public LegacyInquiry Inquiry { get; set; }
        }

        public class LegacyHeaderSegment
        {
            [XmlElement("REQUEST-ACTION-TYPE")]
            public string RequestActionType { get; set; }

            [XmlElement("DATE-OF-REQUEST")]
            public string DateOfRequest { get; set; }

            [XmlElement("RESPONSE-FORMAT")]
            public string ResponseFormat { get; set; }

            [XmlElement("REQUEST-TYPE")]
            public string RequestType { get; set; }
        }

        public class LegacyInquiry
        {
            [XmlElement("PRIORITY")]
            public string Priority { get; set; }

            [XmlElement("APPLICATION-SEGMENT")]
            public LegacyApplicationSegment ApplicationSegment { get; set; }
        }

        public class LegacyApplicationSegment
        {
            [XmlElement("APPLN-ID")]
            public string ApplnId { get; set; }

            [XmlElement("APPLN-DT")]
            public string ApplnDt { get; set; }

            [XmlElement("BRANCH-REGION")]
            public string BranchRegion { get; set; }

            [XmlElement("BRANCH-ID")]
            public string BranchId { get; set; }

            [XmlElement("ACCT-OPEN-DATE")]
            public string AcctOpenDate { get; set; }

            [XmlElement("APPLN-VALUE")]
            public string ApplnValue { get; set; }

            [XmlElement("LTV")]
            public string Ltv { get; set; }

            [XmlElement("TERM")]
            public string Term { get; set; }

            [XmlElement("APPLN-TYPE")]
            public string ApplnType { get; set; }

            [XmlElement("OTHER-DESC")]
            public string OtherDesc { get; set; }

            [XmlArray("APPLICANT-SEGMENT")]
            [XmlArrayItem("APPLICANT")]
            public List<LegacyApplicant> Applicants { get; set; } = new();
        }

        public class LegacyApplicant
        {
            [XmlElement("APPLICANT-TYPE")]
            public string ApplicantType { get; set; }

            [XmlElement("APPLICANT-ID")]
            public string ApplicantId { get; set; }

            [XmlElement("GENDER")]
            public string Gender { get; set; }

            [XmlElement("APPLICANT-NAME")]
            public LegacyApplicantName ApplicantName { get; set; }

            [XmlElement("DOB")]
            public LegacyDob Dob { get; set; }

            [XmlArray("IDS")]
            [XmlArrayItem("ID")]
            public List<LegacyId> Ids { get; set; } = new();

            [XmlArray("RELATIONS")]
            [XmlArrayItem("RELATION")]
            public List<LegacyRelation> Relations { get; set; } = new();

            [XmlElement("ACC-VERIFICATION")]
            public LegacyAccVerification AccVerification { get; set; }

            [XmlArray("ADDRESSES")]
            [XmlArrayItem("ADDRESS")]
            public List<LegacyAddress> Addresses { get; set; } = new();

            [XmlArray("PHONES")]
            [XmlArrayItem("PHONE")]
            public List<LegacyPhone> Phones { get; set; } = new();

            [XmlArray("E-MAILS")]
            [XmlArrayItem("E-MAIL")]
            public List<LegacyEmail> Emails { get; set; } = new();
        }

        public class LegacyApplicantName
        {
            [XmlElement("NAME1")] public string Name1 { get; set; }
            [XmlElement("NAME2")] public string Name2 { get; set; }
            [XmlElement("NAME3")] public string Name3 { get; set; }
            [XmlElement("NAME4")] public string Name4 { get; set; }
            [XmlElement("NAME5")] public string Name5 { get; set; }
        }

        public class LegacyDob
        {
            [XmlElement("DOB-DATE")] public string DobDate { get; set; }
            [XmlElement("AGE-AS-ON")] public string AgeAsOn { get; set; }
            [XmlElement("AGE")] public string Age { get; set; }
        }

        public class LegacyId
        {
            [XmlElement("APPLICANT-ID-VALUE")] public string Value { get; set; }
            [XmlElement("APPLICANT-ID-TYPE")] public string Type { get; set; }
        }

        public class LegacyRelation
        {
            [XmlElement("RELATION-TYPE")] public string RelationType { get; set; }
            [XmlElement("RELATIVE-NAME")] public string RelativeName { get; set; }
        }

        public class LegacyAccVerification
        {
            [XmlElement("VERIFICATION")]
            public LegacyVerification Verification { get; set; }
        }

        public class LegacyVerification
        {
            [XmlElement("ACCT-NUMBER")]
            public string AcctNumber { get; set; }
        }

        public class LegacyAddress
        {
            [XmlElement("ADDRESS-TYPE")] public string AddressType { get; set; }
            [XmlElement("ADDRESS-LINE1")] public string Line1 { get; set; }
            [XmlElement("ADDRESS-LINE2")] public string Line2 { get; set; }
            [XmlElement("ADDRESS-LINE3")] public string Line3 { get; set; }
            [XmlElement("ADDRESS-LINE4")] public string Line4 { get; set; }
            [XmlElement("ADDRESS-LINE5")] public string Line5 { get; set; }
            [XmlElement("CITY")] public string City { get; set; }
            [XmlElement("STATE")] public string State { get; set; }
            [XmlElement("PIN")] public string Pin { get; set; }
            [XmlElement("COUNTRY")] public string Country { get; set; }
        }

        public class LegacyPhone
        {
            [XmlElement("TELE-NO")] public string TeleNo { get; set; }
            [XmlElement("TELE-NO-TYPE")] public string TeleNoType { get; set; }
        }

        public class LegacyEmail
        {
            [XmlElement("EMAIL-TYPE")] public string EmailType { get; set; }
            [XmlElement("EMAIL-ID")] public string EmailId { get; set; }
        }
    // ---- JSON envelope from CRIF ----
    public class CrifSherlockJsonResponse
    {
        [JsonPropertyName("responseInfo")]
        public string ResponseInfo { get; set; }
    }

    // ---- Inner XML inside responseInfo ----
    [XmlRoot("RESPONSE")]
    public class CrifSherlockXmlResponse
    {
        [XmlElement("INQUIRY-STATUS")]
        public InquiryStatusDto InquiryStatus { get; set; }

        [XmlElement("SHERLOCK-REPORT")]
        public SherlockReportDto SherlockReport { get; set; }
    }

    public class InquiryStatusDto
    {
        [XmlElement("INQUIRY")]
        public InquiryDetailDto Inquiry { get; set; }
    }

    public class InquiryDetailDto
    {
        [XmlElement("APPLN-ID")] public string ApplnId { get; set; }
        [XmlElement("DATE-OF-REQUEST")] public string DateOfRequest { get; set; }
        [XmlElement("RESPONSE-DATE")] public string ResponseDate { get; set; }
    }

    public class SherlockReportDto
    {
        [XmlElement("HEADER")]
        public SherlockHeaderDto Header { get; set; }

        [XmlElement("SHERLOCK-DETAILS")]
        public SherlockDetailsDto Details { get; set; }
    }

    public class SherlockHeaderDto
    {
        [XmlElement("REQUEST-TYPE")] public string RequestType { get; set; }
        [XmlElement("CUSTOMER-ID")] public string CustomerId { get; set; }
        [XmlElement("CASE-ID")] public string CaseId { get; set; }
        [XmlElement("SHERLOCK-DECISION")] public string SherlockDecision { get; set; }
        [XmlElement("REQUEST-STATUS")] public string RequestStatus { get; set; }
        [XmlElement("DATE-OF-REQUEST")] public string DateOfRequest { get; set; }
        [XmlElement("RESPONSE-FORMAT")] public string ResponseFormat { get; set; }
    }

    public class SherlockDetailsDto
    {
        [XmlElement("APPLICANT")]
        public SherlockApplicantDto Applicant { get; set; }
    }

    public class SherlockApplicantDto
    {
        [XmlElement("APPLICANT-TYPE")] public string ApplicantType { get; set; }
        [XmlElement("APPLICANT-ID")] public string ApplicantId { get; set; }
        [XmlElement("STATUS")] public string Status { get; set; }
        [XmlElement("SCORE")] public string Score { get; set; }
        [XmlElement("PRINTABLE-RPRT")] public string PrintableReport { get; set; }
        // RULES and VERIFICATION-SUMMARY are empty/self-closing in the sample —
        // intentionally unmapped; XmlSerializer ignores them silently.
    }
    public class CrifReportEnvelope
    {
        [JsonPropertyName("CIR-REPORT-FILE")]
        public CrifReportFileDto ReportFile { get; set; }
    }

    public class CrifReportFileDto
    {
        [JsonPropertyName("HEADER-SEGMENT")] public CrifResponseHeaderDto HeaderSegment { get; set; }
        [JsonPropertyName("REQUEST-STATUS")] public List<CrifRequestStatusDto> RequestStatus { get; set; } = new();
        [JsonPropertyName("REPORT-DATA")] public CrifReportDataDto ReportData { get; set; }
        [JsonPropertyName("PRINTABLE-REPORT")] public CrifPrintableReportDto PrintableReport { get; set; }

        // Present only on CRIF's error shape (validation failures before a report is generated)
        [JsonPropertyName("PRODUCT-TYPE")] public string ErrorProductType { get; set; }
        [JsonPropertyName("PRODUCT-VERSION")] public string ErrorProductVersion { get; set; }
        [JsonPropertyName("ERRORS")] public List<CrifErrorDto> Errors { get; set; }
    }

    public class CrifResponseHeaderDto
    {
        [JsonPropertyName("DATE-OF-REQUEST")] public string DateOfRequest { get; set; }
        [JsonPropertyName("PREPARED-FOR")] public string PreparedFor { get; set; }
        [JsonPropertyName("PREPARED-FOR-ID")] public string PreparedForId { get; set; }
        [JsonPropertyName("DATE-OF-ISSUE")] public string DateOfIssue { get; set; }
        [JsonPropertyName("REPORT-ID")] public string ReportId { get; set; }
        [JsonPropertyName("BATCH-ID")] public string BatchId { get; set; }
        [JsonPropertyName("STATUS")] public string Status { get; set; }
        [JsonPropertyName("PRODUCT-TYPE")] public string ProductType { get; set; }
        [JsonPropertyName("PRODUCT-VER")] public string ProductVer { get; set; }
    }

    public class CrifRequestStatusDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("STATUS")] public string Status { get; set; }
        [JsonPropertyName("ERRORS")] public List<CrifErrorDto> Errors { get; set; } = new();
    }

    public class CrifReportDataDto
    {
        [JsonPropertyName("ALERTS")] public List<CrifAlertDto> Alerts { get; set; } = new();
        // STANDARD-DATA, ACCOUNTS-SUMMARY, etc. intentionally unmapped for now —
        // add fields here later if you need tradelines/score/summary in the output.
    }

    public class CrifAlertDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("DESCRIPTION")] public string Description { get; set; }
        [JsonPropertyName("RULES")] public List<CrifRuleDto> Rules { get; set; } = new();
    }

    public class CrifRuleDto
    {
        [JsonPropertyName("RULE-CATEGORY")] public string RuleCategory { get; set; }
        [JsonPropertyName("RULE-CODE")] public string RuleCode { get; set; }
        [JsonPropertyName("RULE-DESCRIPTION")] public string RuleDescription { get; set; }
    }

    public class CrifPrintableReportDto
    {
        [JsonPropertyName("TYPE")] public string Type { get; set; }
        [JsonPropertyName("FILE-NAME")] public string FileName { get; set; }
        [JsonPropertyName("CONTENT")] public string Content { get; set; }
    }
    public class CrifErrorDto
    {
        [JsonPropertyName("ERROR-CODE")] public string ErrorCode { get; set; }
        [JsonPropertyName("ERROR-DESC")] public string ErrorDesc { get; set; }
    }
}