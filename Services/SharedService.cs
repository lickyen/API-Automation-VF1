using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace ProductivityApi.Services
{
    public class SharedService
    {
        private readonly SharedRepository _repository;
        private readonly PLFService _plfService;
        private readonly PLFRepository _plfRepository;
        private readonly BranchServices _branchService;
        private readonly AppSettings _appSettings;


        public SharedService(SharedRepository repository, PLFService plfService, PLFRepository plfRepository, BranchServices branchService, AppSettings appSettings)
        {
            _repository = repository;
            _plfService = plfService;
            _plfRepository = plfRepository;
            _branchService = branchService;
            _appSettings = appSettings;
        }

        public async Task<IEnumerable<string>?> GetVistaarBankAccountsAsync()
        {
            var result = await _repository.GetVistaarBankAccountsAsync();
            return result;
        }

        public Task<List<ClosureCamLmsValues>> GetClosureCamLmsAsync(string lan)
            => _plfRepository.GetClosureCamLmsAsync(lan);

        public async Task<(LanDetailsPlf? Data, string Message)> GetLanDetailsAsync(string lan)
        {
            var loan = await _plfService.GetFinance(lan);

            if (loan?.returnStatus == null)
                return (null, "Failed to retrieve data from PLF");

            if (loan.returnStatus.returnCode != "0000")
                return (null, loan.returnStatus.returnText);

            var cif = loan.customer?.cif;

            if (string.IsNullOrWhiteSpace(cif))
                return (null, "CIF not found or InActive");

            LanDetailsPlf data = new LanDetailsPlf();
            data.CustId = cif;
            data.CustName = loan.customer?.personalInfo?.shortName;
            data.Lan = loan.finReference;
            data.DisbDate = loan.financeSchedule?.financeDetail?.finStartDate;
            data.FinActiveStatus = loan.financeSchedule?.summary?.finActiveStatus;
            data.Dpd = loan.financeSchedule?.summary?.dpd;
            data.LoanAmount = Convert.ToDecimal(loan.financeSchedule?.financeDetail?.finAssetValue / 100);
            data.DisbAmount = Convert.ToDecimal(loan.financeSchedule?.financeDetail?.finCurrAssetValue / 100);

            decimal totalNetDisbAmount = loan.disbursement != null
                ? loan.disbursement.Sum(d => d.NetDisbursementAmount)
                : 0m;
            data.NetDisbAmount = Convert.ToDecimal(totalNetDisbAmount / 100);
            data.PendingAmount = data.LoanAmount - data.DisbAmount;

            var disbModes = loan.disbursement?
                .Where(d => !string.IsNullOrEmpty(d.disbType))
                .OrderBy(d => d.disbDate)
                .Select(d => d.disbType)
                .ToList()
                ?? new List<string?>();
            data.DisbModes = disbModes;

            //var (statement, msg) = await GetForeclosureStatementAsync(lan);

            //if (msg != "Success")
            //    return (null, msg);

            //data.TotalOutstanding = statement.TotalOutstandingExclFc;
            //data.PrincipalOutstanding = statement.Pos;

           // data.TotalOutstanding = Convert.ToDecimal(loan.financeSchedule?.summary?.outstandingTotal / 100);
           // data.PrincipalOutstanding = Convert.ToDecimal(loan.financeSchedule?.summary?.outstandingPri / 100);

            data.BranchCode = loan.financeSchedule?.financeDetail?.finBranch;

            if (!string.IsNullOrEmpty(data.BranchCode))
            {
                var branch = _branchService.GetBranchByID(data.BranchCode);
                data.BranchName = branch?.BranchName;
                data.Region = branch?.Region;
                data.Zone = branch?.Zone;
                data.MegaZone = branch?.MegaZone;
            }

            var customer = await _plfService.GetCustomerDetails(cif);

            if (customer?.ReturnStatus == null)
                return (null, "Failed to retrieve data from PLF");

            if (customer.ReturnStatus.returnCode != "0000")
                return (null, customer.ReturnStatus.returnText);

            data.Mobile = customer.Phones?
                .OrderByDescending(p => p.priority)
                .Select(p => p.phoneNumber)
                .FirstOrDefault();

            data.AlternateMobile = customer.Phones?
                .OrderByDescending(p => p.priority)
                .Select(p => p.phoneNumber)
                .Skip(1)
                .FirstOrDefault();

            data.Email = customer.Emails?
                .OrderByDescending(p => p.custEMailPriority)
                .Select(p => p.custEMail)
                .FirstOrDefault();

            data.ResidentialAddress = customer.Addresses?
                .OrderByDescending(p => p.priority)
                .Select(p => p.GetFullAddress())
                .FirstOrDefault();

            data.State = customer.Addresses?
                .OrderByDescending(p => p.priority)
                .Select(p => p.stateName)
                .FirstOrDefault();

            var lanCategory = await _repository.GetLanCategoryAsync(lan, data.Dpd);
            data.Action = lanCategory?.Criticality;
            data.Allocation = lanCategory?.Category;
            data.Bucket = lanCategory?.Bucket;

            return (data, "Success");
        }
        public async Task<(ForeclosureStatement Statement, string Message)> GetForeclosureStatementAsync(string lan)
        {
            var request = new ForeclosureReportRequest
            {
                finReference = lan,
                days = 1
            };

            var response = await _plfService.GetForeclosureStmtV1(request);

            if (response?.ReturnStatus == null)
                return (null, "Failed to retrieve data from PLF");

            if (response.ReturnStatus.returnCode != "0000")
                return (null, response.ReturnStatus.returnText);

            var r = response.ForeclosureReport;

            decimal Pos = r.OutstandingPri + r.PrincipalAmt;
            decimal Interest = r.InterestAmt + r.InstForTheMonth;
            decimal Charges = r.ChargesIncGST + r.LatePayInterestAmt + r.ForeClosFees;
            decimal ChargesExclFc = Charges - r.ForeClosFees;
            decimal InterestExclBpi = Interest - r.InstForTheMonth;

            var statement = new ForeclosureStatement
            {
                Lan = r.FinReference,
                CustId = r.CustCIF,
                CustName = r.CustName,
                SanctionedAmount = r.FinAssetValue,
                DisbAmount = r.FinCurrAssetValue,
                PrincipalOutstanding = r.OutstandingPri,
                PrincipalDue = r.PrincipalAmt,
                InterestDue = r.InterestAmt,
                Bpi = r.InstForTheMonth,
                FcCharges = r.ForeClosFees,
                OtherCharges = r.ChargesIncGST,
                Lpp = r.LatePayCharges,
                Lpi = r.LatePayInterestAmt,
                BpiPerDay = r.IntPerDay,
                Excess = r.TotalRefunds,
                NetReceivable = r.NetReceivable,
                Pos = Pos,
                Interest = Interest,
                Charges = Charges,
                TotalOutstanding = Pos + Interest + Charges,
                TotalOutstandingExclFc = Pos + Interest + ChargesExclFc, 
                TotalOutstandingExclFcBpi = Pos + InterestExclBpi + ChargesExclFc  
            };

            return (statement, response.ReturnStatus.returnText);
        }
        public async Task<KotakData?> GetKotakByLanAsync(string lan)
            => await _repository.GetKotakByLanAsync(lan);
        public async Task<string?> GenerateReferenceNoAsync(string prefix)
        {
            return await _repository.GenerateReferenceNoAsync(prefix);
        }
        public async Task<PaymentLinkResponseDto> PaymentLink(PaymentLinkRequestDto chargeRequest)
        {
            var responseObj = new PaymentLinkResponseDto();

            string apiUrl = "https://api.razorpay.com/v1/payment_links";

            using (HttpClient client = new HttpClient())
            {
                // Razorpay credentials
                // string username = "rzp_live_wttpRiUPLjNEWp";
                //string password = "XfOfOV8WuWYsdCPpNDWOc77L";

                string username = _appSettings.RazorPaySettings.UserName;
                string password = _appSettings.RazorPaySettings.Password;

                string authToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{username}:{password}")
                );

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authToken);

                client.Timeout = TimeSpan.FromSeconds(30);

                try
                {
                    string jsonRequest =
                        JsonSerializer.Serialize(chargeRequest);

                    var requestContent = new StringContent(
                        jsonRequest,
                        Encoding.UTF8,
                        "application/json"
                    );

                    HttpResponseMessage response =
                        await client.PostAsync(apiUrl, requestContent);

                    string content =
                        await response.Content.ReadAsStringAsync();

                    responseObj =
                        JsonSerializer.Deserialize<PaymentLinkResponseDto>(
                            content,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return responseObj;
        }
        public async Task<PolicyDetails?> GetPolicyByLanAsync(string lan)
        {
            if (string.IsNullOrEmpty(lan))
                return null;

            return await _repository.GetPolicyByLanAsync(lan);
        }
        public async Task<(List<LanCollateralPlf>? Data, string Message)> GetCollateralsByLanAsync(string lan)
        {
            var loan = await _plfService.GetFinance(lan);
            if (loan?.returnStatus == null)
                return (null, "Failed to retrieve data from PLF");
            if (loan.returnStatus.returnCode != "0000")
                return (null, loan.returnStatus.returnText);

            if (loan.collaterals == null || loan.collaterals.Count == 0)
                return (null, "No collaterals found for this LAN");

            var result = new List<LanCollateralPlf>();

            foreach (var col in loan.collaterals)
            {
                if (string.IsNullOrWhiteSpace(col.collateralRef))
                    continue;

                var request = new CollateralApiRequest
                {
                    collateralRef = col.collateralRef
                };

                var collateral = await _plfService.GetCollateralsByRef(request);

                if (collateral?.returnStatus == null || collateral.returnStatus.returnCode != "0000")
                    continue;

                var mapValues = collateral.extendedFieldRenderList?
                    .FirstOrDefault()?.mapValues;

                var item = new LanCollateralPlf
                {
                    CollateralRef = col.collateralRef,
                    CollateralAddress = mapValues == null ? null :
                        $"{mapValues.addresslineonecoll}, {mapValues.addresslinetwocoll}, {mapValues.citycollateral}, {mapValues.statecollateral} - {mapValues.postalcodecollater}"
                };

                result.Add(item);
            }

            return (result, "Success");
        }
        public async Task<(List<LanApplicantPlf>? Data, string Message)> GetApplicantsByLanAsync(string lan)
        {
            var loan = await _plfService.GetFinance(lan);
            if (loan?.returnStatus == null)
                return (null, "Failed to retrieve data from PLF");
            if (loan.returnStatus.returnCode != "0000")
                return (null, loan.returnStatus.returnText);

            var result = new List<LanApplicantPlf>();

            var applicants = new List<(string? cif, string? name, string type)>
            {
                (loan.customer?.cif, loan.customer?.personalInfo?.shortName, "B")
            };

            if (loan.coApplicants != null)
                applicants.AddRange(loan.coApplicants.Select(a => (a.cif, a.shortName, "C")));

            foreach (var (cif, name, type) in applicants)
            {
                if (string.IsNullOrWhiteSpace(cif))
                    continue;

                var customer = await _plfService.GetCustomerDetails(cif);
                if (customer?.ReturnStatus == null || customer.ReturnStatus.returnCode != "0000")
                    continue;

                result.Add(new LanApplicantPlf
                {
                    CustId = cif,
                    CustName = name,
                    Mobile = customer.Phones?
                                        .OrderByDescending(p => p.priority)
                                        .Select(p => p.phoneNumber)
                                        .FirstOrDefault(),
                    Email = customer.Emails?
                                        .OrderByDescending(p => p.custEMailPriority)
                                        .Select(p => p.custEMail)
                                        .FirstOrDefault(),
                    Address = customer.Addresses?
                                        .OrderByDescending(p => p.priority)
                                        .Select(p => p.GetFullAddress())
                                        .FirstOrDefault(),
                    DOB = customer.PersonalInfo?.dateOfBirth,
                    PAN = customer.PersonalInfo?.custCRCPR,
                    ApplicantType = type
                });
            }

            return (result, "Success");
        }

        public async Task<PlfBankDetails?> GetBankDetailsByIfscAsync(string ifsc)
        {
            if (string.IsNullOrEmpty(ifsc))
                return null;

            return await _plfRepository.GetBankDetailsByIfscAsync(ifsc);
        }

        public async Task<string?> GetAppVersionAsync(string applicationName, string? platform)
            => await _repository.GetAppVersionAsync(applicationName, platform);

        public async Task<List<DisbLansResponseDto>> GetDisbursementsAsync(DisbLansRequestDto request)
        {
            return await _plfRepository.GetDisbursementsAsync(request);
        }
    }
}
