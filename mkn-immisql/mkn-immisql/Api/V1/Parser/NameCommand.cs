using System;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class NameCommand : ICommand
{
    private String _tableName;

    public int StatusCode { get; private set; } = 200;

    public NameCommand(Word name)
    {
        _tableName = name.GetName();
    }

    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out Table table))
            return table;
        StatusCode = 404;
        return Table.Empty;
    }
}