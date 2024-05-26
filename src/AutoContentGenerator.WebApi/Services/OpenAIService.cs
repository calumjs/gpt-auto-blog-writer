using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace AutoContentGenerator.WebApi.Services;

public static class OpenAIService
{
    public static async Task<string> SendChatRequest(string apiKey, string content)
    {
        using var client = CreateClient(apiKey);

        var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            JObject responseObject = JObject.Parse(responseString);
            return responseObject["choices"][0]["message"]["content"].ToString();
        }
        else
        {
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
        }
    }

    public static async Task<string> GenerateImage(string apiKey, string prompt, string imageModel = "dall-e-3")
    {
        using var client = CreateClient(apiKey);

        var requestData = new
        {
            model = imageModel,
            prompt = "Photorealistic image: " + prompt,
            n = 1,
            size = "1024x1024",
        };

        var jsonString = JsonConvert.SerializeObject(requestData);
        var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/images/generations", httpContent);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            JObject responseObject = JObject.Parse(responseString);
            return responseObject["data"][0]["url"].ToString();
        }
        else
        {
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
        }
    }

    public static HttpClient CreateClient(string apiKey)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(600)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
