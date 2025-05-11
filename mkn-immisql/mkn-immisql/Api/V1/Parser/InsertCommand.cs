using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class InsertCommand : ICommand
{
    public static String CommandName => "INSERT INTO";
    private String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private List<List<Word>> rows = new ();
    
    private List<Word> columnsNames;
    
    private List<Word> returningColumns = new ();
    public InsertCommand(List<IParserNode> args)
    {
        //Parsing name
        var it = args.GetEnumerator();
        it.MoveNext();
        if (it.Current is Word nameWord && nameWord.IsName)
            _tableName = nameWord.ToString();
        else throw new Exception("Table name should be a Word");
        //Parsing argument block
        if (!it.MoveNext()) throw new Exception("Column names should be given");
        columnsNames = Parser.FlatArgList(Parser.SplitArgList(Parser.GetArgList(it.Current, out int colCount)));
        if (!it.MoveNext() || !it.Current.Equals("VALUES")) throw new Exception("Expected values to insert");
        while (it.MoveNext() && it.Current is Block row)
        {
            var rowArgs = Parser.GetArgList(row, out int argsCount);
            if (argsCount != colCount) throw new Exception("Wrong values count");
            rows.Add(Parser.FlatArgList(Parser.SplitArgList(rowArgs)));
            if (!it.MoveNext()) return; if (!it.Current.Equals(",")) break;
        }
        
        if (it.Current.Equals("RETURNING"))
        {
            List<Word> blockWords = new();
            while (it.MoveNext())
            {
                if (it.Current is Word colName)
                    blockWords.Add(colName);
                else throw new ArgumentException($"Expected column name, but was { it.Current }");
            }
            var retBlock = new Block(blockWords);
            returningColumns = Parser.FlatArgList(Parser.SplitArgList(Parser.GetArgList(retBlock, out int _)));
        }
        else throw new Exception($"Unexpected argument in {CommandName} after Values: {it.Current}");
    }

    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out var table))
        {
            Table retTable = Table.Success;
            foreach (var row in rows)
            {
                try
                {
                    table!.InsertRow(columnsNames, row);
                }
                catch (PrimaryKeyException)
                {
                    StatusCode = 409;
                    return Table.Failed;
                }
            }
            if (returningColumns.Count != 0) 
                retTable = table!.SelectColumns(returningColumns, 
                    Enumerable.Range(table.RowCount-rows.Count, rows.Count).ToArray());
            StatusCode = 200;
            return retTable;
        }
        StatusCode = 404;
        return Table.Failed;
    }

}