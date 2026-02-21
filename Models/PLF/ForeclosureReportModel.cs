namespace ProductivityApi.Models.PLF
{
    public class ForeclosureReportModel
    {
        public ForeclosureReport ForeclosureReport { get; set; }
        public ReturnStatus ReturnStatus { get; set; }
    }
    public class ForeclosureReport
    {
        public DateTime CalDate { get; set; }
        public string CustName { get; set; }
        public string Address { get; set; }
        public string CustCIF { get; set; }
        public string VanNumber { get; set; }
        public int FinID { get; set; }
        public string FinReference { get; set; }
        public decimal FinAmount { get; set; }
        public string FinAmountInWords { get; set; }
        public decimal FinAssetValue { get; set; }
        public string FinAssetValueInWords { get; set; }
        public decimal FinCurrAssetValue { get; set; }
        public string DisbursalDate { get; set; }
        public string ChrgTillDate { get; set; }
        public decimal OutstandingPri { get; set; }
        public string OutstandingPriInWords { get; set; }
        public decimal LatePayCharges { get; set; }
        public string LatePayChargesInWords { get; set; }
        public decimal CheqBncCharges { get; set; }
        public string CheqBncChargesInWords { get; set; }
        public decimal InstForTheMonth { get; set; }
        public string InstForTheMonthInWords { get; set; }
        public decimal ForeClosFees { get; set; }
        public string ForeClosFeesInWords { get; set; }
        public decimal PrincipalAmt { get; set; }
        public decimal InterestAmt { get; set; }
        public decimal PendingInsts { get; set; }
        public string PendingInstsInWords { get; set; }
        public decimal TDS { get; set; }
        public decimal Refund { get; set; }
        public decimal IntOnTerm { get; set; }
        public decimal TotWaiver { get; set; }
        public decimal TotalDues { get; set; }
        public decimal AdvInsts { get; set; }
        public decimal OtherRefunds { get; set; }
        public string OtherRefundsInWords { get; set; }
        public decimal TotalRefunds { get; set; }
        public string TotalRefundsInWords { get; set; }
        public decimal NetReceivable { get; set; }
        public string NetReceivableInWords { get; set; }
        public decimal NetReceivableNoRefund { get; set; }
        public string NetReceivableNoRefundInWords { get; set; }
        public decimal ManualAdviceAmt { get; set; }
        public decimal LatePayInterestAmt { get; set; }
        public string Total { get; set; }
        public string EntityDesc { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public decimal Amount4 { get; set; }
        public decimal Amount5 { get; set; }
        public decimal Amount6 { get; set; }
        public decimal Amount7 { get; set; }
        public decimal IntPerDay { get; set; }
        public int NoOfIntDays { get; set; }
        public string NameOftheBorrowers { get; set; }
        public string CustSalutation { get; set; }
        public string LinkedFinRef { get; set; }
        public decimal ActPercentage { get; set; }
        public decimal GstOnForeClosFees { get; set; }
        public decimal ForeClosFeesExGST { get; set; }
        public decimal ChargesIncGST { get; set; }
        public string ChargesIncGSTInWords { get; set; }
        public string CustAddrHNbr { get; set; }
        public string CustFlatNo { get; set; }
        public string CustAddrStreet { get; set; }
        public string CustAddrCityName { get; set; }
        public string CustAddrProvinceName { get; set; }
        public string CustAddrZIP { get; set; }
        public string CustAddrCountryName { get; set; }
        public string ValidTill { get; set; }
        public List<string> CollAddressList { get; set; }
        public string FirstInstDate { get; set; }
        public decimal LpiAccured { get; set; }
        public decimal LpiUnAccured { get; set; }
    }

    public class ForeclosureReportRequest
    {
        public string FinReference { get; set; }

        public int days { get; set; }
    }
}



