namespace Bot.Core.Services;

/// <summary>
///     Configuration for speech transcription: which languages to auto-detect.
/// </summary>
public class SpeechOptions
{
    /// <summary>
    ///     A list of BCP-47 codes the TranscriptionService will try to auto-detect.
    ///     E.g. "en-US","ig-NG","yo-NG","ha-NG","pcm-NG" (Nigerian Pidgin).
    /// </summary>
    public string[] SupportedLanguages { get; set; } = new[]
    {
        "en-US",
        "ig-NG",
        "yo-NG",
        "ha-NG",
        "pcm-NG"
    };
}
