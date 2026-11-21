using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The state of exploring (step 20.6, ADR 0025) as a page of the site reads it: from the address the page was
/// asked with, for the visitor of the request.
/// </summary>
public static class Exploring
{
    /// <summary>Reads the state of the page of a view of something.</summary>
    /// <param name="context">The request.</param>
    /// <param name="visitor">Who asks: how far they have read is part of the state and of no address.</param>
    /// <param name="subject">What the page is about.</param>
    /// <param name="view">The view the page is.</param>
    public static Exploration Of(HttpContext context, Visitor visitor, EntityId subject, ExplorationView view)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(visitor);
        return Exploration.Read(subject, view, visitor.Boundary, context.Request.Query.Select(asked => new KeyValuePair<string, string?>(asked.Key, asked.Value.Count > 0 ? asked.Value[0] : null)));
    }

    /// <summary>
    /// Returns whether a name has a lane in the chart of time of its collection: something is stated of it
    /// with a time. That is what a comparison can lay beside others.
    /// </summary>
    public static bool HasTimes(Projection? projection, EntityId entity)
    {
        return projection?.Timeline().Subjects.Any(subject => subject.Id == entity) ?? false;
    }

    /// <summary>
    /// Returns whether a name can be added to the comparison of a state: it is not the one in the middle, not
    /// compared already, the comparison has room, and the name has something with a time.
    /// </summary>
    public static bool CanCompare(Projection? projection, Exploration state, EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(state);
        return entity != state.Subject && !state.Compared.Contains(entity) && state.Compared.Length < Exploration.MostCompared && HasTimes(projection, entity);
    }
}
