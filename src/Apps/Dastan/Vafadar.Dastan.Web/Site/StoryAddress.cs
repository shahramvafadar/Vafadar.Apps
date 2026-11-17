using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The address of a step of a story. The story a visitor enters first lives at <c>story</c>; every other story
/// of the collection at <c>story/</c> and its identifier.
/// </summary>
public static class StoryAddress
{
    /// <summary>Returns the address of the page of a story.</summary>
    public static string Of(DastanSite site, Visitor visitor, EntityId story)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Projection is { Stories: [var first, ..] } && first == story ? $"{site.Collection}/story" : $"{site.Collection}/story/{story}";
    }

    /// <summary>Returns the address of a step on the page of its story.</summary>
    public static string Of(DastanSite site, Visitor visitor, StoryStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return $"{Of(site, visitor, step.Story)}#step-{step.Number}";
    }
}
