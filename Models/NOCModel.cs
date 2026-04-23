using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class NocTransaction
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string CustId { get; set; }
        public string CollId { get; set; }
        public string CustName { get; set; }
        public string CustMobile { get; set; }
        public string CoAppCustId { get; set; }
        public string CoAppCollId { get; set; }
        public string CoAppName { get; set; }
        public string CoAppMobile { get; set; }
        public string LanStatus { get; set; }
        public string LanStatusCode { get; set; }
        public DateTime? DisbDt { get; set; }
        public DateTime? ClosedDt { get; set; }
        public string NocStatus { get; set; }
        public string EsignerId { get; set; }
        public string RmuFilePath { get; set; }
        public string PropertyOwner { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class NOCPendingLanDto
    {
        public string Lan { get; set; }
        public int BranchId { get; set; }
        public string CustId { get; set; }
        public string CollId { get; set; }
        public DateTime? DisbDt { get; set; }
        public string CoAppCustId { get; set; }
        public string CoAppCollId { get; set; }
        public string StatusCode { get; set; }
        public DateTime? ClosedDt { get; set; }
        public string NocStatus { get; set; }
    }

    public class NeolotexDisb
    {
        [Key]
        [Column("sz_loan_account_no")]
        [StringLength(50)]
        public string sz_loan_account_no { get; set; }

        [Column("applicant_type")]
        [StringLength(1)]
        public string applicant_type { get; set; }

        [Column("branch_code")]
        [StringLength(20)]
        public string branch_code { get; set; }

        [Column("branchname")]
        [StringLength(100)]
        public string branchname { get; set; }

        [Column("cluster_name")]
        [StringLength(100)]
        public string cluster_name { get; set; }

       
        [StringLength(100)]
        public string state_name { get; set; }

       
        [StringLength(50)]
        public string customerid { get; set; }

        
        [StringLength(150)]
        public string customer_name { get; set; }

       
        [StringLength(50)]
        public string? sz_collateral_id { get; set; }

       
        [StringLength(50)]
        public string loan_status { get; set; }

      
        public DateTime? loanclosure_date { get; set; }

       
        public DateTime? disbursement_date { get; set; }

       
        public DateTime? dt_correction { get; set; }

       
        [StringLength(20)]
        public string noc_status { get; set; }

       
        public DateTime? extraction_dt { get; set; }

     
        [StringLength(100)]
        public string external_reference { get; set; }
    }

}