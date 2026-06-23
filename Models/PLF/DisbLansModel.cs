namespace ProductivityApi.Models.PLF
{
    public class DisbLansResponseDto
    {
        public string Lan { get; set; } = string.Empty;
        public DateTime DisbDate { get; set; }
        public int DisbSeq { get; set; }
        public string LosApplicationNo { get; set; } = string.Empty;
    }

    public class DisbLansRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}