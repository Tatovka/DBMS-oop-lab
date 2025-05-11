using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class DropTableCommand : ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    private readonly bool _ifExists;
    public string CommandName => "DROP TABLE";
    public DropTableCommand(List<IParserNode> args)
    {
        int nameIndex = 0;
        if (!(args[0] is Word)) throw new Exception($"Invalid argument to {CommandName}");
        if (args[0].Equals("IF EXISTS"))
        {
            nameIndex ++;
            _ifExists = true;
        }
        if (args.Count != 1 + nameIndex) 
            throw new Exception($"Wrong number of arguments to {CommandName}: {args.Count}\n" +
                                $"Expected: {1 + nameIndex}");
        if (args[nameIndex] is Word nameWord)
            _tableName = nameWord.GetName();
        else throw new Exception("table name should be a Word");
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