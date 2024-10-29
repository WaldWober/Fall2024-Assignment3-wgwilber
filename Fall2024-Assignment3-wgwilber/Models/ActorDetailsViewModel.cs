namespace Fall2024_Assignment3_wgwilber.Models
{
    public class ActorDetailsViewModel
    {
        public Actor Actor { get; set; }
        public IEnumerable<Movie> Movies { get; set; }
        public IEnumerable<Tweet> Tweets { get; set; }
        public double OverallSentiment { get; set; }
        
        public ActorDetailsViewModel(Actor actor, IEnumerable<Movie> movies, IEnumerable<Tweet> tweets, double sentiment)
        {
            Actor = actor;
            Movies = movies;
            Tweets = tweets;
            OverallSentiment = sentiment;
        }
    }
}
