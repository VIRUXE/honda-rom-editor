namespace HRE.Core;

/// <summary>The 8-bit additive checksum these ECUs use: the covered bytes must sum to zero.</summary>
public static class Checksum {
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
