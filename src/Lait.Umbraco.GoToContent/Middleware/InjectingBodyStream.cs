using System.Buffers;
using System.Text;

namespace Lait.GoToContent.Middleware;

/// <summary>
/// Stream decorator that buffers a small trailing window of bytes from the response
/// so it can locate the last <c>&lt;/body&gt;</c> marker even when it straddles two
/// writes, then injects the configured snippet immediately before that marker.
/// </summary>
internal sealed class InjectingBodyStream : Stream
{
    private const int WindowSize = 64;

    private static readonly byte[] BodyClose = Encoding.UTF8.GetBytes("</body>");

    private readonly Stream _inner;
    private readonly byte[] _snippet;
    private readonly byte[] _tail = new byte[WindowSize];

    private int _tailLength;
    private bool _injected;

    public InjectingBodyStream(Stream inner, string snippet)
    {
        _inner = inner;
        _snippet = Encoding.UTF8.GetBytes(snippet);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        if (_injected || _snippet.Length == 0)
        {
            await PassThroughAsync(buffer, cancellationToken).ConfigureAwait(false);
            return;
        }

        var combinedLength = _tailLength + buffer.Length;
        var combined = ArrayPool<byte>.Shared.Rent(combinedLength);
        try
        {
            Array.Copy(_tail, 0, combined, 0, _tailLength);
            buffer.CopyTo(combined.AsMemory(_tailLength));

            var markerIndex = LastIndexOfCaseInsensitive(
                combined.AsSpan(0, combinedLength),
                BodyClose);

            if (markerIndex >= 0)
            {
                await _inner.WriteAsync(combined.AsMemory(0, markerIndex), cancellationToken).ConfigureAwait(false);
                await _inner.WriteAsync(_snippet, cancellationToken).ConfigureAwait(false);
                await _inner.WriteAsync(combined.AsMemory(markerIndex, combinedLength - markerIndex), cancellationToken).ConfigureAwait(false);
                _tailLength = 0;
                _injected = true;
                return;
            }

            var keep = Math.Min(combinedLength, WindowSize - 1);
            var flushLength = combinedLength - keep;
            if (flushLength > 0)
            {
                await _inner.WriteAsync(combined.AsMemory(0, flushLength), cancellationToken).ConfigureAwait(false);
            }
            Array.Copy(combined, flushLength, _tail, 0, keep);
            _tailLength = keep;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(combined);
        }
    }

    private async ValueTask PassThroughAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_tailLength > 0)
        {
            await _inner.WriteAsync(_tail.AsMemory(0, _tailLength), cancellationToken).ConfigureAwait(false);
            _tailLength = 0;
        }
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public async Task FlushTailAsync(CancellationToken cancellationToken = default)
    {
        if (_tailLength == 0)
        {
            return;
        }

        await _inner.WriteAsync(_tail.AsMemory(0, _tailLength), cancellationToken).ConfigureAwait(false);
        _tailLength = 0;
    }

    private static int LastIndexOfCaseInsensitive(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        if (needle.IsEmpty || haystack.Length < needle.Length)
        {
            return -1;
        }
        for (var i = haystack.Length - needle.Length; i >= 0; i--)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (ToLowerAscii(haystack[i + j]) != ToLowerAscii(needle[j]))
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                return i;
            }
        }
        return -1;
    }

    private static byte ToLowerAscii(byte b)
        => b is >= (byte)'A' and <= (byte)'Z' ? (byte)(b + 32) : b;
}
