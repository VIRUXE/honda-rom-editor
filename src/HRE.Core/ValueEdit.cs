namespace HRE.Core;

/// <summary>
/// A setting, which is a single value, as the user reads and writes it: its text, the range it
/// may take, and the byte writes that set it or move it a step. Values are in display units;
/// what is stored is rounded to the nearest step and kept inside what the definition allows.
/// </summary>
public static class ValueEdit {
    /// <summary>Whether the definition lets the value be changed: a setting whose smallest and largest value are the same is fixed.</summary>
    public static bool CanChange(ParamDef def) => def.Kind is ParamKind.Word word && word.Min != word.Max && def.Scale != 0f;

    /// <summary>The value as it is shown.</summary>
    public static float Value(RomImage image, ParamDef def) => def.Display(ParamAccess.Word(image, def));

    /// <summary>The smallest and the largest value the setting can show.</summary>
    public static (float Min, float Max) Range(ParamDef def) {
        var word = (ParamKind.Word)def.Kind;
        var (a, b) = (def.Display(word.Min), def.Display(word.Max));
        return (MathF.Min(a, b), MathF.Max(a, b));
    }

    /// <summary>The smallest change the setting can take, as it is shown.</summary>
    public static float Step(ParamDef def) => MathF.Abs(def.Scale);

    /// <summary>How many decimals the value is written with: as few as tell one step from the next.</summary>
    public static int Decimals(ParamDef def) => Step(def) > 0f ? TableEdit.DecimalsFor(Step(def)) : 0;

    /// <summary>What the setting shows, without its unit.</summary>
    public static string Text(RomImage image, ParamDef def) => TableEdit.Text(Value(image, def), Decimals(def));

    /// <summary>Whether the setting can hold <paramref name="display"/>, give or take half a step.</summary>
    public static bool Fits(ParamDef def, float display) {
        var word = (ParamKind.Word)def.Kind;
        var raw = (display - def.Bias) / (double)def.Scale;
        return raw >= word.Min - 0.5 && raw <= word.Max + 0.5;
    }

    /// <summary>What is stored to show <paramref name="display"/>: the nearest step, inside what the definition allows.</summary>
    public static ushort Stored(ParamDef def, float display) {
        var word = (ParamKind.Word)def.Kind;
        return (ushort)Math.Clamp(Math.Round((display - def.Bias) / (double)def.Scale, MidpointRounding.AwayFromZero), word.Min, word.Max);
    }

    /// <summary>The byte writes that set the value to <paramref name="display"/>. None for a fixed value.</summary>
    public static (int Address, byte Value)[] Set(ParamDef def, float display) =>
        CanChange(def) ? ParamAccess.WordWrites(def, Stored(def, display)) : [];

    /// <summary>
    /// The byte writes that raise (or, with a negative count, lower) the value by
    /// <paramref name="steps"/> of its smallest step. "Raise" is what the user sees: with a
    /// negative scale the stored value goes down.
    /// </summary>
    public static (int Address, byte Value)[] Move(RomImage image, ParamDef def, int steps) {
        if (!CanChange(def)) return [];
        var word = (ParamKind.Word)def.Kind;
        var raw = ParamAccess.Word(image, def) + (def.Scale < 0f ? -steps : steps);
        return ParamAccess.WordWrites(def, (ushort)Math.Clamp(raw, word.Min, word.Max));
    }
}
