using System.Text.Json.Serialization;

namespace Weather.Models.Geocoding;

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<City> Cities { get; set; }
    
    [JsonPropertyName("error")]
    public bool Error { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; }
}