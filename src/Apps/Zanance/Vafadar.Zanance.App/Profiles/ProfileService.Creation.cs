using Vafadar.Data;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    private const string CreationKey = "profiles.creation.pending";
    private const int CreationLimit = 64 * 1024;

    /// <summary>Captures device-only Add intent with the same privacy as existing device profile names.</summary>
    internal string CaptureCreation(ProfileCatalogueReview review, LocalProfile profile, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            location.Access.RequireDrainedOwner(owner); ValidateWorkCore(review, true);
            if (preferences.Get(CreationKey) is not null) throw new IOException("Previous profile creation must finish first.");
            if (!review.CanCreate) throw new InvalidOperationException("The reviewed profile cannot be created.");
            var mutation = Guid.ParseExact(profile.Id, "N");
            var after = EncodeCatalogue(NewProfileCatalogue(review, profile, mutation), mutation);
            var request = string.Join('|', "1", "0", profile.Id, EncodeField(profile.Name), CatalogueDigest(preferences.Get(CatalogueKey)),
                CatalogueDigest(after), EncodeField(after), EncodeField(Path.GetFullPath(MainPath())));
            if (request.Length > CreationLimit) throw new IOException("Profile creation exceeds its bound.");
            preferences.Set(CreationKey, request); return request;
        }
    }

    /// <summary>Reads bounded existing device intent without manufacturing an identity or Add request.</summary>
    internal string? ReadCreation(DatabaseAccessGate.ExclusiveLease owner)
    {
        location.Access.RequireDrainedOwner(owner); var raw = preferences.Get(CreationKey);
        if (raw is not null) _ = ParseCreation(raw); return raw;
    }

    /// <summary>Validates complete original/published catalogue state before preparing the retained identity.</summary>
    internal LocalProfile ValidateCreation(string raw, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            location.Access.RequireDrainedOwner(owner); RequireCreation(raw);
            var fields = ParseCreation(raw); var profile = new LocalProfile(fields[2], DecodeField(fields[3]));
            if (fields[1] == "1") return profile;
            var actual = CatalogueDigest(preferences.Get(CatalogueKey));
            if (actual != fields[4] && actual != fields[5]) throw new IOException("Catalogue changed during pending creation.");
            if (actual == fields[4])
            {
                var review = ReadWorkReview(); var mutation = Guid.ParseExact(profile.Id, "N");
                if (!review.CanCreate || review.Profiles.Any(p => p.Id == profile.Id)
                    || EncodeCatalogue(NewProfileCatalogue(review, profile, mutation), mutation) != DecodeField(fields[6]))
                    throw new IOException("Pending creation differs from current policy or catalogue.");
            }
            else if (!All.Contains(profile)) throw new IOException("Published creation differs from its captured profile.");
            return profile;
        }
    }

    /// <summary>Publishes only the recorded catalogue after native preparation; persistence belongs to the host.</summary>
    internal void PublishCreation(string raw, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            _ = ValidateCreation(raw, owner); var fields = ParseCreation(raw);
            if (fields[1] != "0") throw new IOException("Creation is already completed.");
            preferences.Set(CatalogueKey, DecodeField(fields[6]));
            if (CatalogueDigest(preferences.Get(CatalogueKey)) != fields[5]) throw new IOException("Creation publication differs.");
        }
    }

    /// <summary>Records completion after native publication; later legitimate edits cannot replay old state.</summary>
    internal string CompleteCreation(string raw, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            _ = ValidateCreation(raw, owner); var fields = ParseCreation(raw);
            if (CatalogueDigest(preferences.Get(CatalogueKey)) != fields[5])
                throw new IOException("Creation publication was not retained after persistence.");
            fields[1] = "1";
            var completed = string.Join('|', fields); preferences.Set(CreationKey, completed); return completed;
        }
    }

    /// <summary>Removes only the exact completed request without deleting financial/key artifacts.</summary>
    internal void ForgetCreation(string raw, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            location.Access.RequireDrainedOwner(owner); RequireCreation(raw);
            if (ParseCreation(raw)[1] != "1") throw new IOException("Pending creation cannot be forgotten.");
            preferences.Remove(CreationKey);
        }
    }
    private void RequireCreation(string raw)
    { if (preferences.Get(CreationKey) != raw) throw new IOException("Creation request changed."); }
    private string[] ParseCreation(string raw)
    {
        if (raw.Length > CreationLimit) throw new IOException("Creation request exceeds its bound.");
        var fields = raw.Split('|');
        if (fields.Length != 8 || fields[0] != "1" || fields[1] is not ("0" or "1")
            || !Guid.TryParseExact(fields[2], "N", out var id) || id == Guid.Empty || id.ToString("N") != fields[2])
            throw new IOException("Creation request is malformed.");
        try
        {
            _ = new ProfileRetirementCatalogueDigest(fields[4], fields[5]);
            var name = DecodeField(fields[3]);
            if (string.IsNullOrWhiteSpace(name) || Clean(name) != name || DecodeField(fields[7]) != Path.GetFullPath(MainPath()))
                throw new IOException("Creation belongs to another device catalogue.");
            var after = DecodeField(fields[6]) ?? throw new IOException("Missing profile publication.");
            if (!CatalogueProfiles(ReadCatalogue(after, null, null)).Contains(new LocalProfile(fields[2], name)))
                throw new IOException("Pending publication differs from its profile.");
        }
        catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException)
        { throw new IOException("Creation request is malformed.", error); }
        return fields;
    }
}
