// Bot.Infrastructure.Configuration/FormRecognizerOptions.cs

namespace Bot.Infrastructure.Configuration;

public class FormRecognizerOptions
{
    /// <summary>Endpoint like "https://your-form-recognizer.cognitiveservices.azure.com/".</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>API Key for the Form Recognizer resource.</summary>
    public string ApiKey { get; set; } = null!;
}