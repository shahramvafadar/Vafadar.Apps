using System.Text;

namespace Vafadar.Backup.Tests;

internal sealed class InMemoryBackupSource(string name, string content) : IBackupSource
{
    public string Name { get; } = name;

    public string Content { get; set; } = content;

    public int RestoreCount { get; private set; }

    public Task WriteAsync(Stream destination, CancellationToken cancellationToken) =>
        destination.WriteAsync(Encoding.UTF8.GetBytes(Content), cancellationToken).AsTask();

    public async Task RestoreAsync(Stream source, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(source, Encoding.UTF8);
        Content = await reader.ReadToEndAsync(cancellationToken);
        RestoreCount++;
    }
}
