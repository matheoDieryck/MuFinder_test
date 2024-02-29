using Newtonsoft.Json;

namespace MuFinder.Models
{
	public class SearchResponse
{
    public Tracks Tracks { get; set; }
}

public class Tracks
{
    public TrackItem[] Items { get; set; }
}

public class TrackItem
{
    public string Name { get; set; }
    public int duration_ms { get; set; }
    public string Id { get; set; }
		// Add other properties as needed

		// Duration property to represent duration in seconds
		public string Duration
		{
			get
			{
				int seconds = duration_ms / 1000;
				int minutes = seconds / 60;
				seconds %= 60;
				return $"{minutes:00}:{seconds:00}";
			}
		}
	}

}

