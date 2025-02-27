
using Hackathon.Server.Models;
using System.Text.Json;

namespace Hackathon.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Configuration.AddUserSecrets<Program>();
            builder.Services.AddHttpClient();

            var knowledgeArticles = new List<KnowledgeArticle>();
            var problemsFolderPath = Path.Combine(Environment.CurrentDirectory, "Problems");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policyBuilder =>
                {
                    policyBuilder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });


            // Make sure the folder exists and contains .json files
            if (Directory.Exists(problemsFolderPath))
            {
                var jsonFiles = Directory.GetFiles(problemsFolderPath, "*.json");
                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var fileContent = File.ReadAllText(filePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true
                        };
                        var article = JsonSerializer.Deserialize<KnowledgeArticle>(fileContent, options);
                        if (article != null)
                        {
                            knowledgeArticles.Add(article);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Problems folder not found at {problemsFolderPath}");
            }
            builder.Services.AddSingleton(knowledgeArticles);

            var app = builder.Build();

            app.UseCors("AllowAll");



            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
