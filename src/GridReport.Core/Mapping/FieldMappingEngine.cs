using GridReport.Core.Domain;
using GridReport.Core.Template;

namespace GridReport.Core.Mapping;

public sealed class FieldMappingEngine
{
    private readonly Dictionary<string, string[]> _aliases;
    public FieldMappingEngine(IReadOnlyDictionary<string, string[]>? aliases = null)
    {
        _aliases = aliases is null ? DefaultAliases() : new(aliases, StringComparer.OrdinalIgnoreCase);
    }

    public List<FieldMapping> Match(IEnumerable<TemplateField> fields, IEnumerable<DataValue> values)
    {
        var data = values.GroupBy(x => x.Field, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        return fields.Select(field => Map(field, data)).ToList();
    }

    private FieldMapping Map(TemplateField field, IReadOnlyDictionary<string, List<DataValue>> values)
    {
        if (values.TryGetValue(field.Name, out var exact))
            return exact.Count == 1 ? new(field, exact[0], FieldStatus.ExactMatched, field.IsRequired) : new(field, null, FieldStatus.Conflict, field.IsRequired);
        foreach (var alias in AliasesFor(field.Name))
            if (values.TryGetValue(alias, out var candidates))
                return candidates.Count == 1 ? new(field, candidates[0], FieldStatus.Suggested, field.IsRequired) : new(field, null, FieldStatus.Conflict, field.IsRequired);
        return new(field, null, FieldStatus.Missing, field.IsRequired);
    }

    private IEnumerable<string> AliasesFor(string field) => _aliases.TryGetValue(field, out var aliases) ? aliases : [];
    private static Dictionary<string, string[]> DefaultAliases() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["客户名称"] = ["委托单位", "委托方", "建设单位"],
        ["项目名称"] = ["工程名称", "电站名称"],
        ["装机容量"] = ["总容量", "容量"],
        ["逆变器型号"] = ["逆变器型号规格"],
        ["测试日期"] = ["试验日期", "检测日期"]
    };
}
