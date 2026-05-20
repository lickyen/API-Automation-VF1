namespace ProductivityApi.Models
{
    // =========================================
    // TRAINING MASTER
    // =========================================

    public class TrainingMasterModel
    {
        public int TrainingId { get; set; }

        public string? TrainingName { get; set; }
    }

    // =========================================
    // TRAINING LOCATION
    // =========================================

    public class TrainingLocationModel
    {
        public string? Location { get; set; }
    }

    // =========================================
    // TRAINING EMPLOYEE LIST
    // =========================================

    public class TrainingEmployeeModel
    {
        public int ID { get; set; }

        public string? EmpID { get; set; }

        public string? Location { get; set; }

        public string? TrainingName { get; set; }

        public DateTime? Start_Date { get; set; }

        public DateTime? Expiry_Date { get; set; }
    }

    // =========================================
    // ASSIGN SINGLE TRAINING DTO
    // =========================================

    public class AssignTrainingRequestModel
    {
        public string? EmpID { get; set; }

        public int TrainingID { get; set; }

        public string? Location { get; set; }

        public DateTime Start_Date { get; set; }

        public DateTime Expiry_Date { get; set; }

        public string? CreatedBy { get; set; }
    }

    // =========================================
    // BULK ASSIGN DTO
    // =========================================

    public class BulkAssignTrainingModel
    {
        public List<string> EmpIDs { get; set; } = new();

        public int TrainingID { get; set; }

        public string? Location { get; set; }

        public DateTime Start_Date { get; set; }

        public DateTime Expiry_Date { get; set; }

        public string? CreatedBy { get; set; }
    }

    // =========================================
    // BULK ASSIGN RESPONSE DTO
    // =========================================

    public class BulkAssignTrainingResponseModel
    {
        public string? EmpID { get; set; }

        public string? Status { get; set; }
    }

    // =========================================
    // COMMON API RESPONSE DTO
    // =========================================

    public class TrainingApiResponseModel
    {
        public bool Success { get; set; }

        public string? Message { get; set; }
    }
}