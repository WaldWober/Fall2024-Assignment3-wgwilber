namespace Fall2024_Assignment3_wgwilber.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string? ReviewerName { get; set; }
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }
        public double SentimentScore { get; set; }


        public int MovieId { get; set; }
        public Movie Movie { get; set; }
    }
}
