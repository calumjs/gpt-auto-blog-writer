# Auto Content Generator

Auto Content Generator is an Azure Function written in C# that uses OpenAI's GPT-3.5-turbo to generate new markdown formatted blog posts. I have left my prompt in there to show you what I did, but obviously you will need to update it to meet the requirements of your blog. The function clones a specified GitHub repository, generates a new Markdown file with the blog post content, commits the new file, and creates a pull request for the change.

## Environment Variables:
- GitHubToken: The personal access token for your GitHub account.
- GitHubRepoOwner: The owner of the GitHub repository.
- GitHubRepoName: The name of the GitHub repository.
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
    "GitHubPostsDirectory": "<posts_directory>",
    "OpenAIKey": "<your_openai_key>"
  }
}
```

2. Deploy the function to Azure.
3. The function will run every 8 hours, generating a new blog post and creating a pull request in the specified repository.