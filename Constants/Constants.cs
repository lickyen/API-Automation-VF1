namespace ProductivityApi.Constants
{
    public static class FcRefundStatus
    {
        public const string Foreclosed = "Foreclosed";
        public const string PendingForAction = "Pending For Action";
        public const string MailSentToInsurer = "Mail Sent to Insurer";
        public const string Completed = "Completed";
    }
    public static class LcRefundStatus
    {
        public const string New = "New";
        public const string InProgress = "In Progress";  
        public const string Completed = "Completed";   
        public const string NotCovered = "Not Covered";
    }

    public static class LcRefundInsurer
    {
        public const string Kotak = "Kotak";
        public const string ICICI = "ICICI";
        public const string NivaBupa = "NivaBupa";
    }

    public static class LegalBillingStatus
    {
        public const string Initiated = "Initiated";

        public const string ZDSHApproved = "ZDSH-Approved";
        public const string ZDSHRejected = "ZDSH-Rejected";
        public const string ZDSHSentBack = "ZDSH-SentBack";

        public const string MZDSHApproved = "MZDSH-Approved";
        public const string MZDSHRejected = "MZDSH-Rejected";
        public const string MZDSHSentBack = "MZDSH-SentBack";

        public const string CDSOApproved = "CDSO-Approved";
        public const string CDSORejected = "CDSO-Rejected";
        public const string CDSOSentBack = "CDSO-SentBack";

        public const string FinanceApproved = "Finance-Approved";

        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Pending = "Pending";
    }

    public static class LegalBillingRoles
    {
        public const string CDSM = "CDSM";
        public const string ZDSH = "ZDSH";
        public const string MZDSH = "MZDSH";
        public const string CDSO = "CDSO";
        public const string Legal = "Legal";
        public const string Finance = "Finance";
    }
}
