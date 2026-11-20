namespace HRE.Core;

public enum ChecksumKind {
    /// <summary>The ROM does not check itself; nothing is verified or corrected.</summary>
    None,

    /// <summary>The covered bytes sum to zero, modulo 256.</summary>
    Sum8,
}

/// <summary>
/// Where a ROM keeps its checksum: the correction byte at <see cref="Location"/> makes the bytes
/// of <c>[Start, End)</c> pass. <see cref="Location"/> lies inside that range.
/// </summary>
public sealed record ChecksumSpec(ChecksumKind Kind, int Location, int Start, int End) {
    /// <summary>The usual layout: the whole image sums to zero, corrected in its last byte.</summary>
    public static ChecksumSpec WholeImage(int romSize) => new(ChecksumKind.Sum8, romSize - 1, 0, romSize);
}

/// <summary>The 8-bit additive checksum these ECUs use: the covered bytes must sum to zero.</summary>
public static class Checksum {
    /// <summary>Whether the image passes the checksum the spec describes. A range outside the image fails.</summary>
    public static bool IsValid(RomImage image, ChecksumSpec spec) =>
        spec.Kind == ChecksumKind.None || (spec.End <= image.Length && IsValid(image, spec.Start, spec.End - spec.Start));

    /// <summary>Corrects the image so it passes the checksum the spec describes.</summary>
    public static RomImage Fix(RomImage image, ChecksumSpec spec) =>
        spec.Kind == ChecksumKind.None ? image : Fix(image, spec.Location, spec.Start, spec.End - spec.Start);

    public static byte Sum8(ReadOnlySpan<byte> bytes) {
        byte sum = 0;
        foreach (var b in bytes) sum += b;
        return sum;
    }

    public static bool IsValid(RomImage image) => Sum8(image.Bytes) == 0;

    /// <summary>Whether <paramref name="length"/> bytes from <paramref name="start"/> sum to zero.</summary>
    public static bool IsValid(RomImage image, int start, int length) => Sum8(image.Bytes.Slice(start, length)) == 0;

    /// <summary>Adjusts the last byte so the whole image sums to zero.</summary>
    public static RomImage Fix(RomImage image) => image.Length == 0 ? image : Fix(image, image.Length - 1, 0, image.Length);

    /// <summary>
    /// Adjusts the byte at <paramref name="at"/> so that <paramref name="length"/> bytes from
    /// <paramref name="start"/> sum to zero. <paramref name="at"/> must lie inside that range.
    /// </summary>
    public static RomImage Fix(RomImage image, int at, int start, int length) {
        if (at < start || at >= start + length) throw new ArgumentOutOfRangeException(nameof(at), "the correction byte must be inside the summed range");
        var sumWithout = (byte)(Sum8(image.Bytes.Slice(start, length)) - image[at]);
        return image.With(at, (byte)(0 - sumWithout));
    }
}
