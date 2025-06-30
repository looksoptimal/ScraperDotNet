namespace ScrapperDotNet.Ai
{
    /// <summary>
    /// Interface for AI client implementations
    /// </summary>
    public interface IAiClient
    {
        /// <summary>
        /// Sends a message to the AI model and gets a response
        /// </summary>
        /// <param name="message">The user's message to send to the model</param>
        /// <returns>The model's response text</returns>
        Task<string> GetResponseAsync(string message);

        /// <summary>
        /// Creates a chat completion with a system message for more control over the conversation
        /// </summary>
        /// <param name="systemMessage">Instructions for the AI model</param>
        /// <param name="userMessage">The user's message</param>
        /// <returns>The model's response text</returns>
        Task<string> GetChatResponseAsync(string systemMessage, string userMessage);
        Task<string> AskAboutImageAsync(string userMessage, string imagePath);
    }
}
