# Auto Content Generator

[![YouTube video](https://img.youtube.com/vi/zxxxrx5gH9g/0.jpg)](https://www.youtube.com/watch?v=zxxxrx5gH9g)  
**Original Video**

[![YouTube video](https://img.youtube.com/vi/W5nljACjvAc/0.jpg)](https://www.youtube.com/watch?v=W5nljACjvAc)  
**Feature: Interactive Review Process...Coming to the repo soon!**

Auto Content Generator is an Azure Function written in C# that uses OpenAI's GPT-3.5-turbo to generate new markdown formatted blog posts. I have left my prompt in there to show you what I did, but obviously you will need to update it to meet the requirements of your blog. The function clones a specified GitHub repository, generates a new Markdown file with the blog post content, commits the new file, and creates a pull request for the change.

## Environment Variables:
- GitHubToken: The personal access token for your GitHub account.
- GitHubRepoOwner: The owner of the GitHub repository.
- GitHubRepoName: The name of the GitHub repository.
- GitHubEmail: The email address of your GitHub account.
- GitHubPostsDirectory: The directory in the repository where the blog posts are stored.
- OpenAIKey: Your OpenAI API key.

## Usage:
1. Set the required environment variables in the `local.settings.json` file.

Sample `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "GitHubToken": "<your_github_token>",
    "GitHubRepoOwner": "<repository_owner>",
    "GitHubRepoName": "<repository_name>",
    "GitHubEmail": "<github_email>",
    "GitHubPostsDirectory": "<posts_directory>",
    "OpenAIKey": "<your_openai_key>"
  }
}
```

2. Deploy the function to Azure.
3. Call the HTTP trigger whenever you want a new blog post
4. Set up the GitHub webhook...the event is pull_request_review and the payload URL will be https://<function app name>.azurewebsites.net/api/HandlePullRequestComment?code=<function code>
