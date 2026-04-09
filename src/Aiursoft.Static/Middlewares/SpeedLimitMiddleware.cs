using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;

namespace Aiursoft.Static.Middlewares;

/// <summary>
/// Limits the download speed per connection. Placed before UseStaticFiles so that
/// both the SendFileAsync (kernel sendfile) path and the stream-write path are throttled.
/// </summary>
public class SpeedLimitMiddleware(RequestDelegate next, int maxBytesPerSecond)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (maxBytesPerSecond <= 0)
        {
            await next(context);
            return;
        }

        var originalFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
        var throttled = new ThrottledResponseBodyFeature(originalFeature, maxBytesPerSecond);
        context.Features.Set<IHttpResponseBodyFeature>(throttled);

        try
        {
            await next(context);
        }
        finally
        {
            context.Features.Set(originalFeature);
        }
    }
}

/// <summary>
/// Replaces the two fast-paths used by UseStaticFiles:
///   1. SendFileAsync  – normally becomes a kernel sendfile() call on Linux
///   2. Stream writes  – fallback path on all platforms
/// Both are wrapped with a token-bucket style rate limiter per connection.
/// </summary>
internal sealed class ThrottledResponseBodyFeature(
    IHttpResponseBodyFeature inner,
    int maxBytesPerSecond) : IHttpResponseBodyFeature
{
    private ThrottledStream? _throttledStream;

    // Wrap the body stream so that write-based paths are throttled.
    public Stream Stream
    {
        get
        {
            _throttledStream ??= new ThrottledStream(inner.Stream, maxBytesPerSecond);
            return _throttledStream;
        }
    }

    // PipeWriter is not used by UseStaticFiles' hot path, but forward it anyway.
    public PipeWriter Writer => inner.Writer;

    public Task CompleteAsync() => inner.CompleteAsync();
    public void DisableBuffering() => inner.DisableBuffering();
    public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);

    /// <summary>
    /// Replaces the kernel sendfile() path with a chunked, throttled read-write loop.
    /// Chunk size is sized so each chunk takes roughly 100 ms at the target rate,
    /// which gives smooth pacing without excessive syscall overhead.
    /// </summary>
    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken)
    {
        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536,
            useAsync: true);

        if (offset > 0)
            fs.Seek(offset, SeekOrigin.Begin);

        var remaining = count ?? (fs.Length - offset);

        // Target ~100 ms per chunk; clamp between 4 KiB and 256 KiB.
        var chunkSize = (int)Math.Clamp(maxBytesPerSecond / 10L, 4096, 262144);
        var buffer = new byte[chunkSize];

        var windowStart = DateTime.UtcNow;
        long totalSent = 0;

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            var toRead = (int)Math.Min(buffer.Length, remaining);
            var bytesRead = await fs.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
            if (bytesRead == 0) break;

            await inner.Stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            await inner.Stream.FlushAsync(cancellationToken);

            totalSent += bytesRead;
            remaining  -= bytesRead;

            // Sleep if we are ahead of the allowed rate.
            var elapsed        = (DateTime.UtcNow - windowStart).TotalSeconds;
            var expectedSeconds = (double)totalSent / maxBytesPerSecond;
            var delaySeconds    = expectedSeconds - elapsed;
            if (delaySeconds > 0.001)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }
    }
}

/// <summary>
/// Wraps a response body stream and enforces a per-connection byte-rate ceiling
/// using a simple elapsed-time token-bucket approach.
/// </summary>
internal sealed class ThrottledStream(Stream inner, int maxBytesPerSecond) : Stream
{
    private long     _bytesSent;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public override bool  CanRead  => inner.CanRead;
    public override bool  CanSeek  => inner.CanSeek;
    public override bool  CanWrite => inner.CanWrite;
    public override long  Length   => inner.Length;
    public override long  Position { get => inner.Position; set => inner.Position = value; }

    public override void  Flush()                                              => inner.Flush();
    public override Task  FlushAsync(CancellationToken ct)                     => inner.FlushAsync(ct);
    public override int   Read(byte[] buffer, int offset, int count)           => inner.Read(buffer, offset, count);
    public override long  Seek(long offset, SeekOrigin origin)                 => inner.Seek(offset, origin);
    public override void  SetLength(long value)                                => inner.SetLength(value);
    public override void  Write(byte[] buffer, int offset, int count)          => inner.Write(buffer, offset, count);

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleAsync(count, cancellationToken);
        await inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleAsync(buffer.Length, cancellationToken);
        await inner.WriteAsync(buffer, cancellationToken);
    }

    private async Task ThrottleAsync(int byteCount, CancellationToken cancellationToken)
    {
        _bytesSent += byteCount;
        var elapsed         = (DateTime.UtcNow - _startTime).TotalSeconds;
        var expectedSeconds = (double)_bytesSent / maxBytesPerSecond;
        var delaySeconds    = expectedSeconds - elapsed;
        if (delaySeconds > 0.001)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
    }
}
