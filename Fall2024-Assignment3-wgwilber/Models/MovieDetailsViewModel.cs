namespace Fall2024_Assignment3_wgwilber.Models
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; }
        public IEnumerable<Actor> Actors { get; set; }
        public IEnumerable<Review> Reviews { get; set; }
        public double OverallSentiment { get; set; }


        public MovieDetailsViewModel(Movie movie, IEnumerable<Actor> actors, IEnumerable<Review> reviews, double sentiment)
        {
            Movie = movie;
            Actors = actors;
            Reviews = reviews;
            OverallSentiment = sentiment;
        }
    }
}
