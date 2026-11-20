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

    [Fact]
    public void EveryPackLoadsWithAllItsParameters() {
        var packs = Packs();
        Assert.Equal(["crome-gold", "ectune-273", "obd1-p28", "obd1-p30", "obd1-p72"], packs.Select(p => p.Id).Order());
        Assert.All(packs, pack => {
            Assert.Empty(pack.Problems);
            Assert.Equal(pack.Params.Count, pack.Defs.Count);
            Assert.Equal(RomPlatform.Obd1Oki66207, pack.Platform);
            Assert.Equal(0x8000, pack.RomSize);
            Assert.False(string.IsNullOrWhiteSpace(pack.Name));
        });
    }

    /// <summary>
    /// A fingerprint that compared table data would stop matching once the table is tuned, and
    /// one that stated a size would refuse a dump with a stray byte after the ROM.
    /// </summary>
    [Fact]
    public void FingerprintsCompareOnlyBytesNoParameterCovers() {
        Assert.All(Packs(), pack => {
            var spans = pack.Defs.SelectMany(d => d.Spans()).ToList();
            Assert.All(pack.Fingerprints, fingerprint => {
                Assert.Null(fingerprint.Size);
                Assert.Null(fingerprint.Sha256);
                Assert.NotEmpty(fingerprint.Bytes);
                Assert.All(fingerprint.Bytes, match => {
                    var compared = new ByteRange(match.Address, match.Address + match.Value.Count);
                    Assert.DoesNotContain(spans, span => span.Start < compared.End && compared.Start < span.End);
                    Assert.False(pack.Checksum.Kind != ChecksumKind.None && compared.Contains(pack.Checksum.Location));
                });
            });
        });
    }

    /// <summary>Editing a parameter must not disturb the checksum byte, nor another parameter's cells.</summary>
    [Fact]
    public void ParametersDoNotCoverTheChecksumByteOrEachOthersCells() {
        Assert.All(Packs(), pack => {
            if (pack.Checksum.Kind != ChecksumKind.None) {
                Assert.All(pack.Defs, d => Assert.DoesNotContain(d.Spans(), span => span.Contains(pack.Checksum.Location)));
            }
            // Maps share axes, never cells or multipliers.
            var data = pack.Defs.Select(d => (d.Name, Cells: new ByteRange(d.Offset, d.Offset + d.Length), d.Multiplier, Table: (ParamKind.Table)d.Kind)).ToList();
            var owned = data.SelectMany(d => d.Multiplier is { } m ? new[] { d.Cells, new ByteRange(m.Offset, m.Offset + d.Table.Cols) } : [d.Cells]).OrderBy(r => r.Start).ToList();
            Assert.All(owned.Zip(owned.Skip(1)), pair => Assert.True(pair.First.End <= pair.Second.Start, $"{pack.Id}: 0x{pair.First.Start:x} runs into 0x{pair.Second.Start:x}"));
        });
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

    /// <summary>The layout every OBD1 pack describes: four main maps per set, fuel with one multiplier per column.</summary>
    [Fact]
    public void EveryPackHasTheMainMapsOfEachCam() {
        Assert.All(Packs(), pack => {
            Assert.Equal(0, pack.Defs.Count % 4);
            Assert.All(pack.Defs.Chunk(4), set => {
                Assert.Equal(["Fuel", "Fuel", "Ignition", "Ignition"], set.Select(d => d.Category));
                Assert.Equal([false, true, false, true], set.Select(d => d.Vtec));
                Assert.All(set, d => Assert.IsType<ParamKind.Table>(d.Kind));
                var (fuelLow, fuelHigh, ignLow, ignHigh) = (set[0], set[1], set[2], set[3]);
                // A map and its other-cam twin differ in rows at most; both tables of a cam share the rpm axis.
                Assert.Equal(fuelLow.Kind, ignLow.Kind);
                Assert.Equal(fuelHigh.Kind, ignHigh.Kind);
                Assert.Equal(fuelLow.RowAxis, ignLow.RowAxis);
                Assert.Equal(fuelHigh.RowAxis, ignHigh.RowAxis);
                Assert.Equal(AxisEncoding.LogBytes, fuelLow.RowAxis.Encoding);
                Assert.Equal(AxisEncoding.Bytes, fuelHigh.RowAxis.Encoding);
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
        });
    }

    [Fact]
    public void OnlyTheEctunePackNamesADatalogProtocol() {
        Assert.Equal(new DatalogSpec("ectune", null), Pack("ectune-273").Datalog);
        Assert.Equal(ChecksumKind.None, Pack("ectune-273").Checksum.Kind);
        Assert.All(Packs().Where(p => p.Id != "ectune-273"), p => {
            Assert.Null(p.Datalog);
            Assert.Equal(ChecksumKind.Sum8, p.Checksum.Kind);
            Assert.Equal((0, 0x8000), (p.Checksum.Start, p.Checksum.End));
        });
    }
}
