using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class DropTableCommand : ParserIterator, ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    private readonly bool _ifExists;
    public string CommandName => "DROP TABLE";
    public DropTableCommand(List<IParserNode> args) : base(args)
    {
        if (NextWord.Equals("IF EXISTS"))
        {
            MoveNext();
            _ifExists = true;
        }
        _tableName = CurrentWord.GetName();
        if (MoveNext()) throw new ArgumentException($"unexpected argument: {Current}");
    }

    public Table Execute()
    {
        if (Database.TryDropTable(_tableName))
        {
            StatusCode = 200;
            return Table.Success;
        }
        StatusCode = _ifExists? 200 : 404;
        return Table.Failed;
    }
}