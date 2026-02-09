namespace ProductivityApi.Models.PLF
{
    public class CustomerDetailsModel
    {
        public string Cif { get; set; }
        public string CoreBankID { get; set; }
        public string CategoryCode { get; set; }
        public string DefaultBranch { get; set; }
        public string BaseCurrency { get; set; }
        public int PrimaryRelationOfficer { get; set; }
        public PersonalInfo PersonalInfo { get; set; }
        public List<Address> Addresses { get; set; }
        public List<Phone> Phones { get; set; }
        public List<Email> Emails { get; set; }
        public List<object> CustomersIncome { get; set; }
        public List<object> CustomersBankInfo { get; set; }
        public List<object> AccountsBehaviour { get; set; }
        public List<object> CustomersExtLiability { get; set; }
        public bool DedupReq { get; set; }
        public bool BlackListReq { get; set; }
        public List<object> CustomerDirector { get; set; }
        public List<object> CustomerGstLists { get; set; }
        public List<object> CustCardSales { get; set; }
        public List<object> CustomerFinanceDetailList { get; set; }
        public ReturnStatus ReturnStatus { get; set; }
        public List<object> GstDetails { get; set; }
        public int Version { get; set; }
    }

}




