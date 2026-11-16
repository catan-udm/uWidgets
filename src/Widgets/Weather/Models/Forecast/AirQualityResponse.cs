using System.Text.Json.Serialization;

namespace Weather.Models.Forecast;

public class AirQualityResponse
{
    [JsonPropertyName("current")]
    public CurrentAirQuality Current { get; set; } = new();
    
    [JsonPropertyName("error")]
    public bool Error { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}