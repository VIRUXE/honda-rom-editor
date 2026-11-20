using System.Security.Cryptography;

namespace HRE.Core;

/// <summary>An immutable ROM image. Every change produces a new image.</summary>
public sealed class RomImage {
    private readonly byte[] _bytes;

    public RomImage(ReadOnlySpan<byte> bytes) => _bytes = bytes.ToArray();

    private RomImage(byte[] owned, bool _) => _bytes = owned;

    public static RomImage Load(string path) => new(File.ReadAllBytes(path), true);

    public int Length => _bytes.Length;

    public ReadOnlySpan<byte> Bytes => _bytes;

    public byte this[int address] => _bytes[address];

    /// <summary>Big-endian 16-bit word at <paramref name="address"/>.</summary>
    public ushort Word(int address) => (ushort)(_bytes[address] << 8 | _bytes[address + 1]);

    public byte[] ToArray() => (byte[])_bytes.Clone();

    public void Save(string path) => File.WriteAllBytes(path, _bytes);

    /// <summary>Lower-case hex SHA-256 of the whole image.</summary>
    public string Sha256() => Convert.ToHexStringLower(SHA256.HashData(_bytes));

    public RomImage With(int address, byte value) {
        if (_bytes[address] == value) return this;
        var copy = ToArray();
        copy[address] = value;
        return new RomImage(copy, true);
    }

    /// <summary>Applies the writes in order. Returns this image when nothing changes.</summary>
    public RomImage With(IEnumerable<(int Address, byte Value)> writes) {
        byte[]? copy = null;
        foreach (var (address, value) in writes) {
            ArgumentOutOfRangeException.ThrowIfNegative(address);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(address, _bytes.Length);
            if ((copy ?? _bytes)[address] == value) continue;
            copy ??= ToArray();
            copy[address] = value;
        }
        return copy is null ? this : new RomImage(copy, true);
    }
}
