using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using TurneraJardin.Api.Services;
using Xunit;

namespace TurneraJardin.Tests;

/// <summary>H12: profile photos lose their metadata and anything appended after the image.</summary>
public class ImageSanitizerTests
{
    // 16×12 JPEG with EXIF (camera make "CamaraSecreta" and a GPS tag), generated with Pillow.
    private const string JpegWithExif = "/9j/4AAQSkZJRgABAQAAAQABAAD/4QBKRXhpZgAATU0AKgAAAAgAAgEPAAIAAAAOAAAAJoglAAQAAAABAAAANAAAAABDYW1hcmFTZWNyZXRhAAABAAEAAgAAAAJTAAAA/9sAQwAQCwwODAoQDg0OEhEQExgoGhgWFhgxIyUdKDozPTw5Mzg3QEhcTkBEV0U3OFBtUVdfYmdoZz5NcXlwZHhcZWdj/9sAQwEREhIYFRgvGhovY0I4QmNjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2Nj/8AAEQgADAAQAwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/aAAwDAQACEQMRAD8Az6KKK9w8I//Z";

    // 16×12 WEBP with the same EXIF.
    private const string WebpWithExif = "UklGRpgAAABXRUJQVlA4WAoAAAAIAAAADwAACwAAVlA4IDAAAAAQAgCdASoQAAwAAUAmJaACdLoB+AH4AAPIAP7guX/2My3QEfmdf/efNYDqv7RgAABFWElGQgAAAE1NACoAAAAIAAIBDwACAAAADgAAACaIJQAEAAAAAQAAADQAAAAAQ2FtYXJhU2VjcmV0YQAAAQABAAIAAAACUwAAAA==";

    private static readonly byte[] Secret = Encoding.ASCII.GetBytes("CamaraSecreta");
    private static readonly byte[] Hidden = Encoding.ASCII.GetBytes("PK\u0003\u0004ARCHIVO-ESCONDIDO");

    private static bool Contains(byte[] haystack, byte[] needle) =>
        haystack.AsSpan().IndexOf(needle) >= 0;

    [Fact]
    public void Jpeg_loses_exif_and_trailing_data()
    {
        var input = Convert.FromBase64String(JpegWithExif).Concat(Hidden).ToArray();
        Assert.True(Contains(input, Secret));

        var result = ImageSanitizer.Sanitize(input, out var error);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(".jpg", result!.Extension);
        Assert.Equal(16, result.Width);
        Assert.Equal(12, result.Height);
        Assert.False(Contains(result.Data, Secret));
        Assert.False(Contains(result.Data, Hidden));
        Assert.Equal(0xD9, result.Data[^1]);
    }

    [Fact]
    public void Webp_loses_exif()
    {
        var result = ImageSanitizer.Sanitize(Convert.FromBase64String(WebpWithExif), out var error);

        Assert.Null(error);
        Assert.Equal(".webp", result!.Extension);
        Assert.Equal(16, result.Width);
        Assert.False(Contains(result.Data, Secret));
        Assert.Equal(result.Data.Length - 8, (int)BinaryPrimitives.ReadUInt32LittleEndian(result.Data.AsSpan(4, 4)));
    }

    [Fact]
    public void Png_loses_text_chunks_and_trailing_data()
    {
        var input = BuildPng(3, 2, withText: true).Concat(Hidden).ToArray();

        var result = ImageSanitizer.Sanitize(input, out var error);

        Assert.Null(error);
        Assert.Equal(".png", result!.Extension);
        Assert.Equal(3, result.Width);
        Assert.False(Contains(result.Data, Encoding.ASCII.GetBytes("DatoPrivado")));
        Assert.False(Contains(result.Data, Hidden));
    }

    [Fact]
    public void Too_large_images_are_rejected()
    {
        var result = ImageSanitizer.Sanitize(BuildPng(5000, 1, withText: false), out var error);
        Assert.Null(result);
        Assert.Contains("4096", error);
    }

    [Fact]
    public void Truncated_and_unknown_files_are_rejected()
    {
        var jpeg = Convert.FromBase64String(JpegWithExif);
        Assert.Null(ImageSanitizer.Sanitize(jpeg[..(jpeg.Length / 2)], out _));
        Assert.Null(ImageSanitizer.Sanitize(Encoding.ASCII.GetBytes("GIF89a000000"), out _));
        var png = BuildPng(3, 2, withText: false);
        png[40] ^= 0xFF; // breaks a CRC
        Assert.Null(ImageSanitizer.Sanitize(png, out _));
    }

    /// <summary>Minimal valid grayscale PNG, optionally with a tEXt chunk.</summary>
    private static byte[] BuildPng(int width, int height, bool withText)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 0; // grayscale
        WriteChunk(output, "IHDR", ihdr);

        if (withText)
        {
            WriteChunk(output, "tEXt", Encoding.ASCII.GetBytes("Comment\0DatoPrivado"));
        }

        var raw = new byte[height * (width + 1)]; // filter byte 0 + pixels per row
        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            z.Write(raw);
        }

        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        output.Write(number);
        var typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        output.Write(typeAndData);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc32(typeAndData));
        output.Write(number);
    }

    private static uint Crc32(byte[] bytes)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            c ^= b;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        }

        return c ^ 0xFFFFFFFFu;
    }
}
