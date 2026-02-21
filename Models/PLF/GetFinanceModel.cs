namespace ProductivityApi.Models.PLF
{
    public class GetFinanceModel
    {
        public string finReference { get; set; }
        public Customer customer { get; set; }
        public FinanceSchedule financeSchedule { get; set; }
        public List<Disbursement> disbursement { get; set; }
        public ReturnStatus returnStatus { get; set; }
    }

    public class Customer
    {
        public string cif { get; set; }
        public string defaultBranch { get; set; }
        public PersonalInfo personalInfo { get; set; }
        public List<Address> addresses { get; set; }
        public List<Phone> phones { get; set; }
        public List<Email> emails { get; set; }
    }

    public class PersonalInfo
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string salutation { get; set; }
        public string shortName { get; set; }
        public string fatherName { get; set; }
        public string language { get; set; }
        public string dateOfBirth { get; set; }
        public string countryOfBirth { get; set; }
        public string nationality { get; set; }
        public string gender { get; set; }
        public string maritalStatus { get; set; }
        public string phoneNumber { get; set; }
    }

    public class Address
    {
        public string addrType { get; set; }
        public string buildingNo { get; set; }
        public string flatNo { get; set; }
        public string street { get; set; }
        public string addrLine1 { get; set; }
        public string addrLine2 { get; set; }
        public string poBox { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string pinCode { get; set; }
        public int priority { get; set; }
        public bool mailingAddress { get; set; }
        public int version { get; set; }
        public int pinCodeId { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string countryName { get; set; }
        public string stateName { get; set; }
        public string cityName { get; set; }
        public bool sameAsPermanentAddress { get; set; }

        public string GetFullAddress()
        {
            return $"{buildingNo}-{flatNo}, {street}, {addrLine1}, {addrLine2}, {cityName}, {stateName}, {pinCode}";
        }
    }

    public class Phone
    {
        public string phoneTypeCode { get; set; }
        public string phoneNumber { get; set; }
        public int priority { get; set; }
        public int version { get; set; }
    }

    public class Email
    {
        public string custEMailTypeCode { get; set; }
        public string custEMail { get; set; }
        public int custEMailPriority { get; set; }
        public int version { get; set; }
    }

    public class Finance
    {
        public FinanceSchedule financeSchedule { get; set; }
        public List<ForeClosure> foreClosures { get; set; }
    }

    public class FinanceSchedule
    {
        public FinanceDetail financeDetail { get; set; }
        public List<Schedule> schedules { get; set; }
        public Summary summary { get; set; }
    }

    public class FinanceDetail
    {
        public string finType { get; set; }
        public string finBranch { get; set; }
        public double finAmount { get; set; }
        public double finAssetValue { get; set; }
        public DateTime firstDisbDate { get; set; }
        public DateTime lastDisbDate { get; set; }
        public DateTime finStartDate { get; set; }
    }

    public class Summary
    {
        public double effectiveRateOfReturn { get; set; }
        public double totalGracePft { get; set; }
        public double totalGraceCpz { get; set; }
        public double totalGrossGrcPft { get; set; }
        public double totalCpz { get; set; }
        public double totalProfit { get; set; }
        public double totalRepayAmt { get; set; }
        public double feeChargeAmount { get; set; }
        public int numberOfTerms { get; set; }
        public int loanTenor { get; set; }
        public DateTime maturityDate { get; set; }
        public DateTime firstDisbDate { get; set; }
        public DateTime lastDisbDate { get; set; }
        public double firstEmiAmount { get; set; }
        public DateTime nextSchDate { get; set; }
        public double nextRepayAmount { get; set; }
        public double futureInst { get; set; }
        public double futureTenor { get; set; }
        public DateTime firstInstDate { get; set; }
        public double paidTotal { get; set; }
        public double paidPri { get; set; }
        public double paidPft { get; set; }
        public DateTime lastRepayDate { get; set; }
        public double outstandingTotal { get; set; }
        public double outstandingPri { get; set; }
        public double outstandingPft { get; set; }
        public double overdueTotal { get; set; }
        public double overduePri { get; set; }
        public double overduePft { get; set; }
        public int overdueInst { get; set; }
        public double overdueTotalIncludeCharges { get; set; }
        public double advPaymentAmount { get; set; }
        public string finActiveStatus { get; set; }
        public bool fullyDisb { get; set; }
        public double limitBalance { get; set; }
        public double billedAmount { get; set; }
        public double unbilledAmount { get; set; }
        public double limitDrop { get; set; }
        public double currentDroplineLimit { get; set; }
        public double utilisation { get; set; }
        public double availableLimit { get; set; }
        public double graceInst { get; set; }
        public double graceTenor { get; set; }
        public double futureGraceInst { get; set; }
        public double futureGraceTenor { get; set; }
        public double dueCharges { get; set; }
        public double overDueAmount { get; set; }
        public double bpiAmount { get; set; }
        public double dpd { get; set; }
    }

    public class Schedule
    {
        public DateTime schDate { get; set; }
        public double pftAmount { get; set; }
        public double schdPft { get; set; }
        public double schdPftPaid { get; set; }
        public double schdPri { get; set; }
        public double schdPriPaid { get; set; }
        public double feeSchd { get; set; }
        public double tdsAmount { get; set; }
        public double totalAmount { get; set; }
        public double endBal { get; set; }
        public double limitDrop { get; set; }
        public double dropLineLimit { get; set; }
        public double availableLimit { get; set; }
        public string bpiOrHoliday { get; set; }
        public int instNumber { get; set; }
        public int version { get; set; }
        public DateTime dueDate { get; set; }
    }

    public class ForeClosure
    {
        public DateTime valueDate { get; set; }
        public double accruedIntTillDate { get; set; }
        public double foreCloseAmount { get; set; }
        public double chargeAmount { get; set; }
        public double bounceCharge { get; set; }
        public double lpiAmount { get; set; }
        public double receivableADFee { get; set; }
    }

    public class Disbursement
    {
        public string disbParty { get; set; }
        public string disbType { get; set; }
        public DateTime disbDate { get; set; }
        public decimal disbAmount { get; set; }
        public int partnerBankId { get; set; }
        public string partnerBankCode { get; set; }
        public string favourName { get; set; }
        public DateTime valueDate { get; set; }
        public int version { get; set; }
        public string finReference { get; set; }
        public bool holdDisbursement { get; set; }
        public long disbInstId { get; set; }
        public string disbStatus { get; set; }
    }


}
