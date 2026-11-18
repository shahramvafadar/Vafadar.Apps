using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>What a page writes for a stop of a thread: its name, what kind of thing it is, and where it leads.</summary>
public static class ThreadText
{
    /// <summary>Returns the name of what a stop leads to.</summary>
    public static LocalizedText Label(ThreadStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return step.Entity?.Label ?? step.Step?.Event.Label ?? step.Media!.Label;
    }

    /// <summary>Returns the key of the words for the kind of a stop.</summary>
    public static string Kind(ThreadStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return step.Entity is not null ? "Thread_Stop_Entity" : step.Step is not null ? "Thread_Stop_Step" : "Thread_Stop_Media";
    }

    /// <summary>Returns the address of what a stop leads to: the page of an entity or an item, or the step on the page of its story.</summary>
    public static string Address(DastanSite site, Visitor visitor, ThreadStep step)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(step);
        return step switch
        {
            { Entity: { } entity } => $"{site.Collection}/entity/{entity.Id}",
            { Step: { } storyStep } => StoryAddress.Of(site, visitor, storyStep),
            _ => $"{site.Collection}/media/{step.Media!.Id}",
        };
    }
}
