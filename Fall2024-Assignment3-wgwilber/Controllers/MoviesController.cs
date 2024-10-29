using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_wgwilber.Data;
using Fall2024_Assignment3_wgwilber.Models;
using System.Numerics;
using static System.Net.WebRequestMethods;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ClientModel.Primitives;
using System.Diagnostics;
using VaderSharp2;

namespace Fall2024_Assignment3_wgwilber.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;


        public MoviesController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<IActionResult> GetMoviePhoto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null || movie.Photo == null)
            {
                return NotFound();
            }

            var data = movie.Photo;
            return File(data, "image/jpg");
        }

        public async Task<string> CallReviewAPI(string movieTitle, string reviewer)
        {
            var endpointSecret = _config["OpenAi:Endpoint"] ?? throw new Exception("OpenAI:Endpoint does not exist in the current Configuration");
            var apiKeySecret = _config["OpenAi:ApiKey"] ?? throw new Exception("OpenAI:ApiKey does not exist in the current Configuration");

            var endpoint = new Uri(endpointSecret);
            var apiKey = new System.ClientModel.ApiKeyCredential(apiKeySecret);

            try
            {
                AzureOpenAIClient client = new(endpoint, apiKey);
                ChatClient chat = client.GetChatClient("gpt-35-turbo");

                ChatCompletion completion = await chat.CompleteChatAsync($"Write an honest review of the film {movieTitle} from the perspective of {reviewer}. " +
                    $"Use the character's mannerisms in the review and limit the review to four sentences. Begin the review with the character's name and a colon to indicate who is speaking.");
                
                Console.WriteLine(completion.Content[0].Text);
                return completion.Content[0].Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReview(int id, string reviewer)
        {
            var movie = await _context.Movie.FindAsync(id);

            if (movie == null)
            {
                return NotFound();
            }

            string generatedReviewText = await CallReviewAPI(movie.Title, reviewer);
            
            var analyzer = new SentimentIntensityAnalyzer();
            var sentimentResults = analyzer.PolarityScores(generatedReviewText);

            var review = new Review
            {
                ReviewText = generatedReviewText,
                CreatedAt = DateTime.Now,
                MovieId = id,
                SentimentScore = sentimentResults.Compound
            };

            _context.Review.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
        }

        public async Task<double> GetOverallSentiment(int id)
        {
            var reviews = await _context.Review
                .Where(r => r.MovieId == id)
                .ToListAsync();

            if (reviews.Count == 0) return 0;

            double result = reviews.Average(r => r.SentimentScore);

            return result;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            var actors = await _context.ActorMovie
                .Include(am => am.Actor)
                .Where(am => am.MovieId== movie.Id)
                .Select(am => am.Actor)
                .ToListAsync();

            var reviews = await _context.Review
                .Where(r => r.MovieId == movie.Id)
                .ToListAsync();

            double sentiment = await GetOverallSentiment(id.Value);

            var vm = new MovieDetailsViewModel(movie, actors, reviews, sentiment);

            return View(vm);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,IMDBLink,Genre,Year")] Movie movie, IFormFile Photo)
        {

            if (Photo == null || Photo.Length == 0)
            {
                return View(movie);
            }

            if (ModelState.IsValid)
            {
                using (var memoryStream = new MemoryStream())
                {
                    Photo.CopyTo(memoryStream);
                    movie.Photo = memoryStream.ToArray();
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,IMDBLink,Genre,Year")] Movie movie, IFormFile Photo)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        Photo.CopyTo(memoryStream);
                        movie.Photo = memoryStream.ToArray();
                    }

                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }
}
