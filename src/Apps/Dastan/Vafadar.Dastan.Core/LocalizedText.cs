using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>Whether a text has been reviewed by the editors.</summary>
public enum TextReview
{
    /// <summary>The text is part of the reviewed editorial content.</summary>
    Reviewed = 0,

    /// <summary>The text is a draft that still waits for review, for example a new translation.</summary>
    Draft = 1,
}

/// <summary>One text in one language.</summary>
/// <param name="Value">The text.</param>
/// <param name="Review">Whether it has been reviewed.</param>
public readonly record struct TextVariant(string Value, TextReview Review);

/// <summary>A text as a visitor gets it: the wording, the language it is in, and whether that is what was asked for.</summary>
/// <param name="Value">The text.</param>
/// <param name="Language">The language of <paramref name="Value"/>, as a lower-case language tag.</param>
/// <param name="IsFallback">Whether the text is in another language than the one asked for.</param>
/// <param name="Review">Whether the text has been reviewed.</param>
public readonly record struct ResolvedText(string Value, string Language, bool IsFallback, TextReview Review);

/// <summary>
/// A label, summary or caption with its language: one wording per language, each marked as reviewed or draft. The
/// language of the content is a fact of its own, separate from the language of the interface – a page that shows an
/// English summary inside a Persian interface has to be able to say so.
/// </summary>
public sealed class LocalizedText
{
    private readonly ImmutableSortedDictionary<string, TextVariant> _variants;

    private LocalizedText(ImmutableSortedDictionary<string, TextVariant> variants) => _variants = variants;

    /// <summary>Gets the languages the text exists in, as lower-case language tags.</summary>
    public IEnumerable<string> Languages => _variants.Keys;

    /// <summary>Creates a text in one language.</summary>
    public static LocalizedText Of(string language, string value, TextReview review = TextReview.Reviewed) =>
        new LocalizedText(ImmutableSortedDictionary.Create<string, TextVariant>(StringComparer.Ordinal)).With(language, value, review);

    /// <summary>Returns this text with a wording added or replaced for <paramref name="language"/>.</summary>
    public LocalizedText With(string language, string value, TextReview review = TextReview.Reviewed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new LocalizedText(_variants.SetItem(Tag(language), new TextVariant(value, review)));
    }

    /// <summary>Returns the wording in exactly <paramref name="language"/>, or <see langword="null"/> when there is none.</summary>
    public TextVariant? In(string language) => _variants.TryGetValue(Tag(language), out var variant) ? variant : null;

    /// <summary>
    /// Returns the text for a visitor: in <paramref name="language"/> when it exists (a regional tag such as
    /// "fa-IR" also finds "fa"), otherwise in the first of <paramref name="fallbacks"/> that exists, otherwise in
    /// any language – marked as a fallback, never presented as if it were the language asked for.
    /// </summary>
    public ResolvedText Resolve(string language, params string[] fallbacks)
    {
        ArgumentNullException.ThrowIfNull(fallbacks);
        if (Find(language) is { } wanted)
        {
            return new ResolvedText(wanted.Variant.Value, wanted.Language, IsFallback: false, wanted.Variant.Review);
        }

        foreach (var fallback in fallbacks)
        {
            if (Find(fallback) is { } found)
            {
                return new ResolvedText(found.Variant.Value, found.Language, IsFallback: true, found.Variant.Review);
            }
        }

        var first = _variants.First();
        return new ResolvedText(first.Value.Value, first.Key, IsFallback: true, first.Value.Review);
    }

    private (string Language, TextVariant Variant)? Find(string language)
    {
        var tag = Tag(language);
        if (_variants.TryGetValue(tag, out var exact))
        {
            return (tag, exact);
        }

        var region = tag.IndexOf('-', StringComparison.Ordinal);
        return region > 0 && _variants.TryGetValue(tag[..region], out var neutral) ? (tag[..region], neutral) : null;
    }

    private static string Tag(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        return language.Trim().ToLowerInvariant();
    }
}
