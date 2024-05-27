using AutoContentGenerator.WebApi.Models;
using AutoContentGenerator.WebApi.Services;
using LibGit2Sharp;
using Octokit;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoContentGenerator.WebApi.Endpoints;

public static class GenerateBlogPostEndpoint
{
    public static async Task<IResult> GenerateBlogPost(AppConfig appConfig)
    {
        if (appConfig?.GitHubConfig == null || appConfig.OpenAIConfig == null)
        {
            return TypedResults.BadRequest("GitHub or OpenAI configuration is missing.");
        }

        if (appConfig.GitHubConfig.GitHubToken.StartsWith("<<") || appConfig.OpenAIConfig.OpenAIApiKey.StartsWith("<<"))
        {
            return TypedResults.BadRequest("GitHub or OpenAI configuration is missing.");
        }

        // Read GitHub configuration from environment variables
        string gitHubToken = appConfig.GitHubConfig.GitHubToken;
        string repoOwner = appConfig.GitHubConfig.GitHubRepoOwner;
        string repoName = appConfig.GitHubConfig.GitHubRepoName;
        string gitHubUser = appConfig.GitHubConfig.GitHubEmail;
        string postsDirectory = appConfig.GitHubConfig.GitHubPostsDirectory;

        // Initialize GitHub client
        var gitHubClient = new GitHubClient(new ProductHeaderValue("AutoContentGenerator"))
        {
            Credentials = new Octokit.Credentials(repoOwner, gitHubToken)
        };

        // Clone the repository
        string repoUrl = $"https://github.com/{repoOwner}/{repoName}.git";
        string clonePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var libgit2sharpCredentials = new UsernamePasswordCredentials { Username = repoOwner, Password = gitHubToken };
        LibGit2Sharp.Repository.Clone(repoUrl, clonePath, new CloneOptions { CredentialsProvider = (_, __, ___) => libgit2sharpCredentials });

        string postsPath = Path.Combine(clonePath, postsDirectory);
        string[] filePaths = Directory.GetFiles(postsPath);
        string[] fileNames = filePaths.Select(Path.GetFileName).ToArray();
        string filesList = string.Join("\n", fileNames);

        // Generate the new Markdown file
        var blogPost = await WriteBlogPost(filesList, appConfig.OpenAIConfig);

        string markdownContent = blogPost.Content;
        string newFilePath = Path.Combine(clonePath, postsDirectory, blogPost.Title + ".md");
        await File.WriteAllTextAsync(newFilePath, markdownContent);

        // Download and save the image
        string imagesPath = Path.Combine(clonePath, "public/images/posts");
        string imageFileName = $"{blogPost.Title}.png";
        string savedImagePath = await DownloadAndSaveImage(blogPost.ImageURL, imageFileName, imagesPath);

        string newBranchName;
        // Commit the new file
        using (var repo = new LibGit2Sharp.Repository(clonePath))
        {
            // Create a new branch
            newBranchName = $"blog-post-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
            var branch = repo.CreateBranch(newBranchName);
            Commands.Checkout(repo, branch);

            // Add the new file and commit
            Commands.Stage(repo, newFilePath);
            Commands.Stage(repo, savedImagePath);
            var author = new LibGit2Sharp.Signature("GPT-Blog-Writer", gitHubUser, DateTimeOffset.Now);
            repo.Commit("Add new blog post", author, author);

            // Push the new branch
            var remote = repo.Network.Remotes["origin"];
            var options = new PushOptions { CredentialsProvider = (_, __, ___) => libgit2sharpCredentials };
            repo.Network.Push(remote, $"refs/heads/{newBranchName}", options);
        }

        // Create a pull request
        try
        {
            var pr = new NewPullRequest($"Add new blog post {blogPost.Title}", newBranchName, "main");
            var email = gitHubClient.User.Email;
            var createdPr = await gitHubClient.PullRequest.Create(repoOwner, repoName, pr);

            return TypedResults.Ok(createdPr.HtmlUrl);
        }
        catch (ApiValidationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }


    }
    public static async Task<BlogPost> WriteBlogPost(string existingPosts, OpenAIConfig openAIConfig)
    {
        string prompt = $$"""
You are a blog writer for my blog on tea called Tea Treasury, at teatreasury.com
This is a blog all about tea - we cover all aspects essential and tangential related to tea, tea production, tea consumption, etc.
Feel free to be controversial in order to drive engagement.
Use markdown when you create the page.
Do not put the title in an h1 tag at the start of the article, because it will be added separately via my blog page.
Use an occasional pun or thoughtful personal remark in the introduction or conclusion. Encourage people to engage with the discussion area under the post via various means.
Today's date is {{DateTime.Now:o}}.
Include frontmatter on your page in the following format:
---
title: "<title>"
excerpt: "<excerpt>"
coverImage: "/images/posts/<title>.png"
date: "{{DateTime.Now:o}}"
author:
  name: Tea Treasury
ogImage:
  url: "/images/posts/<title>.png"
---
You will receive a list of past topics from the user, write a blog on a brand new topic not listed. Do not repeat a topic already covered. Aim for 1000+ words. Include a table or two to break up the solid text content.
Reply with *only* the blog post and no additional explanatory details.
""";

        string chatRequest = $$"""
{
    "model": "{{openAIConfig.Model ?? "gpt-4o"}},
    "messages": [
      {
        "role": "system",
        "content": "{{prompt}}"
      },
      {
        "role": "user",
        "content": "{{existingPosts}}"
      }
    ]
}
""";

        string response = await OpenAIService.SendChatRequest(openAIConfig.OpenAIApiKey, chatRequest);

        string title = null;
        string kebabTitle = null;
        var frontMatterRegex = new Regex(@"---\s*(.*?)---", RegexOptions.Singleline);
        var match = frontMatterRegex.Match(response);

        if (match.Success)
        {
            string frontMatter = match.Groups[1].Value;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var frontMatterData = deserializer.Deserialize<Dictionary<string, object>>(frontMatter);

            title = frontMatterData["title"].ToString();
            kebabTitle = ToKebabCase(title);
            frontMatterData["coverImage"] = $"/public/images/posts/{kebabTitle}.png";
            var ogImage = frontMatterData["ogImage"] as Dictionary<object, object>;
            ogImage["url"] = $"/public/images/posts/{kebabTitle}.png";

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithEventEmitter(next => new QuotingEventEmitter(next))
                .Build();

            var updatedFrontMatter = serializer.Serialize(frontMatterData);
            response = frontMatterRegex.Replace(response, $"---\n{updatedFrontMatter}\n---", 1);
        }

        var imageUrl = await OpenAIService.GenerateImage(openAIConfig.OpenAIApiKey, title, openAIConfig.ImageModel);

        return new BlogPost
        {
            Title = kebabTitle,
            Content = response,
            ImageURL = imageUrl
        };
    }

    public static string ToKebabCase(string title)
    {
        var words = Regex.Split(title, @"\s+")
            .Select(word => Regex.Replace(word.ToLowerInvariant(), @"[^a-z-]", ""));
        return string.Join("-", words.Where(word => !string.IsNullOrWhiteSpace(word)));
    }

    public static async Task<string> DownloadAndSaveImage(string imageUrl, string fileName, string savePath)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(imageUrl);

        if (response.IsSuccessStatusCode)
        {
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(savePath);

            var filePath = Path.Combine(savePath, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);
            return filePath;
        }
        else
        {
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
        }
    }

    public class QuotingEventEmitter : ChainedEventEmitter
    {
        public QuotingEventEmitter(IEventEmitter nextEmitter) : base(nextEmitter)
        {
        }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            eventInfo.Style = ScalarStyle.DoubleQuoted;
            base.Emit(eventInfo, emitter);
        }
    }
}
