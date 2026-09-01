using GridReport.Core.Domain;

namespace GridReport.Tests;

public sealed class DomainTests
{
    [Fact]
    public void DataValue_keeps_value_provenance_and_confirmation_state()
    {
        var source = new DataProvenance("客户参数.xlsx", "逆变器!D17", "Excel 单元格", "额定输出电压");
        var value = DataValue.Auto("额定输出电压", "400", source);

        var confirmed = value.Confirm();

        Assert.Equal("400", confirmed.Value);
        Assert.Equal("客户参数.xlsx", confirmed.Provenance.FileName);
        Assert.Equal(FieldStatus.Confirmed, confirmed.Status);
        Assert.True(confirmed.IsConfirmed);
    }

    [Fact]
    public void ProjectData_replaces_field_without_losing_previous_audit_entry()
    {
        var project = new GridProject { Name = "XX光伏涉网试验项目" };
        var initial = DataValue.Auto("项目名称", "旧名称", DataProvenance.Manual("初次导入"));
        var replacement = DataValue.Manual("项目名称", "新名称", "工程师校核");

        project.SetValue(initial);
        project.SetValue(replacement);

        Assert.Equal("新名称", project.Values["项目名称"].Value);
        Assert.Single(project.AuditEntries);
        Assert.Contains("旧名称", project.AuditEntries[0].OldValue);
    }
}
