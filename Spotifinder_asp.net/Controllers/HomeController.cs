using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MuFinder.Models;
using System.Text;

namespace MuFinder.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

        public IActionResult Index()
        {
            string? profilePictureUrl = HttpContext.Session.GetString("ProfilePictureUrl");
            string? userName = HttpContext.Session.GetString("UserName");

            ViewBag.ProfilePictureUrl = profilePictureUrl;
            ViewBag.UserName = userName;
            return View();
        }
        [HttpPost]
		public async Task<IActionResult> Search(string query)
		{
			// Replace these with your own credentials
			string clientId = "9311a75e1a1b4fcf8736daf9f00ad5ca";
			string clientSecret = "cf73887ee0f5401dbfb60d4839593972";

			string accessToken = await GetAccessToken(clientId, clientSecret);
			if (!string.IsNullOrEmpty(accessToken))
			{
				var searchResults = await SearchTrack(query, accessToken);
				if (searchResults != null)
				{
					return View("SearchResults", searchResults.Tracks.Items);
				}
			}

			return View("Index");
		}

        static async Task<User> createUser(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync("https://api.spotify.com/v1/me");
                if (response.IsSuccessStatusCode)
                {
                    var user = JsonConvert.DeserializeObject<User>(await response.Content.ReadAsStringAsync());
                    return user;
                }
                else
                {
                    return null;
                }
            }
        }

		static async Task<string> GetAccessToken(string clientId, string clientSecret)
		{
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
					Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

				var content = new FormUrlEncodedContent(new[]
				{
					new KeyValuePair<string, string>("grant_type", "client_credentials"),
				});

				var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
				if (response.IsSuccessStatusCode)
				{
					var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
					await createUser(tokenResponse.accessToken);
					return tokenResponse.accessToken;
				}
				else
				{
					return null;
				}
			}
		}

		static async Task<SearchResponse> SearchTrack(string query, string accessToken)
		{
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var response = await client.GetAsync($"https://api.spotify.com/v1/search?q={query}&type=track");
				if (response.IsSuccessStatusCode)
				{
					return JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());
				}
				else
				{
					return null;
				}
			}
		}
        public IActionResult Login()
        {
            // Replace these with your own credentials
            string clientId = "9311a75e1a1b4fcf8736daf9f00ad5ca";
            string scheme = Request?.Scheme ?? "http";
            string redirectUri = Url.Action("Callback", "Home", null, scheme);

            // The scopes your app needs access to
            string[] scopes = new[] { "user-read-private", "user-read-email" };

            // Generate the URL for the Spotify Accounts service's Authorization endpoint
            string authUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(string.Join(" ", scopes))}";

            // Redirect the user to the Spotify login page
            return Redirect(authUrl);
        }

        public async Task<IActionResult> Callback(string code)
        {
            // Replace these with your own credentials
            string clientId = "9311a75e1a1b4fcf8736daf9f00ad5ca";
            string clientSecret = "cf73887ee0f5401dbfb60d4839593972";
            string redirectUri = Url.Action("Callback", "Home", null, Request.Scheme);

            // Exchange the authorization code for an access token and refresh token
            string tokenUrl = "https://accounts.spotify.com/api/token";
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            });

            var httpClient = new HttpClient();
            var tokenResponse = await httpClient.SendAsync(tokenRequest);
            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResponseBody);

            // Save the access token and refresh token somewhere safe (like a user session)
            string accessToken = tokenData["access_token"];
            string refreshToken = tokenData["refresh_token"];

            var user = await createUser(accessToken);
            if (user != null && user.Images != null && user.Images.Count > 0)
            {
                HttpContext.Session.SetString("UserName", user.DisplayName);
                HttpContext.Session.SetString("ProfilePictureUrl", user.Images[0].Url);
            } else if (user != null)
            {
                HttpContext.Session.SetString("UserName", user.DisplayName);
            }

            // Redirect the user to the home page, or wherever you want them to go after logging in
            return RedirectToAction("Index");
        }

        public class TokenResponse
		{
			[JsonProperty("access_token")]
			public string accessToken { get; set; }
		}

	}
}
