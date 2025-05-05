using System;
using System.ComponentModel.DataAnnotations;

namespace MknImmiSql.Api.V1.Tables;
public class DefaultValueInfo
{
    [Required] public Boolean IsSpecified { get; set; } = false;
    [Required] public Boolean IsNull { get; set; } = false;
    [Required] public String Value { get; set; } = "";
}

public class TableSchemaColumnInfo
{
    [Required] public String Name { get; set; }
    [Required] public String Type { get; set; }
    [Required] public Boolean IsPKey { get; set; }
    [Required] public Boolean IsNullable { get; set; }
    [Required] public DefaultValueInfo DefaultValue { get; set; }
}

public class TableSchemaInfo
{
    [Required] public TableSchemaColumnInfo[] Columns { get; set; }
}

public class PostTablesSchemaOutput
{
    [Required] public TableSchemaInfo Schema { get; set; }
}