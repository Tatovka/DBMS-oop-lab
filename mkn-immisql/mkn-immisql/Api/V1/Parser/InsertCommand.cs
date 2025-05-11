using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class InsertCommand : ParserIterator, ICommand
{
    public static String CommandName => "INSERT INTO";
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private List<List<Word>> rows = new ();
    
    private List<Word> columnsNames;
    
    private List<Word> returningColumns = new ();
    public InsertCommand(List<IParserNode> args) : base(args)
    {
        //Parsing name
        _tableName = NextWord.GetName();
        //Parsing argument block
        if (!MoveNext()) throw new Exception("Column names should be given");
        if (Current is not Block) throw new Exception("Column names should be given in brackets");
        columnsNames = ArgumentsList.FromBlock((Block)Current).Flatten;
        if (!NextWord.Equals("Values")) 
            throw new Exception("Values keyword is not found in Insert command body");
        while (MoveNext() && Current is Block row)
        {
            rows.Add(ArgumentsList.FromBlock(row).Flatten);
            if (!MoveNext()) return; if (!Current.Equals(",")) break;
        }
        if (StreamEnds) return;
        
        if (CurrentWord.Equals("Returning"))
            returningColumns = ArgumentsList.UntilEnd(this).Flatten;
        else throw new Exception($"Unexpected argument in {CommandName} after Values: {Current}");
    }

    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out var table))
        {
            Table retTable = Table.Empty;
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