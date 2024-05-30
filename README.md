# Auto Content Generator

[![YouTube video](https://img.youtube.com/vi/zxxxrx5gH9g/0.jpg)](https://www.youtube.com/watch?v=zxxxrx5gH9g)  
**Original Video**

[![YouTube video](https://img.youtube.com/vi/W5nljACjvAc/0.jpg)](https://www.youtube.com/watch?v=W5nljACjvAc)  
**Feature: Interactive Review Process...Coming to the repo soon!**

Auto Content Generator is a .NET 8 application that uses OpenAI's GPT-3.5-turbo (or GPT-4) to generate new markdown formatted blog posts. The application clones a specified GitHub repository, generates a new Markdown file with the blog post content, commits the new file, and creates a pull request for the change. Additionally, it handles pull request reviews, updating content based on comments, and commits changes back to the repository.

## Environment Variables:
- `GitHubToken`: The personal access token for your GitHub account.
- `GitHubRepoOwner`: The owner of the GitHub repository.
- `GitHubRepoName`: The name of the GitHub repository.
- `GitHubEmail`: The email address of your GitHub account.
- `GitHubPostsDirectory`: The directory in the repository where the blog posts are stored.
- `OpenAIKey`: Your OpenAI API key.

## Clone your blog
Start here: [[https://vercel.com/?utm_source=github&utm_medium=readme&utm_campaign=next-example](https://vercel.com/new/clone?repository-url=https://github.com/vercel/next.js/tree/canary/examples/blog-starter&project-name=blog-starter&repository-name=blog-starter)](https://vercel.com/new/calumjs-projects/clone?repository-url=https%3A%2F%2Fgithub.com%2Fvercel%2Fnext.js%2Ftree%2Fcanary%2Fexamples%2Fblog-starter&project-name=blog-starter&repository-name=blog-starter)

## Usage:
1. **Set the required environment variables in the `appsettings.json` file or in the `appsettings.Development.json` file for development.**

Sample `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GitHub": {
    "GitHubToken": "<< PUT IN SECRETS OR 'appsettings.Development.json' >>",
    "GitHubRepoOwner": "<< PUT IN SECRETS OR 'appsettings.Development.json' >>",
    "GitHubRepoName": "<< PUT IN SECRETS OR 'appsettings.Development.json' >>",
    "GitHubEmail": "<< PUT IN SECRETS OR 'appsettings.Development.json' >>",
    "GitHubPostsDirectory": "_posts"
  },
  "OpenAI": {
    "OpenAIApiKey": "<< PUT IN SECRETS OR 'appsettings.Development.json' >>",
    "Model": "gpt-4",
    "ImageModel": "dall-e-3"
  }
}
```

2. Run the application.

Use the following command to run the application:

```bash
dotnet run
```

3. Generate a new blog post.

Make an HTTP POST request to the endpoint /generate-blog-post. This will give you a new pull request on your repo.

4. Respond to PR comments

Leave a comment asking for changes and submit your review, then call the endpoint /pr-webhook/{pullRequestNumber:int}. Your comments will be picked up and actioned.
