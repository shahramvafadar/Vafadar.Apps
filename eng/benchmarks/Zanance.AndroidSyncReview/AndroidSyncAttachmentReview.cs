using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidSyncReview
{
    private static async Task SeedAttachmentReviewAsync(IServiceProvider services, LedgerEntry entry, Account account, CancellationToken ct)
    {
        byte[] image;
        using (var bitmap = Android.Graphics.Bitmap.CreateBitmap(400, 200, Android.Graphics.Bitmap.Config.Argb8888!)!)
        using (var canvas = new Android.Graphics.Canvas(bitmap))
        using (var paint = new Android.Graphics.Paint { Color = Android.Graphics.Color.Black, TextSize = 22 })
        using (var buffer = new MemoryStream())
        {
            canvas.DrawColor(Android.Graphics.Color.White);
            canvas.DrawText("Fictitious local receipt", 20, 70, paint); canvas.DrawText("No real financial information", 20, 120, paint);
            Check(bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, buffer), "Fictitious image encoding required.");
            image = buffer.ToArray(); bitmap.Recycle();
        }
        var pdf = TwoPageFictitiousPdf();
        var file = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-local.png", ContentType = "image/png", Data = image };
        await services.GetRequiredService<ZananceStore>().AddAttachmentAsync(file, ct);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        var rows = new List<(SyncRecord Record, long Sequence)>();
        await using (var query = db.Database.GetDbConnection().CreateCommand())
        {
            query.CommandText = "SELECT TableName,RecordKey,Body,Sequence FROM SyncOutbox WHERE Body IS NOT NULL ORDER BY Sequence";
            await using var reader = await query.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) rows.Add((new(new(reader.GetString(0), reader.GetString(1)), 5,
                JsonSerializer.Deserialize<JsonElement>(reader.GetString(2))), reader.GetInt64(3)));
        }
        var latest = rows.GroupBy(row => row.Record.Record).Select(group => group.Last()).ToArray();
        var captured = latest.Single(row => row.Record.Record.Table == "Attachments" && row.Record.Values!.Value.GetProperty("Id").GetGuid() == file.Id);
        JsonElement Body(byte[] bytes, string name, string type)
        {
            var body = JsonNode.Parse(captured.Record.Values!.Value.GetRawText())!.AsObject();
            body["Data"] = JsonSerializer.SerializeToNode(new { sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), length = bytes.Length });
            body["FileName"] = name; body["ContentType"] = type; return JsonSerializer.SerializeToElement(body);
        }
        var local = Body(image, file.FileName, file.ContentType); var remote = captured.Record with { Version = 7, Values = Body(pdf, "fictitious-service.pdf", "application/pdf") };
        var context = latest.Where(row => row.Record.Record.Table == "Entries" && row.Record.Values!.Value.GetProperty("Id").GetGuid() == entry.Id ||
            row.Record.Record.Table == "Accounts" && row.Record.Values!.Value.GetProperty("Id").GetGuid() == account.Id).Select(row => row.Record).ToArray();
        var conflict = new SyncConflict(new(captured.Record.Record, 6, local), null, remote, ["Data", "ContentType", "FileName"], "CompetingChanges", Context: context);
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict); var operation = Guid.NewGuid(); var cutoff = latest.Max(row => row.Sequence);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,RemoteId,Cutoff,Phase,PartCount,SentParts,ContentHash,ThroughCursor,ReadCursor) VALUES(1,{operation},NULL,{cutoff},1,1,1,{new string('1', 64)},NULL,0)", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES('Attachments',{captured.Record.Record.Key},{payload},{captured.Sequence},NULL)", ct);
        foreach (var bytes in new[] { image, pdf })
        {
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncBlobTransfers(Hash,Length,Manifest) VALUES({hash},{bytes.Length},NULL)", ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncBlobParts(Hash,Part,Data) VALUES({hash},0,{bytes})", ct);
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static async Task CheckAttachmentPreviewAsync(PersonalSyncPage page, PersonalSyncViewModel vm, CancellationToken ct)
    {
        var review = vm.ReviewItems[0].Review;
        Check(vm.ReviewItems[0].CanChooseLocal && vm.ReviewItems[0].CanChooseRemote, "An exact file choice must remain available independently of native renderer support.");
        var before = Directory.EnumerateFiles(FileSystem.CacheDirectory, "render-*.pdf").ToHashSet(StringComparer.Ordinal);
        await vm.PreviewLocalCommand.ExecuteAsync(review);
        Check(vm.Error is null && vm.AttachmentPreview is { PageCount: 1 }, "Original protected local image must render.");
        await CheckNativeImage(); var local = vm.AttachmentPreview!;
        var width = vm.AttachmentImageWidth; vm.PreviewZoomInCommand.Execute(null);
        Check(vm.AttachmentImageWidth > width, "Original preview must enlarge without another file read.");
        await vm.PreviewRemoteCommand.ExecuteAsync(review);
        Check(vm.Error is null && vm.AttachmentPreview is { PageCount: 2 } && vm.CanPreviewNext, "Actual first PDF page/count required.");
        Check(local.Data.All(value => value == 0), "Retired transient image must be cleared.");
        Check(vm.ReviewItems[0].CanChooseRemote, "Optional preview must not consume or block the original explicit file choice.");
        await CheckNativeImage(); var first = vm.AttachmentPreview!;
        await vm.PreviewNextCommand.ExecuteAsync(null);
        Check(vm.Error is null && vm.AttachmentPreview is { PageCount: 2 } && !vm.CanPreviewNext && vm.CanPreviewPrevious,
            "Original next-page action must render the actual second PDF page.");
        Check(first.Data.All(value => value == 0), "Previous PDF image must be cleared.");
        await CheckNativeImage(); var second = vm.AttachmentPreview!;
        Check(vm.ReviewItems[0].CanChooseLocal && vm.ReviewItems[0].CanChooseRemote, "Both exact version choices must remain available after previewing.");
        vm.CloseAttachmentPreviewCommand.Execute(null);
        Check(!vm.HasAttachmentPreview && second.Data.All(value => value == 0), "Closing the original preview must clear its transient image.");
        Check(before.SetEquals(Directory.EnumerateFiles(FileSystem.CacheDirectory, "render-*.pdf")), "Native PDF staging must not remain after rendering.");

        async Task CheckNativeImage()
        {
            var image = page.GetVisualTreeDescendants().OfType<Image>().Single();
            for (var attempt = 0; attempt < 100; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                if (!image.IsLoading && image.Handler?.PlatformView is Android.Widget.ImageView { Drawable.IntrinsicWidth: > 0, Drawable.IntrinsicHeight: > 0 }) return;
                await Task.Delay(50, ct);
            }
            throw new InvalidOperationException("The original native image control did not publish rendered pixels.");
        }
    }

    private static byte[] TwoPageFictitiousPdf()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 600] /Resources << /Font << /F1 7 0 R >> >> /Contents 4 0 R >>",
            Stream("BT /F1 20 Tf 24 540 Td (Fictitious service page 1) Tj ET"),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 600] /Resources << /Font << /F1 7 0 R >> >> /Contents 6 0 R >>",
            Stream("BT /F1 20 Tf 24 540 Td (Fictitious service page 2) Tj ET"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        var pdf = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++)
        { offsets.Add(pdf.Length); pdf.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n"); }
        var start = pdf.Length; pdf.Append("xref\n0 8\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\nstartxref\n").Append(start).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
        static string Stream(string content) => "<< /Length " + content.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n" + content + "\nendstream";
    }
}
