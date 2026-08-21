namespace MovieShowcase.Models
{
    public record Movie
    {
        public long Index { get; init; }
        public string Title { get; init; } = "";
        public string Genre { get; init; } = "";
        public int Year { get; init; }
        public IReadOnlyList<string> Cast { get; init; } = [];
        public string Director { get; init; } = "";
        public int RuntimeMinutes { get; init; }
        public string AgeRating { get; init; } = "";
        public bool IsTop10 { get; init; }
        public string Synopsis { get; init; } = "";
        public int Likes { get; init; }
        public IReadOnlyList<Review> Reviews { get; init; } = [];
        //public TrailerRecipe? Trailer { get; init; }
    }
}