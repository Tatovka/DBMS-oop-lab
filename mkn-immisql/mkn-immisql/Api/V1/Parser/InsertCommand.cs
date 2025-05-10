using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class InsertCommand : ICommand
{
    public static String CommandName => "INSERT INTO";
    private String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private List<List<List<Word>>> rows = new ();
    
    private List<List<Word>> columnsNames;
    
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
        columnsNames = Parser.SplitArgList(Parser.GetArgList(it.Current, out int colCount));
        if (!it.MoveNext() || !it.Current.Equals("VALUES")) throw new Exception("Expected values to insert");
        while (it.MoveNext() && it.Current is Block row)
        {
            var rowArgs = Parser.GetArgList(row, out int argsCount);
            if (argsCount != colCount) throw new Exception("Wrong values count");
            rows.Add(Parser.SplitArgList(rowArgs));
            if (!it.MoveNext()) return; if (!it.Current.Equals(",")) break;
        }
        
        if (it.Current.Equals("RETURNING"))
        {
            while (it.MoveNext() && it.Current is Word colName)
            {
                returningColumns.Add(colName);
                if (!it.MoveNext()) break;
                if (!it.Current.Equals(","))
                    throw new ArgumentException($"Expected ',' or end of command, but was {it.Current}");
            }
            if (it.MoveNext()) throw new Exception($"Unexpected argument in {CommandName} after Returning: {it.Current}");
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
                table!.TryInsertRow(columnsNames, row);
                if (returningColumns.Count != 0)
                    retTable = table.Select(returningColumns);
            }
            StatusCode = 200;
            return retTable;
        }
        StatusCode = 404;
        return Table.Failed;
    }

}