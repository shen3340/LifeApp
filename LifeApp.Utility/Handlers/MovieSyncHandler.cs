using HtmlAgilityPack;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Models;
using LifeApp.Utility.Interfaces;
using LifeApp.Utility.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LifeApp.Utility.Handlers
{
    public class MovieSyncHandler : IMovieSyncHandler
    {
        private readonly IMovieService _movieService;
        private readonly IMovieProviderService _movieProviderService;
        private readonly IMovieGenreService _movieGenreService;
        private readonly Settings _settings;
        private readonly ILogger<MovieSyncHandler> _logger;
        private readonly HttpClient _httpClient;
        private static readonly SemaphoreSlim _tmdbSemaphore = new(5);

        public MovieSyncHandler(
            IMovieService movieService,
            IMovieProviderService movieProviderService,
            IMovieGenreService movieGenreService,
            Settings settings,
            ILogger<MovieSyncHandler> logger)
        {
            _movieService = movieService;
            _movieProviderService = movieProviderService;
            _movieGenreService = movieGenreService;
            _settings = settings;
            _logger = logger;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            );
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://letterboxd.com/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _settings.TMDBApiKey.Replace("Bearer ", "").Trim()
                );
        }

        public async Task SyncWatchlistAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string watchlistUrl = _settings.LetterboxdWatchlistUrl;

                var letterboxdMovies = await ScrapeLetterboxdWatchlistAsync(watchlistUrl);
                _logger.LogInformation($"Scraped {letterboxdMovies.Count} movies from Letterboxd.");

                var tasks = letterboxdMovies.Select(async movie =>
                {
                    var tmdbInfo = await GetTmdbMovieInfoAsync(movie.MovieName);

                    if (tmdbInfo != null)
                    {
                        movie.Runtime = tmdbInfo.Value.runtime;
                        movie.PosterLink = tmdbInfo.Value.posterUrl;
                        movie.Providers = tmdbInfo.Value.providers?.Count > 0
                            ? tmdbInfo.Value.providers
                            : ["NOT FOUND"];
                        movie.Genres = tmdbInfo.Value.genres ?? [];
                    }
                    else
                    {
                        movie.Providers = ["NOT FOUND"];
                        movie.Genres = [];
                    }
                });

                await Task.WhenAll(tasks);

                var upsertResult = _movieService.UpsertMovies(letterboxdMovies);

                if (upsertResult.IsError)
                {
                    _logger.LogError("Movie upsert failed. Aborting sync.");
                    return;
                }

                var dbMovies = _movieService.GetAllMovies();

                var watchlistKeys = letterboxdMovies
                   .Select(movie => $"{movie.MovieName}_{movie.ReleaseYear}")
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var moviesToDelete = dbMovies
                    .Where(movie => !watchlistKeys.Contains($"{movie.MovieName}_{movie.ReleaseYear}"))
                    .ToList();

                if (moviesToDelete.Count != 0)
                {
                    _movieService.BulkDeleteMovies(moviesToDelete);
                }

                dbMovies = _movieService.GetAllMovies();

                var movieIds = dbMovies.Select(movie => movie.Id).ToList();

                _movieProviderService.TruncateMovieProviders();
                _movieGenreService.TruncateMovieGenres();

                var movieLookup = dbMovies.ToDictionary(
                    movie => $"{movie.MovieName}_{movie.ReleaseYear}",
                    StringComparer.OrdinalIgnoreCase);

                var providersToInsert = new List<MovieProvider>();
                var genresToInsert = new List<MovieGenre>();

                foreach (var movie in letterboxdMovies)
                {
                    var key = $"{movie.MovieName}_{movie.ReleaseYear}";

                    if (!movieLookup.TryGetValue(key, out var dbMovie))
                        continue;

                    providersToInsert.AddRange(
                        movie.Providers.Select(provider => new MovieProvider
                        {
                            MovieId = dbMovie.Id,
                            ProviderName = provider
                        })
                    );

                    genresToInsert.AddRange(
                        movie.Genres.Select(genre => new MovieGenre
                        {
                            MovieId = dbMovie.Id,
                            GenreName = genre
                        })
                    );
                }

                _movieProviderService.BulkInsertMovieProviders(providersToInsert);
                _movieGenreService.BulkInsertMovieGenres(genresToInsert);

                stopwatch.Stop();
                _logger.LogInformation($"Letterboxd watchlist sync complete in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing Letterboxd watchlist: {ex.Message}");
                throw;
            }
        }

        private async Task<List<SDK.Models.Movie>> ScrapeLetterboxdWatchlistAsync(string watchlistUrl)
        {
            var movies = new List<SDK.Models.Movie>();
            int page = 1;

            while (true)
            {
                var url = $"{watchlistUrl}/page/{page}/";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) 
                    break;

                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var items = doc.DocumentNode.SelectNodes("//li[contains(@class,'griditem')]");
                
                if (items == null || items.Count == 0) 
                    break;

                foreach (var item in items)
                {
                    var lazyPoster = item.SelectSingleNode(".//div[@data-component-class='LazyPoster']");
                    
                    if (lazyPoster == null) 
                        continue;

                    var nameAttr = lazyPoster.GetAttributeValue("data-item-name", null);
                    var decodedName = WebUtility.HtmlDecode(nameAttr);

                    var lastParen = decodedName.LastIndexOf('(');
                    int releaseYear = 0;
                    string movieName = decodedName;

                    if (lastParen > 0)
                    {
                        movieName = decodedName[..lastParen].Trim();
                        int.TryParse(decodedName[(lastParen + 1)..].TrimEnd(')'), out releaseYear);
                    }

                    movies.Add(new SDK.Models.Movie
                    {
                        MovieName = movieName,
                        ReleaseYear = releaseYear
                    });
                }

                page++;
            }

            return movies;
        }

        private async Task<(int? runtime, string posterUrl, List<string> providers, List<string> genres)?> GetTmdbMovieInfoAsync(string movieName)
        {
            await _tmdbSemaphore.WaitAsync();
            try
            {
                var searchUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(movieName)}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (!response.IsSuccessStatusCode) 
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var results = json["results"];

                if (results == null || !results.Any()) 
                    return null;

                var match = results.FirstOrDefault(r => string.Equals(r["title"]?.ToString(), movieName, StringComparison.OrdinalIgnoreCase))
                            ?? results.First();

                var movieId = match["id"]?.ToString();

                if (string.IsNullOrEmpty(movieId)) 
                    return null;

                int? runtime = null;
                string posterUrl = "https://s.ltrbxd.com/static/img/empty-poster-125-AiuBHVCI.png";
                var providers = new List<string>();
                var genres = new List<string>();

                var detailsUrl = $"https://api.themoviedb.org/3/movie/{movieId}";
                var detailsResp = await _httpClient.GetAsync(detailsUrl);
                
                if (detailsResp.IsSuccessStatusCode)
                {
                    var detailsJson = JObject.Parse(await detailsResp.Content.ReadAsStringAsync());
                    runtime = detailsJson["runtime"]?.ToObject<int?>();
                    genres = detailsJson["genres"]?.Select(JToken => JToken["name"].ToString()).ToList() ?? [];
                }

                var providersUrl = $"https://api.themoviedb.org/3/movie/{movieId}/watch/providers";
                var providersResp = await _httpClient.GetAsync(providersUrl);
                
                if (providersResp.IsSuccessStatusCode)
                {
                    var providersJson = JObject.Parse(await providersResp.Content.ReadAsStringAsync());
                    var flatrate = providersJson["results"]?["US"]?["flatrate"];
                    providers = flatrate != null
                        ? [.. flatrate.Select(jToken => jToken["provider_name"].ToString()).Distinct()]
                        : ["NOT FOUND"];
                }

                var movieUrl = $"https://api.themoviedb.org/3/movie/{movieId}?language=en&api_key={_settings.TMDBApiKey}";
                var movieResp = await _httpClient.GetAsync(movieUrl);

                if (movieResp.IsSuccessStatusCode)
                {
                    var movieJson = JObject.Parse(await movieResp.Content.ReadAsStringAsync());
                    var posterPath = movieJson["poster_path"]?.ToString();
                    if (!string.IsNullOrEmpty(posterPath))
                    {
                        posterUrl = $"https://image.tmdb.org/t/p/w185{posterPath}";
                    }
                }

                return (runtime, posterUrl, providers, genres);
            }
            finally
            {
                _tmdbSemaphore.Release();
            }
        }
    }
}