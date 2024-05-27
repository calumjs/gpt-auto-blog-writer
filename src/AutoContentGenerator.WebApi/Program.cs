using AutoContentGenerator.WebApi.Endpoints;
using AutoContentGenerator.WebApi.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

GitHubConfig ghConfig = builder.Configuration.GetSection("GitHub").Get<GitHubConfig>();
OpenAIConfig aiConfig = builder.Configuration.GetSection("OpenAI").Get<OpenAIConfig>();

AppConfig appConfig = new AppConfig
{
    GitHubConfig = ghConfig,
    OpenAIConfig = aiConfig
};

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("generate-blog-post", () => GenerateBlogPostEndpoint.GenerateBlogPost(appConfig))
    .WithName("GenerateBlogPost")
    .WithDescription("This will create a new PR on GitHub with a blog post Markdown and an image.")
    .Produces<string>(StatusCodes.Status200OK)
    .Produces<string>(StatusCodes.Status400BadRequest)
    .WithOpenApi();

app.MapPost("pr-webhook/{pullRequestNumber:int}", async (int pullRequestNumber) =>
    await HandlePullRequestWebhookEndpoint.HandleWebhook(pullRequestNumber, appConfig))
    .WithName("HandleGitHubWebHook")
    .WithDescription("This will react to GitHub review comments in PRs.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces<string>(StatusCodes.Status400BadRequest)
    .WithOpenApi();

app.Run();
