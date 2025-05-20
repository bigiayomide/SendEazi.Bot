using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Bot.Core.Services;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;

namespace Bot.Tests.Services;

public class VoicePickerTests
{
    private static VoiceInfo CreateVoice(string locale, string shortName)
    {
        var type = typeof(VoiceInfo);
        var obj = (VoiceInfo)RuntimeHelpers.GetUninitializedObject(type);
        type.GetProperty("Locale")?.SetValue(obj, locale);
        type.GetProperty("ShortName")?.SetValue(obj, shortName);
        return obj;
    }

    [Fact]
    public void PickVoice_Should_Return_Exact_Match()
    {
        var voices = new[]
        {
            CreateVoice("en-US", "en-US-A"),
            CreateVoice("fr-FR", "fr-FR-B")
        };
        var picker = new VoicePicker(voices);

        var result = picker.PickVoice("fr-FR");

        result.Should().Be("fr-FR-B");
    }

    [Fact]
    public void PickVoice_Should_Use_Prefix_When_No_Exact()
    {
        var voices = new[]
        {
            CreateVoice("fr-CA", "fr-CA-A")
        };
        var picker = new VoicePicker(voices);

        var result = picker.PickVoice("fr-FR");

        result.Should().Be("fr-CA-A");
    }

    [Fact]
    public void PickVoice_Should_Fallback_When_No_Match()
    {
        var voices = new[]
        {
            CreateVoice("de-DE", "de-DE-A")
        };
        var picker = new VoicePicker(voices);

        var result = picker.PickVoice("yo-NG");

        result.Should().Be("en-US-JennyNeural");
    }
}