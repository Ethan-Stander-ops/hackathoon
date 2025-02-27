using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Hackathon.Server.Models;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Net.Mail;
using System.Net;



namespace Hackathon.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemsController : ControllerBase
    {
        // Loaded articles from DI
        private readonly List<KnowledgeArticle> _articles;
        private readonly IConfiguration _configuration;

        public ProblemsController(
            IConfiguration configuration,
            List<KnowledgeArticle> articles // from DI
        )
        {
            _configuration = configuration;
            _articles = articles;
        }

        // DTO for problem submissions
        public class ProblemSubmission
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Email { get; set; } // For notification
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitProblem([FromBody] ProblemSubmission submission)
        {
            KnowledgeArticle bestMatch = null;
            foreach (var article in _articles)
            {
                // Evaluate relevance using Azure OpenAI with chunking logic.
                bool isRelevant = await EvaluateRelevance(submission, article);
                if (isRelevant)
                {
                    bestMatch = article;
                    break;
                }
            }

            if (bestMatch != null)
            {
                await SendEmailNotification(submission.Email, bestMatch);
                return Ok(new
                {
                    message = "Problem matched and email notification sent.",
                    article = bestMatch
                });
            }
            else
            {
                return NotFound(new { message = "No relevant article found." });
            }
        }

        /// <summary>
        /// Uses the Azure OpenAI ChatClient with chunking logic to evaluate if an article is relevant.
        /// </summary>
        private async Task<bool> EvaluateRelevance(ProblemSubmission submission, KnowledgeArticle article)
        {
            // Retrieve configuration values (ensure these keys are set in your configuration)
            var baseEndpoint = _configuration["Endpoint"]?.TrimEnd('/');
            var deploymentId = _configuration["Model"]; // deployment name
            var apiKey = _configuration["APIKey"];

            if (string.IsNullOrWhiteSpace(baseEndpoint) ||
                string.IsNullOrWhiteSpace(deploymentId) ||
                string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Missing required OpenAI configuration values.");
            }

            // Create the AzureOpenAIClient and get a ChatClient for your deployment.
            AzureOpenAIClient azureClient = new(
                new Uri(baseEndpoint),new ApiKeyCredential(apiKey)
               );
            ChatClient chatClient = azureClient.GetChatClient(deploymentId);


            var prompt = $"Determine if the following article title is relevant to the problem. It does not need to be exactly the same—it just needs to have some relevance.\n\n" +
                $"Problem Title: {submission.Title}\n\n" +
                $"Article Title: {article.Title}\n\n" +
                "Answer with 'Yes' or 'No'. If your answer is complete, add after the answer 'There are no more chunks remaining.'";



            // Prepare the initial messages.
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an assistant that determines title relevance. If the titles are somewhat related you need to return a yes. It only needs a little bit of relevance."),
                new UserChatMessage(prompt)
            };

            // Use chunking logic to retrieve a full response.
            string fullResponse = await ChatWithChunking(chatClient, messages, maxChunkAttempts: 3);

            // For this demo, if the concatenated response contains "Yes", consider it relevant.
            return fullResponse.Contains("Yes", StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost("trends")]
        public async Task<IActionResult> GetTrends([FromBody] List<ProblemSubmission> problems)
        {
            if (problems == null || !problems.Any())
            {
                return BadRequest(new { message = "No problems provided." });
            }

            // Combine problem titles and descriptions into a single text block.
            string problemsText = string.Join("\n", problems.Select(p => $"Title: {p.Title}\nDescription: {p.Description}\n"));

            // Build the prompt to identify common themes.
            var prompt = "Analyze the following list of problems and identify any common themes or trends. " +
                         "List the common issues succinctly.\n\n" +
                         problemsText;

            // Retrieve configuration values.
            var baseEndpoint = _configuration["Endpoint"]?.TrimEnd('/');
            var deploymentId = _configuration["Model"];
            var apiKey = _configuration["APIKey"];

            if (string.IsNullOrWhiteSpace(baseEndpoint) ||
                string.IsNullOrWhiteSpace(deploymentId) ||
                string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Missing required OpenAI configuration values.");
            }

            // Create the AzureOpenAIClient and get a ChatClient for your deployment.
            AzureOpenAIClient azureClient = new(
                new Uri(baseEndpoint),
                new AzureKeyCredential(apiKey)
            );
            ChatClient chatClient = azureClient.GetChatClient(deploymentId);

            // Prepare the messages.
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant that identifies common trends among a list of problems."),
                new UserChatMessage(prompt)
            };

            // Optionally, if you expect a long response you could implement chunking here too.
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages.ToArray());
            string trends = completion.Content[0].Text;

            return Ok(new { trends });
        }

        /// <summary>
        /// Implements chunking logic by repeatedly calling CompleteChat until a termination phrase is received or maximum attempts reached.
        /// </summary>
        private async Task<string> ChatWithChunking(ChatClient chatClient, List<ChatMessage> messages, int maxChunkAttempts = 3)
        {
            int attemptCount = 0;
            string outputResult = "";
            bool done = false;

            while (!done)
            {
                attemptCount++;

                // Call the chat completions endpoint.
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages.ToArray());
                // Here, we assume that the response's first message text is available in Content[0].Text.
                string suggestion = completion.Content[0].Text;

                // Check for a termination phrase.
                if (suggestion.Contains("There are no more chunks remaining.", StringComparison.OrdinalIgnoreCase))
                {
                    done = true;
                    outputResult += suggestion;
                }
                else
                {
                    outputResult += suggestion;
                    // Add the assistant's message and prompt for the next chunk.
                    messages.Add(new AssistantChatMessage(suggestion));
                    messages.Add(new UserChatMessage("next chunk"));
                }

                if (attemptCount >= maxChunkAttempts)
                {
                    done = true;
                }
            }

            return string.IsNullOrWhiteSpace(outputResult) ? "" : outputResult;
        }

        // Dummy method to simulate sending an email notification.
        private async Task SendEmailNotification(string email, KnowledgeArticle article)
        {
            // Configure Gmail SMTP settings.
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUser = "ethanstander55@gmail.com"; 
            string smtpPass = "orjq fffz jqkw epzx"; 

            // Create the email message.
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(smtpUser);
                message.To.Add(email);
                message.Subject = "Relevant Article Recommendation";
                message.Body = $"Hello,\n\nWe found a relevant article for your problem:\n\n" +
                               $"Title: {article.Title}\n" +
                               $"Link: {article.Url}\n\n" +
                               "Best regards,\nYour Support Team";
                message.IsBodyHtml = false;

                // Configure the SMTP client.
                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    client.EnableSsl = true; // Gmail requires SSL.

                    // Send the email asynchronously.
                    await client.SendMailAsync(message);
                }
            }
        }
        }
}
