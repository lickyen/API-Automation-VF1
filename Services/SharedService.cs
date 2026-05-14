using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repositories;
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
        private readonly BranchServices _branchService;
        private readonly AppSettings _appSettings;


        public SharedService(SharedRepository repository, PLFService plfService, BranchServices branchService, AppSettings appSettings)
        {
            _repository = repository;
            _plfService = plfService;
            _branchService = branchService;
            _appSettings = appSettings;
        }

        public async Task<IEnumerable<string>?> GetVistaarBankAccountsAsync()
        {
            var result = await _repository.GetVistaarBankAccountsAsync();
            return result;
        }

        public async Task<LanDetailsPlf?> GetLanDetailsAsync(string lan)
        {
            var loan = await _plfService.GetFinance(lan);
            LanDetailsPlf data = new LanDetailsPlf();

            if (loan == null || loan?.returnStatus?.returnCode != "0000")
            {
                throw new Exception("LAN not found or InActive");
            }

            var cif = loan.customer?.cif;

            if (string.IsNullOrWhiteSpace(cif))
            {
                throw new Exception("CIF not found or InActive");
            }

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

            data.TotalOutstanding = Convert.ToDecimal(loan.financeSchedule?.summary?.outstandingTotal / 100);
            data.PrincipalOutstanding = Convert.ToDecimal(loan.financeSchedule?.summary?.outstandingPri / 100);

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

            if (customer == null || customer?.ReturnStatus?.returnCode != "0000")
            {
                throw new Exception("Customer not found or InActive");
            }

            data.Mobile = customer.Phones?
                .OrderByDescending(p => p.priority)
                .Select(p => p.phoneNumber)
                .FirstOrDefault();

            data.Email = customer.Emails?
                .OrderByDescending(p => p.custEMailPriority)
                .Select(p => p.custEMail)
                .FirstOrDefault();

            var lanCategory = await _repository.GetLanCategoryAsync(lan, data.Dpd);

            data.Action = lanCategory?.Criticality;
            data.Allocation = lanCategory?.Category;
            data.Bucket = lanCategory?.Bucket;

            return data;
        }


        public async Task<KotakData?> GetKotakByLanAsync(string lan)
            => await _repository.GetKotakByLanAsync(lan);

        public  async Task<PaymentLinkResponseDto> PaymentLink(
        PaymentLinkRequestDto chargeRequest)
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
    }
}
