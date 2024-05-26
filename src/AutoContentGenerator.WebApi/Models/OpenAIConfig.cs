namespace AutoContentGenerator.WebApi.Models;

public class OpenAIConfig
{
    public string OpenAIApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o";
    public string ImageModel { get; set; } = "dall-e-3";
}
