using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class DeleteCommand : ParserIterator, ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly WhereCondition? _whereConditions;
    private readonly List<Word>? _returningColumns;
    
    public DeleteCommand(List<IParserNode> args) : base(args)
    {
        _tableName = NextWord.GetName();
        while (MoveNext())
        {
            if (CurrentWord.Equals("Where"))
            {
                if (_whereConditions is not null) 
                    throw new ArgumentException("Where condition was given twice");
                _whereConditions = new WhereCondition(this);
            }
            else if (CurrentWord.Equals("Returning"))
            {
                if (_returningColumns is not null)
                    throw new ArgumentException("Returning was given twice");
                _returningColumns = ArgumentsList.UntilKeyword(this).Flatten;
            }
            else throw new ArgumentException($"Unknown Delete command flag {Current}");
        }
    }
    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out var table))
        {
            StatusCode = 200;
            var removingRows = table!.RowsWhere(_whereConditions);
            Table result = _returningColumns is not null? 
                table.SelectColumns(_returningColumns, removingRows) : Table.Empty;
            table.RemoveRows(removingRows);
            return result;
        }
        StatusCode = 404;
        return Table.Failed;
    }
}