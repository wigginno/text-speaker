using Microsoft.CognitiveServices.Speech;

namespace TextSpeaker.Models;

/// <summary>
/// Wraps a <see cref="VoiceInfo"/> with a friendly <see cref="DisplayName"/> for UI binding
/// while still exposing core properties needed elsewhere in the codebase.
/// </summary>
public record VoiceDisplayItem(VoiceInfo Voice, string DisplayName)
{
    // Convenience forwarders so existing code can still access Name/Locale without refactor turbulence
    public string Name => Voice.Name;
    public string ShortName => Voice.ShortName;
    public string Locale => Voice.Locale;
    public Microsoft.CognitiveServices.Speech.SynthesisVoiceType VoiceType => Voice.VoiceType;

    public override string ToString() => DisplayName;
}
