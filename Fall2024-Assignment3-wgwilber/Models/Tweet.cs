namespace Fall2024_Assignment3_wgwilber.Models
{
    public class Tweet
    {
        public int Id { get; set; }
        public string? TweeterName { get; set; }
        public string TweetText { get; set; }
        public DateTime CreatedAt { get; set; }
        public double SentimentScore { get; set; }

        public int ActorId { get; set; }
        public Actor Actor { get; set; }
    }
}
