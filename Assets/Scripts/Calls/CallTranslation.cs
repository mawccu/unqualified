/// <summary>
/// Whether an English line may appear under something a customer said, and why.
/// There is exactly one source of free translation: the accessibility setting
/// "Translated call captions" (Settings, off by default), which works without any
/// gadget and is labelled as a caption. Claiming a language on the CV only routes
/// calls; nobody on shift speaking it only opens the phrasebook (general terms);
/// the WhisperBox glosses the clue's key words in its own strip and is not a
/// translator. Without captions, customers' lines stay in their language.
/// </summary>
public static class CallTranslation
{
    public enum Source { None, Captions }

    public static Source For(TranscriptLine line, bool captionsSetting)
    {
        if (line == null || line.Speaker != "customer" || line.Language == CallLanguage.English)
            return Source.None;
        return captionsSetting ? Source.Captions : Source.None;
    }

    /// The label shown in front of a translated line.
    public static string Label(Source source) => source == Source.Captions ? "Caption: " : "";
}
