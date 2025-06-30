using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;

namespace ScraperDotNet.Ai
{
    /// <summary>
    /// Client for interacting with Ollama AI models using Semantic Kernel
    /// </summary>
    public class OllamaClient : IAiClient
    {
        private readonly Kernel _kernel;
        private readonly ILogger<OllamaClient> _logger;
        private readonly string _modelName;

        public OllamaClient(AppSettings appSettings, ILogger<OllamaClient> logger)
        {
            _logger = logger;

            if (appSettings.AiEnabled)
            {
                _modelName = appSettings.OllamaModelName;

                // Create the kernel builder
#pragma warning disable SKEXP0070
                var kernelBuilder = Kernel.CreateBuilder();
                var ollamaUri = new Uri(appSettings.OllamaEndpoint);
                // Configure the Ollama text generation service
                kernelBuilder.AddOllamaTextGeneration(
                    modelId: _modelName,
                    endpoint: ollamaUri);

                _kernel = kernelBuilder.Build();

                _logger.LogInformation("Initialized Ollama client with model: {ModelName}", _modelName);
            }
            else
            {
                _logger.LogInformation("AI settings are absent, Ollama client not initialized.");
            }
        }

        /// <summary>
        /// Sends a message to the AI model and gets a response
        /// </summary>
        /// <param name="message">The user's message to send to the model</param>
        /// <returns>The model's response text</returns>
        public async Task<string> GetResponseAsync(string message)
        {
            try
            {
                _logger.LogInformation("Sending message to Ollama model: {ModelName}", _modelName);

                // Get the response from the model
                var result = await _kernel.InvokePromptAsync(message);

                _logger.LogInformation("Received response from Ollama");
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting response from Ollama model");
                return $"Error communicating with AI model: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a chat completion with a system message for more control over the conversation
        /// </summary>
        /// <param name="systemMessage">Instructions for the AI model</param>
        /// <param name="userMessage">The user's message</param>
        /// <returns>The model's response text</returns>
        public async Task<string> GetChatResponseAsync(string systemMessage, string userMessage)
        {
            try
            {
                _logger.LogInformation("Sending chat message to Ollama model: {ModelName}", _modelName);

                // Structure the prompt with system and user messages
                var prompt = $"<system>{systemMessage}</system>\n<user>{userMessage}</user>";

                // Get the response from the model
                var result = await _kernel.InvokePromptAsync(prompt);

                _logger.LogInformation("Received chat response from Ollama");
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response from Ollama model");
                return $"Error communicating with AI model: {ex.Message}";
            }
        }

        public async Task<string> AskAboutImageAsync(string userMessage, string imagePath)
        {
            try
            {
                _logger.LogInformation("Sending image analysis request to Ollama model: {ModelName}", _modelName);

                // Get the full path to the Python script
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ai", "askOllamaVisionModel.py");
                
                // Prepare the process start info
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" --model \"{_modelName}\" --image \"{imagePath}\" --prompt \"{userMessage}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process
                using var process = new System.Diagnostics.Process { StartInfo = startInfo };
                process.Start();

                // Read the output and error asynchronously
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Python script failed with error: {error}");
                }

                _logger.LogInformation("Received image analysis response from Ollama");
                return output.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image analysis from Ollama model");
                return $"Error analyzing image with AI model: {ex.Message}";
            }
        }
    }
}
