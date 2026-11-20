using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>The packs in <c>packs/</c>: what must hold of them whatever image they are used on.</summary>
public class ShippedPackTests {
    internal static string PackDir => Path.Combine(AppContext.BaseDirectory, "packs");

    internal static IReadOnlyList<DefinitionPack> Packs() {
        var (packs, errors) = DefinitionPacks.LoadDirectory(PackDir);
        Assert.Empty(errors);
        return packs;
    }

    internal static DefinitionPack Pack(string id) => Packs().Single(p => p.Id == id);

    /// <summary>Whether the pack gives every address and size outright, so that it resolves the same on any image.</summary>
    internal static bool IsFixed(DefinitionPack pack) => ParamDefinitions.Resolve(pack.Params, null, pack.RomSize).Problems.Count == 0;

    [Fact]
    public void EveryPackLoads() {
        var packs = Packs();
        Assert.Equal(
            ["crome-gold", "crome-p30-boost", "crome-p30-boost-rev", "crome-p30-boost1", "crome-p72-boost", "ectune-273", "obd1-p28", "obd1-p30", "obd1-p72", "uberdata-p72"],
            packs.Select(p => p.Id).Order(StringComparer.Ordinal));
        Assert.All(packs, pack => {
            Assert.Equal(RomPlatform.Obd1Oki66207, pack.Platform);
            Assert.Equal(0x8000, pack.RomSize);
            Assert.False(string.IsNullOrWhiteSpace(pack.Name));
            Assert.Equal(0, pack.Params.Count % 4);
            // A fingerprint that stated a size would refuse a dump with a stray byte after the
            // ROM, and one that hashed the image would refuse every tune.
            Assert.All(pack.Fingerprints, fingerprint => {
                Assert.Null(fingerprint.Size);
                Assert.Null(fingerprint.Sha256);
                Assert.NotEmpty(fingerprint.Bytes);
            });
        });
        // The packs that read something from the ROM: where a tuning program put the maps, or
        // the range of the MAP sensor it wrote down.
        Assert.Equal(["crome-p30-boost", "crome-p30-boost-rev", "crome-p30-boost1", "crome-p72-boost"], packs.Where(p => !IsFixed(p)).Select(p => p.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void APackWithFixedAddressesResolvesOnAnyImage() {
        Assert.All(Packs().Where(IsFixed), pack => {
            var (defs, problems) = pack.Resolve(DefinitionPackTests.Blank);
            Assert.Empty(problems);
            AssertMainMaps(pack, defs);
        });
    }

    /// <summary>
    /// The layout every OBD1 pack describes, on the parameters it resolved to: four main maps per
    /// set, fuel with one multiplier per column in the row after the last, no map sharing cells
    /// with another.
    /// </summary>
    internal static void AssertMainMaps(DefinitionPack pack, IReadOnlyList<ParamDef> defs) {
        Assert.Equal(pack.Params.Count, defs.Count);
        Assert.All(defs.Chunk(4), set => {
            Assert.Equal(["Fuel", "Fuel", "Ignition", "Ignition"], set.Select(d => d.Category));
            Assert.Equal([false, true, false, true], set.Select(d => d.Vtec));
            Assert.All(set, d => Assert.IsType<ParamKind.Table>(d.Kind));
            var (fuelLow, fuelHigh, ignLow, ignHigh) = (set[0], set[1], set[2], set[3]);
            // A map and its other-cam twin differ in rows at most; both tables of a cam share the rpm axis.
            Assert.Equal(fuelLow.Kind, ignLow.Kind);
            Assert.Equal(fuelHigh.Kind, ignHigh.Kind);
            Assert.Equal(((ParamKind.Table)fuelLow.Kind).Cols, ((ParamKind.Table)fuelHigh.Kind).Cols);
            Assert.Equal(fuelLow.RowAxis, ignLow.RowAxis);
            Assert.Equal(fuelHigh.RowAxis, ignHigh.RowAxis);
            Assert.Equal(AxisEncoding.LogBytes, fuelLow.RowAxis.Encoding);
            Assert.Equal(AxisEncoding.Bytes, fuelHigh.RowAxis.Encoding);
            Assert.All(set, d => Assert.Equal(AxisEncoding.Bytes, d.ColAxis.Encoding));
            Assert.All(new[] { fuelLow, fuelHigh }, fuel => {
                var table = (ParamKind.Table)fuel.Kind;
                // The lookup reads the multipliers from the row after the last.
                Assert.Equal(new ColumnMultiplier(fuel.Offset + table.Rows * table.Cols, MultiplierKind.Linear), fuel.Multiplier);
                // Shown as a quarter of cell x multiplier, as the tuning programs for these ROMs show it.
                Assert.Equal((0.25f, 0f), (fuel.Scale, fuel.Bias));
            });
            Assert.All(new[] { ignLow, ignHigh }, ign => {
                Assert.Null(ign.Multiplier);
                Assert.Equal((0.25f, -6f, "° BTDC"), (ign.Scale, ign.Bias, ign.Unit));
            });
        });
        // Maps share axes, never cells or multipliers.
        var owned = defs.SelectMany(d => d.Multiplier is { } m ? new[] { new ByteRange(d.Offset, d.Offset + d.Length), new ByteRange(m.Offset, m.Offset + ((ParamKind.Table)d.Kind).Cols) } : [new ByteRange(d.Offset, d.Offset + d.Length)])
            .OrderBy(r => r.Start).ToList();
        Assert.All(owned.Zip(owned.Skip(1)), pair => Assert.True(pair.First.End <= pair.Second.Start, $"{pack.Id}: 0x{pair.First.Start:x} runs into 0x{pair.Second.Start:x}"));
    }

    /// <summary>No image can be two of these ROMs: every two packs disagree on a byte both compare.</summary>
    [Fact]
    public void NoTwoPacksCanMatchTheSameImage() {
        var packs = Packs();
        for (var a = 0; a < packs.Count; a++) {
            for (var b = a + 1; b < packs.Count; b++) {
                var both = from x in packs[a].Fingerprints from y in packs[b].Fingerprints select (x, y);
                Assert.All(both, pair => Assert.True(Contradict(pair.x, pair.y), $"`{packs[a].Id}` and `{packs[b].Id}` could both match one image"));
            }
        }
    }

    /// <summary>Whether two fingerprints expect different values of a bit that both compare.</summary>
    private static bool Contradict(Fingerprint x, Fingerprint y) {
        var expected = new Dictionary<int, (byte Value, byte Mask)>();
        foreach (var m in x.Bytes) {
            for (var i = 0; i < m.Value.Count; i++) expected[m.Address + i] = (m.Value[i], m.Mask is { } masks ? masks[i] : (byte)0xFF);
        }
        foreach (var m in y.Bytes) {
            for (var i = 0; i < m.Value.Count; i++) {
                if (!expected.TryGetValue(m.Address + i, out var other)) continue;
                var mask = (byte)(other.Mask & (m.Mask is { } masks ? masks[i] : 0xFF));
                if (((other.Value ^ m.Value[i]) & mask) != 0) return true;
            }
        }
        return false;
    }

    [Fact]
    public void PacksSayHowTheRomIsCheckedAndLogged() {
        var packs = Packs();
        // The ROMs that do not check their own sum.
        Assert.Equal(["ectune-273", "uberdata-p72"], packs.Where(p => p.Checksum.Kind == ChecksumKind.None).Select(p => p.Id).Order(StringComparer.Ordinal));
        Assert.All(packs.Where(p => p.Checksum.Kind == ChecksumKind.Sum8), p => {
            Assert.Equal((0, 0x8000), (p.Checksum.Start, p.Checksum.End));
            Assert.True(p.Checksum.Location is 0x7FFF or 0x003B);
        });
        // Only the firmware whose serial routine is part of what the pack recognises names a protocol.
        Assert.Equal(new DatalogSpec("ectune", null), Pack("ectune-273").Datalog);
        Assert.All(packs.Where(p => p.Id != "ectune-273"), p => Assert.Null(p.Datalog));
    }
}
