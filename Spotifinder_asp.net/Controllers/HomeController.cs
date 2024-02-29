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
					return tokenResponse.AccessToken;
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
		public class TokenResponse
		{
			[JsonProperty("access_token")]
			public string AccessToken { get; set; }
		}
	}
}
