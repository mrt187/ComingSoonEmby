using System;

namespace ComingSoonPlugin.Models
{
    /// <summary>Movie release dates returned by TMDB for one ISO 3166-1 country.</summary>
    public class RegionalReleaseDates
    {
        public DateTime? InCinemas { get; set; }
        public DateTime? Digital { get; set; }
        public DateTime? Physical { get; set; }
    }
}
