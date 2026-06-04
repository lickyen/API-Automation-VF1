namespace ProductivityApi.Models.PLF
{
    public class ForeclosureStatement
    {
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public decimal SanctionedAmount { get; set; }
        public decimal DisbAmount { get; set; }
        public decimal PrincipalOutstanding { get; set; }
        public decimal PrincipalDue { get; set; }
        public decimal InterestDue { get; set; }
        public decimal Bpi { get; set; }
        public decimal FcCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal Lpp { get; set; }
        public decimal Lpi { get; set; }
        public decimal BpiPerDay { get; set; }
        public decimal Excess { get; set; }
        public decimal NetReceivable { get; set; }
        public decimal Pos { get; set; }
        public decimal Interest { get; set; }
        public decimal Charges { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOutstandingExclFc { get; set; }
    }
}