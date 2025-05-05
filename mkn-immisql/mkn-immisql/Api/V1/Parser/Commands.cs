using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;
using Tables;

public interface ICommand
{
    public string CommandName { get; }
    public Int32 StatusCode { get; }
    Table Execute();
}

public class CreateTableCommand : ICommand
{
    private readonly String tableName;
    public readonly SqlColumn[] columns;
    private readonly int primaryKeyIndex = -1;

    public int StatusCode { get; private set; }

    enum ColumnParseState
    {
        Name, Type, Extra, Finished
    }
    //     -- Создание таблицы
    // CREATE TABLE [IF NOT EXISTS] table_name (
    //     column1_name TYPE [PRIMARY KEY] [NOT NULL] [DEFAULT value],
    // column2_name TYPE [NOT NULL] [DEFAULT value],
    // ...
    // columnN_name TYPE [NOT NULL] [DEFAULT value],
    // );
    public string CommandName => "CREATE TABLE";
    private readonly bool _ifNotExists = false;
    public CreateTableCommand(List<ParserNode> args)
    {
        int nameIndex = 0;
        if (!(args[0] is Word)) throw new Exception($"Invalid argument to {CommandName}");
        
        if (args[0].Equals("IF"))
        {
            nameIndex += 3;
            if (args.Count != 5) 
                throw new Exception($"Wrong number of arguments to {CommandName}: {args.Count}\n" +
                                    "Expected: 5");
            if (!args[1].Equals("NOT") || !args[2].Equals("EXISTS")) 
                throw new Exception($"Invalid argument to {CommandName} {args[0]} {args[1]} {args[2]}\n" +
                                    "Expected: IF NOT EXISTS");
            _ifNotExists = true;
        }
        
        if (args.Count != 2 + nameIndex) 
            throw new Exception($"Wrong number of arguments to {CommandName}: {args.Count}\n" +
                                $"Expected: {2 + nameIndex}");
        
        List<ParserNode> colArgs;
        if (args[nameIndex] is Word nameWord && args[nameIndex + 1] is Block argsBlock)
        {
            tableName = nameWord.ToString(); 
            if (argsBlock.HasBlocks) throw new Exception($"{CommandName} arguments should not contain blocks");
            colArgs = argsBlock.children;
            if (colArgs.Count == 0 || !colArgs.Last().Equals(",")) colArgs.Add(new Word(","));
        }
        //Parsing columns
        else throw new Exception($"Invalid argument to {CommandName}: " +
                            $"{args[nameIndex].GetType()}, {args[nameIndex+1].GetType()}\n Expected: Word, Block");
        int columnsCount = colArgs.Count(word => word.Equals(","));
        columns = new SqlColumn[columnsCount];
        IEnumerator<ParserNode> argEnumerator = colArgs.GetEnumerator();
        for (int curColumn = 0; curColumn < columnsCount; curColumn++)
        {
            columns[curColumn] = ParseColumn(argEnumerator);
            if (columns[curColumn].IsPKey)
            {
                if (primaryKeyIndex == -1) primaryKeyIndex = curColumn;
                else throw new Exception("Primary key cannot be given twice");
            }
        }
    }

    public Table Execute()
    {
        if (Database.TryAddTable(tableName, new Table(columns)))
        {
            StatusCode = 200;
            return Table.Success;
        }
        
        StatusCode = _ifNotExists? 200 : 409;
        return Table.Failed;
    }

    private static SqlColumn ParseColumn(IEnumerator<ParserNode> arg)
    {
        ColumnParseState state = ColumnParseState.Name;
        SqlColumn.ColumnBuilder builder = new();
        while(state != ColumnParseState.Finished && arg.MoveNext())
        {
            var value = (arg.Current as Word)!;
            switch (state)
            {
                case ColumnParseState.Name:
                { 
                    builder = builder.WithName(value);
                    state = ColumnParseState.Type;
                    break;
                }
                case ColumnParseState.Type:
                {
                    builder = builder.WithType(value);
                    state = ColumnParseState.Extra;
                    break;
                }
                case ColumnParseState.Extra:
                {
                    if (value.Equals(","))
                    {
                        state = ColumnParseState.Finished; 
                        break;
                    }
                    
                    if (value.Equals("NOT"))
                    {
                        if(!arg.MoveNext()) throw new Exception($"Expected NULL after NOT");
                        value = (arg.Current as Word)!;
                        if (!value.Equals("NULL")) 
                            throw new Exception($"Expected NULL after NOT");
                        builder = builder.NotNullable;
                        break;
                    }
                    
                    if (value.Equals("PRIMARY"))
                    {
                        if(!arg.MoveNext()) throw new Exception("Expected KEY after PRIMARY");
                        value = (arg.Current as Word)!;
                        if (!value.Equals("KEY")) 
                            throw new Exception("Expected KEY after PRIMARY");
                        builder = builder.HasPrimaryKey;
                        break;
                    }
                    
                    if (value.Equals("DEFAULT"))
                    {
                        if(!arg.MoveNext()) throw new Exception("Expected value after DEFAULT");
                        if (arg.Current is Word defaultWord)
                            builder = builder.WithDefault(defaultWord);
                        break;
                    }
                    
                    throw new Exception($"Invalid argument {value}");
                }
            }
        }
        return builder.Create();
    }
}

public class DropTableCommand : ICommand
{
    private String tableName;
    public Int32 StatusCode { get; private set; }
    private bool _ifExists = false;
    public string CommandName => "DROP TABLE";
    public DropTableCommand(List<ParserNode> args)
    {
        int nameIndex = 0;
        if (!(args[0] is Word)) throw new Exception($"Invalid argument to {CommandName}");
        if (args[0].Equals("IF"))
        {
            nameIndex += 2;
            if (args.Count != 3) 
                throw new Exception($"Wrong number of arguments to {CommandName}: {args.Count}\n" +
                                    "Expected: 3");
            if (!args[1].Equals("EXISTS")) 
                throw new Exception($"Invalid argument to {CommandName} {args[0]} {args[1]}\n" +
                                    "Expected: IF EXISTS");
            _ifExists = true;
        }
        if (args.Count != 1 + nameIndex) 
            throw new Exception($"Wrong number of arguments to {CommandName}: {args.Count}\n" +
                                $"Expected: {1 + nameIndex}");
        if (args[nameIndex] is Word nameWord)
            tableName = nameWord.ToString();
        else throw new Exception("table name should be a Word");
    }

    public Table Execute()
    {
        if (Database.TryDropTable(tableName))
        {
            StatusCode = 200;
            return Table.Success;
        }
        StatusCode = _ifExists? 200 : 404;
        return Table.Failed;
    }
}
