using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using Markdig;
using System;
using OpenAI_API;
using System.Text.RegularExpressions;
using System.Linq;

namespace AutoContentGenerator
{
    public class BlogPost {
        public string Title { get; set; }
        public string Content { get; set; }
    }
    public static class GenerateBlogPost
    {
        [FunctionName("GenerateBlogPost")]
        public static async Task Run([TimerTrigger("0 */8 * * *")] TimerInfo myTimer, ILogger log)
        {
        log.LogInformation("C# HTTP trigger function processed a request.");

            // Read GitHub configuration from environment variables
            string gitHubToken = System.Environment.GetEnvironmentVariable("GitHubToken");
            string repoOwner = System.Environment.GetEnvironmentVariable("GitHubRepoOwner");
            string repoName = System.Environment.GetEnvironmentVariable("GitHubRepoName");
            string postsDirectory = System.Environment.GetEnvironmentVariable("GitHubPostsDirectory");

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
            var blogPost = await WriteBlogPost(filesList);

            string markdownContent = blogPost.Content;
            string newFilePath = Path.Combine(clonePath, postsDirectory, blogPost.Title + ".md");
            await File.WriteAllTextAsync(newFilePath, markdownContent);

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
                var author = new LibGit2Sharp.Signature("GPT-Blog-Writer", System.Environment.GetEnvironmentVariable("GitHubEmail"), DateTimeOffset.Now);
                repo.Commit("Add new blog post", author, author);

                // Push the new branch
                var remote = repo.Network.Remotes["origin"];
                var options = new PushOptions { CredentialsProvider = (_, __, ___) => libgit2sharpCredentials };
                repo.Network.Push(remote, $"refs/heads/{newBranchName}", options);
            }

            // Create a pull request
            try
            {
                var pr = new NewPullRequest($"Add new blog post", newBranchName, "master");
                var email = gitHubClient.User.Email;
                var createdPr = await gitHubClient.PullRequest.Create(repoOwner, repoName, pr);

                return;
            }
            catch (Octokit.ApiValidationException ex)
            {
                log.LogError($"Validation failed: {ex.Message}");
                return;
            }


        }
        public static async Task<BlogPost> WriteBlogPost(string existingPosts)
        {
            OpenAIAPI api = new OpenAIAPI(System.Environment.GetEnvironmentVariable("OpenAIKey"));
            string prompt = @"
You are a blog writer for my blog on tea called Tea Treasury, at teatreasury.com
This is a blog all about tea - we cover all aspects essential and tangential related to tea, tea production, tea consumption, etc.
Feel free to be controversial in order to drive engagement.
Use markdown when you create the page.
Include frontmatter on your page in the following format:
---
title: ""<title>""
date: ""<date>""
author: ""Tea Treasury""
tags:
- ""<tag1>""
- ""<tag2>""
- ""<tag3>""
---
You will receive a list of past topics from the user, write a blog on a new topic not listed. Aim for 500+ words. Include a table or two to break up the solid text content.
";
            var chat = api.Chat.CreateConversation(new OpenAI_API.Chat.ChatRequest() { Model = "gpt-3.5-turbo" });
            chat.AppendSystemMessage(prompt);
            chat.AppendUserInput(existingPosts);
            string response = await chat.GetResponseFromChatbotAsync();

            string title = null;
            var frontMatterRegex = new Regex(@"---\s*(.*?)---", RegexOptions.Singleline);
            var match = frontMatterRegex.Match(response);

            if (match.Success)
            {
                string frontMatter = match.Groups[1].Value;
                var frontMatterLines = frontMatter.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in frontMatterLines)
                {
                    if (line.StartsWith("title:"))
                    {
                        title = line.Substring("title:".Length).Trim().Trim('"');
                        break;
                    }
                }
            }
            
            var blogPost = new BlogPost();
            blogPost.Title = ToKebabCase(title);
            blogPost.Content = response;
            return blogPost;
        }
        public static string ToKebabCase(string title)
        {
            var words = Regex.Split(title, @"\s+")
                .Select(word => Regex.Replace(word.ToLowerInvariant(), @"[^a-z-]", ""));
            return string.Join("-", words.Where(word => !string.IsNullOrWhiteSpace(word)));
        }
    }
}