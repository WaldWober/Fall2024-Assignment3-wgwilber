using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fall2024_Assignment3_wgwilber.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Fall2024_Assignment3_wgwilber.Models.Actor> Actor { get; set; } = default!;

        public DbSet<Fall2024_Assignment3_wgwilber.Models.Movie> Movie { get; set; } = default!;

        public DbSet<Fall2024_Assignment3_wgwilber.Models.ActorMovie> ActorMovie { get; set; } = default!;

        public DbSet<Fall2024_Assignment3_wgwilber.Models.Review> Review { get; set; } = default!;
        public DbSet<Fall2024_Assignment3_wgwilber.Models.Tweet> Tweet { get; set; } = default!;


    }
}
