using System.Collections.Immutable;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The themes of the site as one visitor may see them (step 25.4): every collection is asked through its own
/// projection, at the stage this visitor has read it to, so that a theme of a later stage of one collection is
/// not named because another collection has it too.
/// </summary>
public static class ThemesOfSite
{
    /// <summary>Returns the themes of the collections of the site, the shared ones once.</summary>
    /// <param name="library">The collections of the site.</param>
    /// <param name="visitor">The visitor.</param>
    public static ImmutableArray<SharedTheme> For(DastanLibrary library, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(visitor);
        return SharedThemes.Of(library.Collections
            .Select(collection => (collection.Collection, Projection: visitor.Of(collection)))
            .Where(read => read.Projection is not null)
            .Select(read => (read.Collection, read.Projection!)));
    }
}
