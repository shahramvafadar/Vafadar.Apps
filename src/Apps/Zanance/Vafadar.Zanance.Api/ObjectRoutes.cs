using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace Vafadar.Zanance.Api;

/// <summary>Bounded private immutable storage, authorized by server identity on every operation.</summary>
public static class ObjectRoutes
{
    /// <summary>Maps private object operations; identifiers never authorize access by themselves.</summary>
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/objects").RequireAuthorization();
        group.MapGet("/", async (ClaimsPrincipal principal, ServiceDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Objects.AsNoTracking().Where(o => o.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier) && !o.Deleted)
                .Select(o => new { o.Id, o.Length, o.Sha256, o.CreatedAt }).ToListAsync(ct)));
        group.MapPost("/", UploadAsync);
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            var manifest = await db.Objects.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier) && !o.Deleted, ct);
            if (manifest is null) return Results.NotFound();
            var path = storage.ObjectPath(id);
            if (!File.Exists(path)) return Results.StatusCode(503);
            if (new FileInfo(path).Length > 8 * 1024 * 1024 + 65536) return Results.StatusCode(503);
            // Verification precedes response publication; missing/tampered bytes are never silently served.
            byte[] bytes;
            try { bytes = protection.CreateProtector("private-object-v1").Unprotect(await File.ReadAllBytesAsync(path, ct)); }
            catch (CryptographicException) { return Results.StatusCode(503); }
            if (bytes.LongLength != manifest.Length || Convert.ToHexStringLower(SHA256.HashData(bytes)) != manifest.Sha256) return Results.StatusCode(503);
            return Results.File(bytes, "application/octet-stream", enableRangeProcessing: false);
        });
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ServiceDbContext db, CancellationToken ct) =>
        {
            var changed = await db.Objects.Where(o => o.Id == id && o.UserId == principal.FindFirstValue(ClaimTypes.NameIdentifier) && !o.Deleted)
                .ExecuteUpdateAsync(u => u.SetProperty(o => o.Deleted, true), ct);
            return changed == 1 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static async Task<IResult> UploadAsync(HttpContext http, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct)
    {
        const int MaximumLength = 8 * 1024 * 1024;
        if (http.Request.ContentType != "application/octet-stream" || http.Request.ContentLength is > MaximumLength) return Results.BadRequest();
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await http.Request.Body.ReadAsync(buffer, ct)) != 0)
        {
            if (content.Length + count > MaximumLength) return Results.StatusCode(413);
            await content.WriteAsync(buffer.AsMemory(0, count), ct);
        }
        if (content.Length == 0) return Results.BadRequest();
        var owner = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Identity required.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize capacity across all workers on this real owner, rather than a process-local quota.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({owner}, 0))", ct);
        var used = await db.Objects.Where(o => o.UserId == owner && !o.Deleted).SumAsync(o => o.Length, ct);
        if (used + content.Length > 64 * 1024 * 1024 || await db.Objects.CountAsync(o => o.UserId == owner && !o.Deleted, ct) >= 100)
            return Results.StatusCode(409);
        var bytes = content.ToArray();
        var manifest = new PrivateObject { Id = Guid.NewGuid(), UserId = owner, Length = bytes.LongLength,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), CreatedAt = time.GetUtcNow() };
        var path = storage.ObjectPath(manifest.Id);
        // Publish immutable bytes before committing their manifest. A crash may leave an orphan, never a readable partial object.
        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
        {
            await file.WriteAsync(protection.CreateProtector("private-object-v1").Protect(bytes), ct); file.Flush(flushToDisk: true);
        }
        db.Objects.Add(manifest); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Results.Created($"/objects/{manifest.Id}", new { manifest.Id, manifest.Length, manifest.Sha256 });
    }
}
