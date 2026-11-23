using Microsoft.Extensions.Options;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Where a visitor proposes a place to the editors (D-085, ADR 0023): a place they were at that no collection
/// has an entry for. The site takes proposals when it takes what visitors write to the editors at all
/// (<c>Dastan:Reports</c>) and keeps them in its database; the editors read them with their tool. A proposal
/// is the place in the visitor's words and the day: the site keeps nothing about who wrote it – not the
/// account either, though only who signed in is offered the form.
/// </summary>
public sealed class ProposalDesk
{
    /// <summary>How many places one sender (<see cref="Sender"/>) may propose in an hour.</summary>
    public const int PerHour = 5;

    private readonly ProposalBox? _box;
    private readonly TimeProvider _time;

    /// <summary>Initializes a new instance of the <see cref="ProposalDesk"/> class.</summary>
    public ProposalDesk(IOptions<DastanOptions> options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        _time = time;
        if (options.Value.Reports)
        {
            _box = new ProposalBox(options.Value.DeliveryDatabase("Dastan:Reports"));
        }
    }

    /// <summary>Gets the kinds of place a proposal may name: the keys of the types people can have been at.</summary>
    public static IReadOnlyList<string> Kinds { get; } = [.. Institutions.Types.Select(type => type.Key)];

    /// <summary>Gets a value indicating whether the site takes proposals.</summary>
    public bool IsOpen => _box is not null;

    /// <summary>Keeps a proposal. Returns <see langword="false"/> when the box of the editors is full.</summary>
    /// <param name="place">The place, as <see cref="ProposalBox.Read"/> made it.</param>
    /// <param name="language">The language of the page it was written on.</param>
    public bool Take(ProposedPlace place, string language) => _box!.Take(place, language, DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime));
}
