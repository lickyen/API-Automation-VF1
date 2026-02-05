namespace ProductivityApi.Models
{
    public class DCModel
    {
    }
    public class AddEmployeesToCaseDto
    {
          public List<EmployeeDto> Employees { get; set; }
        public int CaseId { get; set; }
        public string Status { get; set; }
    }
    public class AddCaseDto
    {
      //  public List<EmployeeDto> Employees { get; set; }

        public string CaseSummary { get; set; }
        public string OffenseType { get; set; }
        public string Evidence { get; set; }

        // Y / N
        public string SCNToBeSent { get; set; }     

       

        public string CreatedBy { get; set; }
    }

    public class EmployeeDto
    {
        public string EmpCode { get; set; }
        public string EmpName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string BranchID { get; set; }
        public string BranchName { get; set; }
        public string? Supervisor1EmpID { get; set; }
        public string? Supervisor1EmpName { get; set; }
        public string? Supervisor1Email { get; set; }
        public string? Supervisor2EmpID { get; set; }
        public string? Supervisor2EmpName { get; set; }
        public string? Supervisor2Email { get; set; }
        public string? Supervisor3EmpID { get; set; }
        public string? Supervisor3EmpName { get; set; }
        public string? Supervisor3Email { get; set; }
    }

    public class CaseDto
    {
        public int CaseID { get; set; }

        public string? CaseSummary { get; set; }
        public string? OffenseType { get; set; }
        public string? Evidence { get; set; }
        public string? SCNToBeSent { get; set; }
        public string? CaseStatus { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public List<CaseEmployeeDto> Employees { get; set; } = new();
    }
    public class CaseEmployeeDto
    {
        public int CaseEmpID { get; set; }
        public string? EmpCode { get; set; }
        public string? EmpName { get; set; }
        public string? Role { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string? BranchID { get; set; }
        public string? BranchName { get; set; }
        public string? Supervisor1EmpID { get; set; }
        public string? Supervisor1EmpName { get; set; }
        public string? Supervisor1Email { get; set; }
        public string? Supervisor2EmpID { get; set; }
        public string? Supervisor2EmpName { get; set; }
        public string? Supervisor2Email { get; set; }
        public string? Supervisor3EmpID { get; set; }
        public string? Supervisor3EmpName { get; set; }
        public string? Supervisor3Email { get; set; }
    }
    public class CaseListDto
    {
        public int CaseID { get; set; }
        public string? CaseSummary { get; set; }
        public string? OffenseType { get; set; }
        public string? CaseStatus { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int EmployeeCount { get; set; }
    }


    public class CaseRequest
    {
        public string EmpId { get; set; }
        public string Status { get; set; }
    }

    public class HRUpdateDto
    {
        public int CaseID { get; set; }
        public string SCN_Doc { get; set; }
        public string Demand_Notice { get; set; }
        public string Legal_Notice { get; set; }
        public string HR_Remarks { get; set; }
        public string HR_UpdatedBy { get; set; }
    }
    public class EmployeeResponseDto
    {
        public int CaseID { get; set; }
        public string EmployeeResponse { get; set; } = string.Empty;
        public string EmployeeUpdatedBy { get; set; } = string.Empty;

        // List of evidence documents
        public List<EvidenceDocDto> EvidenceDocs { get; set; } = new List<EvidenceDocDto>();
    }
    public class EvidenceDocDto
    {
        public string EvidenceDocName { get; set; } = string.Empty;
    }

}
