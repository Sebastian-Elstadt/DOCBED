namespace App.Extensions;

public static class ByteArrayExtensions
{
    public static Guid ToGuid(this byte[] bytes)
    {
        Span<byte> span = stackalloc byte[16];
        bytes.AsSpan(0, Math.Min(16, bytes.Length)).CopyTo(span);
        return new Guid(span);
    }
}