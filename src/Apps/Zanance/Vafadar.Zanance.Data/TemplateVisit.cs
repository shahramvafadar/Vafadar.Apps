namespace Vafadar.Zanance.Data;

/// <summary>An in-memory original-template handoff, bound to its reviewed actual file and complete packet.</summary>
public sealed class TemplateVisit
{
    /// <summary>Retains the already reviewed source without copying it into portable settings or stored rows.</summary>
    internal TemplateVisit(QuickResourceReview review, Guid templateId) { Review = review; TemplateId = templateId; }
    /// <summary>Gets the original identity; this value alone never grants new draft activity.</summary>
    public Guid TemplateId { get; }
    /// <summary>Checks the original destination file and cached choice rights before editor row reads.</summary>
    public bool IsOriginalFile(string path)
    {
        Review.TemplateChoice.Access.EnsureSnapshotCurrent(); Review.FilterChoice.Access.EnsureSnapshotCurrent();
        return string.Equals(Review.TemplateChoice.DatabasePath, path, StringComparison.Ordinal);
    }
    /// <summary>Gets the original source authority used only by actual-file destination validation.</summary>
    internal QuickResourceReview Review { get; }
}
