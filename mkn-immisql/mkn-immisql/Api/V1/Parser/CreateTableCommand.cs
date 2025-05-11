using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

//     -- Создание таблицы
// CREATE TABLE [IF NOT EXISTS] table_name (
//     column1_name TYPE [PRIMARY KEY] [NOT NULL] [DEFAULT value],
// column2_name TYPE [NOT NULL] [DEFAULT value],
// ...
// columnN_name TYPE [NOT NULL] [DEFAULT value],
// );

public class CreateTableCommand : ICommand
{
    private readonly String _tableName;
    private readonly SqlColumn[] _columns;
    private readonly bool _hasPk;

    public int StatusCode { get; private set; }

    enum ColumnParseState
    {
        Name, Type, Extra, Finished
    }
    
    public static string CommandName => "CREATE TABLE";
    private readonly bool _ifNotExists;
    public CreateTableCommand(List<IParserNode> args)
    {
        int nameIndex = 0;
        
        //Parsing flag
        if (args[0].Equals("IF NOT EXISTS"))
        {
            nameIndex++;
            _ifNotExists = true;
        }
        
        //Parsing name
        if (args[nameIndex] is Word nameWord && nameWord.IsName)
            _tableName = nameWord.GetName();
        else throw new Exception("table name should be a Word");
        if (args.Count == nameIndex + 1)
        {
            _columns = Array.Empty<SqlColumn>();
            return;
        }
        
        //Parsing argument block
        List<Word> colArgs = Parser.GetArgList(args[nameIndex + 1], out int columnsCount);
        
        //Parsing columns
        _columns = new SqlColumn[columnsCount];
        IEnumerator<Word> argEnumerator = colArgs.GetEnumerator();
        for (int curColumn = 0; curColumn < columnsCount; curColumn++)
        {
            _columns[curColumn] = ParseColumn(argEnumerator);
            if (_columns[curColumn].IsPKey)
            {
                if (!_hasPk) _hasPk = true;
                else throw new Exception("Primary key cannot be given twice");
            }
        }
        if (args.Count > nameIndex + 2) throw new Exception($"Too many arguments at {CommandName}");
    }

    public Table Execute()
    {
        if (Database.TryAddTable(_tableName, new Table(_columns)))
        {
            StatusCode = 200;
            return Table.Success;
        }
        
        StatusCode = _ifNotExists? 200 : 409;
        return Table.Failed;
    }

    private static SqlColumn ParseColumn(IEnumerator<Word> arg)
    {
        ColumnParseState state = ColumnParseState.Name;
        SqlColumn.ColumnBuilder builder = new();
        while(state != ColumnParseState.Finished && arg.MoveNext())
        {
            var value = arg.Current;
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
                    
                    if (value.Equals("NOT NULL"))
                    {
                        builder = builder.NotNullable;
                        break;
                    }
                    
                    if (value.Equals("PRIMARY KEY"))
                    {
                        builder = builder.HasPrimaryKey;
                        break;
                    }
                    
                    if (value.Equals("DEFAULT"))
                    {
                        arg.MoveNext();
                        builder = builder.WithDefault(arg.Current);
                        break;
                    }
                    throw new Exception($"Invalid argument {value}");
                }
            }
        }
        return builder.Create();
    }
}