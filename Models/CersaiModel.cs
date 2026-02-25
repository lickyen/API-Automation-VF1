namespace ProductivityApi.Models
{
    public class CersaiModel
    {
        public int Id { get; set; }

        public string Lan { get; set; }

        public DateOnly DisbDate { get; set; }

        public string SiId { get; set; }

        public string AssetId{ get; set; }

        public string LanStatus { get; set; }

        public string ClosureDesc{ get; set; }

        public DateOnly? ClosureDate { get; set; }

        public string SatisfactionStatus { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDt { get; set; }

        public string? ChargeCreatedBy { get; set; }

        public DateTime? ChargeCreatedDt { get; set; }

        public string? ChargeSatisfiedBy { get; set; }

        public DateTime? ChargeSatisfiedDt { get; set; }
    }

    public class ChargeCreationDto
    {
        public string Lan { get; set;}
        public string SiId { get; set;}
        public string AssetId { get; set;}
        public string UpdatedBy { get; set;}

    }

    public class ChargeSatisfactionDto
    {
        public string Lan { get; set; }
        public string SatisfactionStatus { get; set; }
        public string UpdatedBy { get; set; }

    }

}
