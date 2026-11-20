using HRE.Core;

namespace HRE.App;

/// <summary>One category of the parameter tree and the parameters under it.</summary>
public sealed record ParamCategory(string Name, IReadOnlyList<ParamDef> Params);

/// <summary>Arranges parameters for the tree: by category, in the order the definitions list them.</summary>
public static class ParamTree {
    public static IReadOnlyList<ParamCategory> Build(IEnumerable<ParamDef> defs) {
        var order = new List<string>();
        var byCategory = new Dictionary<string, List<ParamDef>>(StringComparer.Ordinal);
        foreach (var def in defs) {
            if (!byCategory.TryGetValue(def.Category, out var list)) {
                byCategory[def.Category] = list = [];
                order.Add(def.Category);
            }
            list.Add(def);
        }
        return [.. order.Select(name => new ParamCategory(name, byCategory[name]))];
    }
}
