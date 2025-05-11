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

public class CreateTableCommand :  ParserIterator, ICommand
{
    private readonly String _tableName;
    private readonly SqlColumn[] _columns;
    private readonly bool _hasPk;

    public int StatusCode { get; private set; }
    public static string CommandName => "CREATE TABLE";
    private readonly bool _ifNotExists;
    public CreateTableCommand(List<IParserNode> args) : base(args)
    {
        if (NextWord.Equals("If Exists"))
        {
            MoveNext();
            _ifNotExists = true;
        }
        _tableName = CurrentWord.GetName();
        
        //Parsing columns
        if (!MoveNext())
        {
            _columns = Array.Empty<SqlColumn>();
            return;
        }
        List<List<Word>> colArgs;
        if (Current is Block block) 
            colArgs = ArgumentsList.FromBlock(block).Data;
        else throw new ArgumentException($"expected block of arguments, but found: {Current}");
        _columns = new SqlColumn[colArgs.Count];
        for (int curColumn = 0; curColumn < _columns.Length; curColumn++)
        {
            _columns[curColumn] = ParseColumn(colArgs[curColumn]);
            if (_columns[curColumn].IsPKey)
            {
                if (!_hasPk) _hasPk = true;
                else throw new Exception("Primary key cannot be given twice");
            }
        }
        if (MoveNext()) throw new Exception($"Too many arguments at {CommandName}");
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

    private static SqlColumn ParseColumn(List<Word> args)
    {
        ParserIterator it = new ParserIterator(args.Select(x => x as IParserNode).ToList());
        SqlColumn.ColumnBuilder builder = new();
        builder = builder.WithName(it.NextWord).WithType(it.NextWord);
        while (it.MoveNext())
        {
            if (it.CurrentWord.Equals("Primary Key")) 
                builder = builder.HasPrimaryKey;
            else if (it.CurrentWord.Equals("Not Null"))
                builder = builder.NotNullable;
            else if (it.CurrentWord.Equals("Default"))
                builder = builder.WithDefault(it.NextWord);
        }
        return builder.Create();
    }
}