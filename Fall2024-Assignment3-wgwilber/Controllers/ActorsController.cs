using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_wgwilber.Data;
using Fall2024_Assignment3_wgwilber.Models;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using VaderSharp2;

namespace Fall2024_Assignment3_wgwilber.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ActorsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<IActionResult> GetActorPhoto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null || actor.Photo == null)
            {
                return NotFound();
            }

            var data = actor.Photo;
            return File(data, "image/jpg");
        }

        public async Task<string> CallTweetAPI(string actorName, string tweeter)
        {
            var endpointSecret = _config["OpenAi:Endpoint"] ?? throw new Exception("OpenAI:Endpoint does not exist in the current Configuration");
            var apiKeySecret = _config["OpenAi:ApiKey"] ?? throw new Exception("OpenAI:ApiKey does not exist in the current Configuration");

            var endpoint = new Uri(endpointSecret);
            var apiKey = new System.ClientModel.ApiKeyCredential(apiKeySecret);

            try
            {
                AzureOpenAIClient client = new(endpoint, apiKey);
                ChatClient chat = client.GetChatClient("gpt-35-turbo");

                ChatCompletion completion = await chat.CompleteChatAsync($"Write an opinionated tweet about the actor {actorName} from the perspective of {tweeter}. " +
                    $"The tweet can be an opinion about the actor's appearance, personality, or performance in a particular film." +
                    $"Use the character's mannerisms in the review and limit the review to three sentences. Begin the review with the character's name and a colon to indicate who is speaking.");

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
        public async Task<IActionResult> GenerateTweet(int id, string tweeter)
        {
            var actor = await _context.Actor.FindAsync(id);

            if (actor == null)
            {
                return NotFound();
            }

            string generatedTweetText = await CallTweetAPI(actor.Name, tweeter);

            var analyzer = new SentimentIntensityAnalyzer();
            var sentimentResults = analyzer.PolarityScores(generatedTweetText);

            var tweet = new Tweet
            {
                TweetText = generatedTweetText,
                CreatedAt = DateTime.Now,
                ActorId = id,
                SentimentScore = sentimentResults.Compound
            };

            _context.Tweet.Add(tweet);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
        }
        public async Task<double> GetOverallSentiment(int id)
        {
            var tweets = await _context.Tweet
                .Where(t => t.ActorId == id)
                .ToListAsync();

            if (tweets.Count == 0) return 0;

            double result = tweets.Average(r => r.SentimentScore);

            return result;
        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            var movies = await _context.ActorMovie
                .Include(am => am.Movie)
                .Where(am => am.ActorId == actor.Id)
                .Select(am => am.Movie)
                .ToListAsync();

            var tweets = await _context.Tweet
                .Where(t => t.ActorId == actor.Id)
                .ToListAsync();

            double sentiment = await GetOverallSentiment(id.Value);

            var vm = new ActorDetailsViewModel(actor, movies, tweets, sentiment);

            return View(vm);
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Gender,Age,IMDBLink")] Actor actor, IFormFile Photo)
        {
            if (Photo == null || Photo.Length == 0)
            {
                return View(actor);
            }

            if (ModelState.IsValid)
            {
                using (var memoryStream = new MemoryStream())
                {
                    Photo.CopyTo(memoryStream);
                    actor.Photo = memoryStream.ToArray();
                }
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }


        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,Age,IMDBLink")] Actor actor, IFormFile Photo)
        {
            if (id != actor.Id)
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
                        actor.Photo = memoryStream.ToArray();
                    }
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.Id == id);
        }
    }
}
