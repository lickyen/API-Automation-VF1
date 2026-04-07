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
            public const string InProgress = "In Progress";   // after any mail is triggered
            public const string Completed = "Completed";       // after UTR is updated
            public const string NotCovered = "Not Covered";
        }

        public static class LcRefundInsurer
        {
            public const string Kotak = "Kotak";
            public const string ICICI = "ICICI";
            public const string NivaBupa = "NivaBupa";
        }


    }
