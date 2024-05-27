namespace AutoContentGenerator.WebApi.Models;

public class GitHubConfig
{
    public string GitHubToken { get; set; }
    public string GitHubRepoOwner { get; set; }
    public string GitHubRepoName { get; set; }
    public string GitHubEmail { get; set; }
    public string GitHubPostsDirectory { get; set; }
}
