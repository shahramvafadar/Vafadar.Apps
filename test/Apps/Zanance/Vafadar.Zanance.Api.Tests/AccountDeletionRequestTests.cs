using System.Net;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Authenticated account deletion intake retains one original receipt and never applies deletion itself.</summary>
public sealed class AccountDeletionRequestTests
{
    [Fact]
    public async Task Identity_erasure_refuses_unresolved_files_and_revokes_only_the_original_service_account()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_identity_fixture";

        var uploadWait = new OwnerUploadWait();
        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "identity-erasure-service"), uploadWait);
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        var time = services.ServiceProvider.GetRequiredService<TimeProvider>();
        var users = services.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        await db.Database.MigrateAsync("20261121021759_AccountSyncFileRetirement", ct);
        const string email = "identity-erasure-target@example.invalid";
        const string password = "Synthetic-identity-erasure9!";
        var target = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        var other = new ServiceUser { UserName = "identity-erasure-other@example.invalid" };
        Assert.True((await users.CreateAsync(target, password)).Succeeded);
        db.Users.Add(other);
        var now = DateTimeOffset.UtcNow;
        var receipt = Guid.NewGuid();
        var retired = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now, Deleted = true, StorageRetired = true };
        var unaffected = new PrivateObject { Id = Guid.NewGuid(), UserId = other.Id, CreatedAt = now };
        db.Objects.AddRange(retired, unaffected);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.Sessions.Add(new() { Id = Guid.NewGuid(), UserId = other.Id, ExpiresAt = now.AddDays(1) });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());

        using var app = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await app.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var grant = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
        app.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.RootElement.GetProperty("accessToken").GetString());
        var known = await app.GetFromJsonAsync<AccountDeletionReceipt>("/identity/deletion-request", ct);
        Assert.Equal(receipt, known?.ReceiptId);

        // The database claims retirement, but a restored copy still exists and must prevent final erasure.
        var file = storage.ObjectPath(retired.Id);
        await File.WriteAllBytesAsync(file, new byte[] { 1, 2, 3 }, ct);
        var refused = await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct);
        Assert.Equal(new(AccountIdentityErasureStatus.DependenciesRemain, AccountDeletionDependencies.PrivateObjects), refused);
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Empty(await db.Set<AccountDeletionCompletion>().ToListAsync(ct));
        Assert.True(File.Exists(file));
        File.Delete(file);

        // The upload already passed HTTP authentication, but is paused before taking the owner lock.
        uploadWait.Arm();
        using var content = new ByteArrayContent(new byte[] { 71, 82, 93 });
        content.Headers.ContentType = new("application/octet-stream");
        var pendingUpload = app.PostAsync("/objects/", content, ct);
        AccountIdentityErasureResult result;
        try
        {
            await uploadWait.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            result = await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct);
        }
        finally { uploadWait.Release.TrySetResult(); }
        using var refusedUpload = await pendingUpload;
        Assert.Equal(HttpStatusCode.Unauthorized, refusedUpload.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(storage.Root, "objects")));
        Assert.Equal(AccountIdentityErasureStatus.Completed, result.Status);
        Assert.Equal(other.Id, (await db.Users.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Equal(other.Id, (await db.Sessions.AsNoTracking().SingleAsync(ct)).UserId);
        Assert.Equal(unaffected.Id, (await db.Objects.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));
        var completion = await db.Set<AccountDeletionCompletion>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(receipt, completion.ReceiptId);
        Assert.Equal(result, await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct));
        Assert.Equal(AccountIdentityErasureStatus.UnknownReceipt,
            (await AccountIdentityErasure.CompleteAsync(db, storage, time, Guid.NewGuid(), ct)).Status);
        Assert.Equal(completion.CompletedAt, (await db.Set<AccountDeletionCompletion>().AsNoTracking().SingleAsync(ct)).CompletedAt);
        using var retiredSession = await app.GetAsync("/identity/deletion-request", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, retiredSession.StatusCode);
    }

    [Fact]
    public async Task Personal_sync_retirement_cascades_original_rows_and_resumes_exact_queued_pieces()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_sync_fixture";
        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "sync-retirement-service"));
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync("20261121013816_AccountDeletionRequests", ct);
        var target = new ServiceUser { UserName = "sync-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "sync-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        var targetPiece = AddProfile(target);
        var otherPiece = AddProfile(other);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());

        var pieceRoot = Directory.CreateDirectory(Path.Combine(storage.Root, "sync-pieces")).FullName;
        var targetPath = Path.Combine(pieceRoot, targetPiece.FileId.ToString("N"));
        var otherPath = Path.Combine(pieceRoot, otherPiece.FileId.ToString("N"));
        Directory.CreateDirectory(targetPath);
        await File.WriteAllBytesAsync(otherPath, new byte[] { 5, 6, 7 }, ct);
        try
        {
            Assert.Equal(AccountPersonalSyncRetirementResult.UnknownReceipt,
                await AccountPersonalSyncRetirement.RunAsync(db, storage, Guid.NewGuid(), ct));
            Assert.Equal(2, await db.Set<SyncBlobPieceRow>().CountAsync(ct));
            Assert.Equal(AccountPersonalSyncRetirementResult.Pending,
                await AccountPersonalSyncRetirement.RunAsync(db, storage, receipt, ct));
            Assert.Equal(otherPiece.FileId, (await db.Set<SyncBlobPieceRow>().SingleAsync(ct)).FileId);
            Assert.Equal(other.Id, (await db.Set<SyncDeviceRow>().SingleAsync(ct)).UserId);
            Assert.Equal(otherPiece.SpaceId, (await db.Set<PersonalMarketSourceRow>().SingleAsync(ct)).ProfileId);
            var pending = await db.Set<AccountSyncFileRetirement>().SingleAsync(ct);
            Assert.Equal(receipt, pending.ReceiptId);
            Assert.Equal(targetPiece.FileId, pending.FileId);
            Assert.Equal(AccountDeletionDependencies.PersonalSync, await AccountDeletionInventory.InspectAsync(db, receipt, ct));
            Assert.True(Directory.Exists(targetPath));
        }
        finally { Directory.Delete(targetPath); }

        await File.WriteAllBytesAsync(targetPath, new byte[] { 1, 2, 3 }, ct);
        Assert.Equal(AccountPersonalSyncRetirementResult.Retired,
            await AccountPersonalSyncRetirement.RunAsync(db, storage, receipt, ct));
        Assert.False(File.Exists(targetPath));
        Assert.Equal(new byte[] { 5, 6, 7 }, await File.ReadAllBytesAsync(otherPath, ct));
        Assert.Empty(await db.Set<AccountSyncFileRetirement>().ToListAsync(ct));
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(receipt, (await db.AccountDeletionRequests.SingleAsync(ct)).ReceiptId);

        SyncBlobPieceRow AddProfile(ServiceUser owner)
        {
            var id = Guid.NewGuid();
            var device = new SyncDeviceRow { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), UserId = owner.Id };
            var hash = new string('a', 64);
            var piece = new SyncBlobPieceRow { SpaceId = id, BlobHash = hash, FileId = Guid.NewGuid(), Length = 3, Hash = hash };
            db.AddRange(device,
                new SyncSpaceRow { Id = id, UserId = owner.Id, BootstrapDeviceId = device.Id },
                new SyncBlobRow { SpaceId = id, Hash = hash, Length = 3, Parts = 1 },
                piece,
                new PersonalMarketSourceRow { ProfileId = id, DocumentId = Guid.NewGuid(), Digest = hash,
                    ProtectedDocument = "Fictional protected payload", Bytes = 3 });
            return piece;
        }
    }

    [Fact]
    public async Task Account_objects_remain_hidden_after_io_failure_and_resume_only_the_original_retirement()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_objects_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "object-retirement-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = fixture.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "objects-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "objects-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var original = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now };
        var blocked = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now.AddSeconds(1) };
        var unrelated = new PrivateObject { Id = Guid.NewGuid(), UserId = other.Id, CreatedAt = now };
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.Objects.AddRange(original, blocked, unrelated);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await File.WriteAllBytesAsync(storage.ObjectPath(original.Id), new byte[] { 1, 2, 3 }, ct);
        await File.WriteAllBytesAsync(storage.ObjectPath(unrelated.Id), new byte[] { 4, 5, 6 }, ct);
        var blockedPath = storage.ObjectPath(blocked.Id);
        Directory.CreateDirectory(blockedPath);
        try
        {
            Assert.Equal(AccountPrivateObjectRetirementResult.UnknownReceipt,
                await AccountPrivateObjectRetirement.RunAsync(db, storage, Guid.NewGuid(), ct));
            Assert.Equal(3, await db.Objects.CountAsync(row => !row.Deleted, ct));
            Assert.Equal(AccountPrivateObjectRetirementResult.Pending,
                await AccountPrivateObjectRetirement.RunAsync(db, storage, receipt, ct));
            Assert.True(await db.Objects.Where(row => row.UserId == target.Id).AllAsync(row => row.Deleted, ct));
            Assert.True((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == original.Id, ct)).StorageRetired);
            Assert.False((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == blocked.Id, ct)).StorageRetired);
            Assert.False(File.Exists(storage.ObjectPath(original.Id)));
        }
        finally { Directory.Delete(blockedPath); }
        // The blocked directory stood in for an interrupted file operation. The real original file can now retire.
        await File.WriteAllBytesAsync(blockedPath, new byte[] { 7, 8, 9 }, ct);
        Assert.Equal(AccountPrivateObjectRetirementResult.Retired,
            await AccountPrivateObjectRetirement.RunAsync(db, storage, receipt, ct));
        Assert.False(File.Exists(blockedPath));
        Assert.True(await db.Objects.Where(row => row.UserId == target.Id).AllAsync(row => row.Deleted && row.StorageRetired, ct));
        Assert.False((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == unrelated.Id, ct)).Deleted);
        Assert.Equal(new byte[] { 4, 5, 6 }, await File.ReadAllBytesAsync(storage.ObjectPath(unrelated.Id), ct));
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(receipt, (await db.AccountDeletionRequests.SingleAsync(ct)).ReceiptId);
    }

    [Fact]
    public async Task Deletion_inventory_scopes_each_record_group_to_the_original_receipt_without_mutation()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_inventory_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "inventory-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "inventory-target@example.invalid" };
        var other = new ServiceUser { UserName = "inventory-empty@example.invalid" };
        var sharedOwner = new ServiceUser { UserName = "inventory-current-household-owner@example.invalid" };
        var targetReceipt = Guid.NewGuid();
        var otherReceipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.AddRange(target, other, sharedOwner);
        db.AccountDeletionRequests.AddRange(
            new() { UserId = target.Id, ReceiptId = targetReceipt, RequestedAt = now },
            new() { UserId = other.Id, ReceiptId = otherReceipt, RequestedAt = now });
        var space = new HouseholdSpaceRow { Id = Guid.NewGuid(), OwnerId = sharedOwner.Id };
        db.AddRange(
            new DeviceSession { Id = Guid.NewGuid(), UserId = target.Id, ExpiresAt = now.AddDays(1) },
            new IdentityChallenge { Id = Guid.NewGuid(), UserId = target.Id, Purpose = "reset", ExpiresAt = now.AddMinutes(5) },
            new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now },
            new SyncSpaceRow { Id = Guid.NewGuid(), UserId = target.Id },
            space,
            new HouseholdMembershipEventRow { SpaceId = space.Id, Version = 1, ActorId = target.Id, CreatedAt = now },
            new BillingCustomerRow { Id = Guid.NewGuid(), AccountToken = Guid.NewGuid(), CreatedAt = now,
                RegistrationIdentityHash = BillingAccountBinding.Identity(target.Id) },
            new AiOfferRow { Id = Guid.NewGuid(), UserId = target.Id, MaximumUnits = 1, ExpiresAt = now.AddMinutes(10) });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var all = AccountDeletionDependencies.Sessions | AccountDeletionDependencies.IdentityMail |
            AccountDeletionDependencies.PrivateObjects | AccountDeletionDependencies.PersonalSync |
            AccountDeletionDependencies.Household | AccountDeletionDependencies.Billing | AccountDeletionDependencies.Analysis;
        Assert.Equal(all, await AccountDeletionInventory.InspectAsync(db, targetReceipt, ct));
        Assert.Equal(AccountDeletionDependencies.None, await AccountDeletionInventory.InspectAsync(db, otherReceipt, ct));
        Assert.Null(await AccountDeletionInventory.InspectAsync(db, Guid.NewGuid(), ct));
        Assert.Equal(3, await db.Users.CountAsync(ct));
        Assert.Equal(2, await db.AccountDeletionRequests.CountAsync(ct));
        Assert.False((await db.Sessions.SingleAsync(ct)).Revoked);
        Assert.False((await db.Objects.SingleAsync(ct)).Deleted);
        Assert.False((await db.Set<HouseholdSpaceRow>().SingleAsync(ct)).Deleted);
    }

    [Fact]
    public async Task Web_deletion_requires_fresh_proof_and_shares_the_pending_receipt_with_the_app()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_web_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "web-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string email = "fictional-web-deletion@example.invalid";
        const string password = "Synthetic-web-deletion9!";
        var owner = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(owner, password)).Succeeded);

        const string page = "/identity/delete-account";
        using var browser = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var firstPage = await browser.GetAsync(page, ct);
        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        Assert.True(firstPage.Headers.CacheControl?.NoStore);
        Assert.Contains("frame-ancestors 'none'", firstPage.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        var html = await firstPage.Content.ReadAsStringAsync(ct);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1)).Groups[1].Value;
        Assert.NotEmpty(token);
        using var rejectedCsrf = await browser.PostAsync(page, Form(email, password, null), ct);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedCsrf.StatusCode);
        using var wrong = await browser.PostAsync(page, Form(email, "Incorrect-fictional9!", token), ct);
        using var unknown = await browser.PostAsync(page, Form("unknown@example.invalid", "Incorrect-fictional9!", token), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var accepted = await browser.PostAsync(page, Form(email, password, token), ct);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.True(accepted.Headers.CacheControl?.NoStore);
        var pending = await db.AccountDeletionRequests.AsNoTracking().SingleAsync(ct);
        var result = await accepted.Content.ReadAsStringAsync(ct);
        Assert.Contains(pending.ReceiptId.ToString("D"), result, StringComparison.Ordinal);
        Assert.DoesNotContain(email, result, StringComparison.Ordinal);
        Assert.DoesNotContain(password, result, StringComparison.Ordinal);
        Assert.Equal(owner.Id, pending.UserId);
        Assert.Empty(await db.Sessions.ToListAsync(ct));

        using var app = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await app.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var grant = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
        app.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.RootElement.GetProperty("accessToken").GetString());
        var receipt = await app.GetFromJsonAsync<AccountDeletionReceipt>("/identity/deletion-request", ct);
        Assert.Equal(new AccountDeletionReceipt(pending.ReceiptId, pending.RequestedAt), receipt);

        static FormUrlEncodedContent Form(string email, string password, string? token)
        {
            var fields = new Dictionary<string, string> { ["email"] = email, ["password"] = password };
            if (token is not null) fields["__RequestVerificationToken"] = token;
            return new(fields);
        }
    }

    /// <summary>The additive schema preserves existing accounts and accepts only their own reauthenticated request.</summary>
    [Fact]
    public async Task Deletion_request_survives_concurrent_retry_and_keeps_other_accounts_separate()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));

        await using var host = new IdentityHost(configured!, Path.Combine(root, "service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string previous = "20261120003818_AiPilotCreateRefusal";
        await db.Database.MigrateAsync(previous, ct);
        const string password = "Fictional-Deletion-Request9!";
        var owner = await SeedAsync("request-owner@example.invalid");
        var other = await SeedAsync("request-other@example.invalid");
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var unauthenticated = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var ownerHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        const string route = "/identity/deletion-request";
        using var noSession = await unauthenticated.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
        await LoginAsync(ownerHttp, owner);
        await LoginAsync(otherHttp, other);
        using var originalStatus = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, originalStatus.StatusCode);
        using var wrongPassword = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput("Fictional-Incorrect-Password9!"), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        var results = await Task.WhenAll(RequestAsync(), RequestAsync());
        Assert.Equal(results[0], results[1]);
        Assert.NotEqual(Guid.Empty, results[0].ReceiptId);
        using var status = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.True(status.Headers.CacheControl?.NoStore);
        Assert.Equal(results[0], await status.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        using var otherStatus = await otherHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, otherStatus.StatusCode);
        var recorded = Assert.Single(await db.AccountDeletionRequests.AsNoTracking().ToListAsync(ct));
        Assert.Equal(owner.Id, recorded.UserId);
        Assert.Equal(results[0].ReceiptId, recorded.ReceiptId);
        Assert.Equal(results[0].RequestedAt, recorded.RequestedAt);
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(2, await db.Sessions.CountAsync(row => !row.Revoked, ct));
        Assert.Empty(await db.Challenges.ToListAsync(ct));

        async Task<ServiceUser> SeedAsync(string email)
        {
            var user = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            return user;
        }

        async Task LoginAsync(HttpClient http, ServiceUser user)
        {
            using var response = await http.PostAsJsonAsync("/identity/login", new IdentityInput(user.Email!, password), ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var grant = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.GetProperty("accessToken").GetString());
        }

        async Task<AccountDeletionReceipt> RequestAsync()
        {
            using var response = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            return Assert.IsType<AccountDeletionReceipt>(await response.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        }
    }

    private sealed class OwnerUploadWait : DbCommandInterceptor
    {
        private int armed;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT pg_advisory_xact_lock(hashtextextended(", StringComparison.Ordinal) &&
                Interlocked.Exchange(ref armed, 0) == 1)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class IdentityHost(string connection, string root, OwnerUploadWait? uploadWait = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                if (uploadWait is not null)
                    services.ConfigureDbContext<ServiceDbContext>(options => options.AddInterceptors(uploadWait));
            });
        }
    }
}
