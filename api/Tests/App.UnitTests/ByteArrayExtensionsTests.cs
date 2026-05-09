using App.Extensions;

namespace App.UnitTests;

public class ByteArrayExtensionsTests
{
    [Fact]
    public void ToGuid_ZeroPadsBytesShorterThan16()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03 };
        var guid = bytes.ToGuid();

        var span = guid.ToByteArray();
        Assert.Equal(0x01, span[0]);
        Assert.Equal(0x02, span[1]);
        Assert.Equal(0x03, span[2]);
        for (var i = 3; i < 16; i++)
            Assert.Equal(0, span[i]);
    }

    [Fact]
    public void ToGuid_UsesAllBytesWhenExactly16()
    {
        var bytes = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var guid = bytes.ToGuid();

        Assert.Equal(bytes, guid.ToByteArray());
    }

    [Fact]
    public void ToGuid_TruncatesToFirst16WhenLonger()
    {
        var bytes = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var guid = bytes.ToGuid();

        Assert.Equal(bytes.Take(16), guid.ToByteArray());
    }

    [Fact]
    public void ToGuid_HandlesEmptyArray()
    {
        var guid = Array.Empty<byte>().ToGuid();
        Assert.Equal(Guid.Empty, guid);
    }
}
