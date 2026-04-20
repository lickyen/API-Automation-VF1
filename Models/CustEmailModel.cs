namespace ProductivityApi.Models
{
    public class CustEmailModel
    {
    }
    public class CustomerEmailDto
    {
        public int Id { get; set; }
        public long CustId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public DateTime? DOB { get; set; }
        public string Address { get; set; }
        public string Aadhaar { get; set; }
        public string Old_EmailId { get; set; }
        public string New_EmailId { get; set; }
        public string PAN { get; set; }
        public string Email_Validated { get; set; }
        public string Telecaller_Action { get; set; } = String.Empty;
        public DateTime? Telecaller_UpdatedDT { get; set; } 
        public string Telecaller_UpdatedID { get; set; } = String.Empty;
        public string Telecaller_Remarks { get; set; } = String.Empty;
       
        
        public string Pennant_Response { get; set; } = String.Empty;
        public DateTime? Pennan_UpdatedDt { get; set; } 

        // public string IsActive { get; set; }
        public DateTime? CreatedDt { get; set; }
        public DateTime? UpdatedDt { get; set; }
       
    }
    public class UpdateDto
    {
        public int ID { get; set; }

        public string Status { get; set; }

        public int UserId { get; set; }

        public string Telecaller_Remarks { get; set; }
    }
}
