using System.Buffers.Binary;
using System.Text;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Cleans an uploaded profile photo before it is stored and served (H12). It does not decode the
/// pixels: it walks the file's structure and rebuilds it keeping only what is needed to display it.
///  - Removes metadata: EXIF (camera, date and GPS position of where the photo was taken), XMP,
///    comments, text chunks.
///  - Drops anything appended after the end of the image (a common way of hiding another file, e.g. a
///    ZIP or a script, inside a "valid" image).
///  - Rejects truncated or malformed files and images larger than <see cref="MaxDimension"/> per side
///    (a small file that expands into a huge bitmap in the browser).
/// Supported: JPEG, PNG, WEBP. Anything else → null.
/// </summary>
public static class ImageSanitizer
{
    public const int MaxDimension = 4096;

    public sealed record Result(byte[] Data, string Extension, int Width, int Height);

    public static Result? Sanitize(ReadOnlySpan<byte> data, out string? error)
    {
        error = null;
        try
        {
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return Jpeg(data, out error);
            if (data.Length >= 8 && data[..8].SequenceEqual(PngSignature)) return Png(data, out error);
            if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8)) return Webp(data, out error);
            error = "Solo se aceptan imágenes JPG, PNG o WEBP.";
            return null;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            error = "La imagen está dañada o incompleta.";
            return null;
        }
    }

    private static Result? CheckSize(byte[] output, string ext, int width, int height, out string? error)
    {
        error = null;
        if (width <= 0 || height <= 0)
        {
            error = "La imagen está dañada o incompleta.";
            return null;
        }

        if (width > MaxDimension || height > MaxDimension)
        {
            error = $"La imagen es demasiado grande: como máximo {MaxDimension} × {MaxDimension} píxeles.";
            return null;
        }

        return new Result(output, ext, width, height);
    }

    // ---------------------------------------------------------------- JPEG

    private static Result? Jpeg(ReadOnlySpan<byte> d, out string? error)
    {
        using var output = new MemoryStream(d.Length);
        output.Write(stackalloc byte[] { 0xFF, 0xD8 });
        int width = 0, height = 0;
        var pos = 2;

        while (true)
        {
            if (d[pos] != 0xFF)
            {
                error = "La imagen está dañada o incompleta.";
                return null;
            }

            while (d[pos] == 0xFF) pos++; // fill bytes
            var marker = d[pos];
            pos++;

            if (marker == 0xD9) // EOI: end of image. Anything after it is discarded.
            {
                output.Write(stackalloc byte[] { 0xFF, 0xD9 });
                break;
            }

            if (marker is >= 0xD0 and <= 0xD7 or 0x01) // markers without a length
            {
                output.Write(stackalloc byte[] { 0xFF, marker });
                continue;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(pos, 2));
            if (length < 2)
            {
                error = "La imagen está dañada o incompleta.";
                return null;
            }

            var segment = d.Slice(pos, length); // includes the 2 length bytes

            // SOFn (frame header): has the dimensions. C4 = DHT, C8 = JPG, CC = DAC are not frames.
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                height = BinaryPrimitives.ReadUInt16BigEndian(segment.Slice(3, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(segment.Slice(5, 2));
            }

            // Kept: everything needed to decode. Dropped: APP1..APP13 and APP15 (EXIF, XMP, ICC, Photoshop…)
            // and COM (comments). APP0 (JFIF) and APP14 (Adobe, needed for the colours of some JPEGs) stay.
            var drop = marker is >= 0xE1 and <= 0xED or 0xEF or 0xFE;
            if (!drop)
            {
                output.Write(stackalloc byte[] { 0xFF, marker });
                output.Write(segment);
            }

            pos += length;

            if (marker == 0xDA) // SOS: compressed data follows until the next real marker
            {
                var start = pos;
                while (!(d[pos] == 0xFF && d[pos + 1] != 0x00 && d[pos + 1] is < 0xD0 or > 0xD7))
                {
                    pos++;
                }

                output.Write(d[start..pos]);
            }
        }

        return CheckSize(output.ToArray(), ".jpg", width, height, out error);
    }

    // ---------------------------------------------------------------- PNG

    private static ReadOnlySpan<byte> PngSignature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // Chunks needed to display the image (plus colour information). Text, dates, EXIF, etc. are dropped.
    private static readonly HashSet<string> PngKeep = new() { "IHDR", "PLTE", "IDAT", "IEND", "tRNS", "gAMA", "cHRM", "sRGB", "iCCP", "sBIT", "pHYs", "bKGD" };

    private static Result? Png(ReadOnlySpan<byte> d, out string? error)
    {
        using var output = new MemoryStream(d.Length);
        output.Write(PngSignature);
        int width = 0, height = 0;
        var pos = 8;
        var first = true;

        while (true)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(d.Slice(pos, 4));
            if (length < 0 || pos + 12L + length > d.Length)
            {
                error = "La imagen está dañada o incompleta.";
                return null;
            }

            var type = Encoding.ASCII.GetString(d.Slice(pos + 4, 4));
            var chunk = d.Slice(pos, 12 + length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(chunk[^4..]);
            if (Crc32(chunk.Slice(4, 4 + length)) != crc)
            {
                error = "La imagen está dañada o incompleta.";
                return null;
            }

            if (first)
            {
                if (type != "IHDR")
                {
                    error = "La imagen está dañada o incompleta.";
                    return null;
                }

                width = (int)BinaryPrimitives.ReadUInt32BigEndian(chunk.Slice(8, 4));
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(chunk.Slice(12, 4));
                first = false;
            }

            if (PngKeep.Contains(type))
            {
                output.Write(chunk);
            }

            pos += 12 + length;
            if (type == "IEND")
            {
                break; // anything after IEND is discarded
            }
        }

        return CheckSize(output.ToArray(), ".png", width, height, out error);
    }

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in bytes) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    // ---------------------------------------------------------------- WEBP

    private static readonly HashSet<string> WebpKeep = new() { "VP8 ", "VP8L", "VP8X", "ALPH", "ANIM", "ANMF", "ICCP" };

    private static Result? Webp(ReadOnlySpan<byte> d, out string? error)
    {
        var riffSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(4, 4));
        var end = 8 + riffSize;
        if (riffSize < 4 || end > d.Length)
        {
            error = "La imagen está dañada o incompleta.";
            return null;
        }

        using var body = new MemoryStream(d.Length);
        body.Write("WEBP"u8);
        int width = 0, height = 0;
        var pos = 12;

        while (pos + 8 <= end) // anything after the RIFF size is discarded
        {
            var fourCc = Encoding.ASCII.GetString(d.Slice(pos, 4));
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(pos + 4, 4));
            var padded = size + (size & 1);
            if (size < 0 || pos + 8L + size > end)
            {
                error = "La imagen está dañada o incompleta.";
                return null;
            }

            var payload = d.Slice(pos + 8, size);
            switch (fourCc)
            {
                case "VP8X":
                    width = 1 + (payload[4] | payload[5] << 8 | payload[6] << 16);
                    height = 1 + (payload[7] | payload[8] << 8 | payload[9] << 16);
                    break;
                case "VP8L" when width == 0:
                    var bits = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(1, 4));
                    width = (int)(bits & 0x3FFF) + 1;
                    height = (int)((bits >> 14) & 0x3FFF) + 1;
                    break;
                case "VP8 " when width == 0:
                    width = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2)) & 0x3FFF;
                    height = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)) & 0x3FFF;
                    break;
            }

            if (WebpKeep.Contains(fourCc))
            {
                var copy = d.Slice(pos, 8 + size).ToArray();
                if (fourCc == "VP8X")
                {
                    copy[8] &= unchecked((byte)~0x0C); // the EXIF and XMP chunks are gone: clear their flags
                }

                body.Write(copy);
                if ((size & 1) == 1) body.WriteByte(0);
            }

            pos += 8 + padded;
        }

        using var output = new MemoryStream((int)body.Length + 8);
        output.Write("RIFF"u8);
        Span<byte> sizeBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(sizeBytes, (uint)body.Length);
        output.Write(sizeBytes);
        body.Position = 0;
        body.CopyTo(output);

        return CheckSize(output.ToArray(), ".webp", width, height, out error);
    }
}
