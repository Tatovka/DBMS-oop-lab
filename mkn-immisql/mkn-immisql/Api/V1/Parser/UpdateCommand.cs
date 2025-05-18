using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class SetExpression
{
    public Word ColumnName;
    public Word Value;

    public SetExpression(List<Word> it)
    {
        try
        {
            if (it.Count != 3) throw new Exception("Invalid set expression");
            ColumnName = it[0];
            if (!it[1].Equals("=")) 
                throw new ArgumentException($"Expected =, but found {it[1]}");
            Value = it[2];
        } catch (Exception e)
        {
            throw new ArgumentException($"Cannot parse where condition: {e}");
        }
    }
    
}

public class UpdateCommand : ParserIterator, ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly WhereCondition? _whereConditions;
    private readonly List<Word>? _returningColumns;
    private readonly List<SetExpression> _setExpressions = new();
    
    public UpdateCommand(List<IParserNode> args) : base(args)
    {
        _tableName = NextWord.GetName();
        if (!Next.Equals("Set")) 
            throw new ArgumentException("Set not found in Update request");
        var setArgs = ArgumentsList.UntilKeyword(this);
        foreach (var setExpr in setArgs.Data)
            _setExpressions.Add(new SetExpression(setExpr));
        //Parse flags
        while (!StreamEnds)
        {
            if (CurrentWord.Equals("Where"))
            {
                if (_whereConditions is null)
                    _whereConditions = new WhereCondition(this);
                else throw new ArgumentException("Where condition was given twice");
            }
            else if (CurrentWord.Equals("Returning"))
            {
                if (_returningColumns is not null) 
                    throw new ArgumentException("Returning was given twice");
                _returningColumns = ArgumentsList.UntilKeyword(this).Flatten;
            }
            else throw new ArgumentException($"Unknown Update argument: {Current}");
            MoveNext();
        };
    }
    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out var table))
        {
            StatusCode = 200;
            var updatingRows = table!.RowsWhere(_whereConditions);
            try
            {
                table.UpdateRows(_setExpressions, updatingRows);
            } catch (PrimaryKeyException)
            {
                StatusCode = 409;
                return Table.Failed;
            }

            Table result = _returningColumns is not null? 
                table.SelectColumns(_returningColumns, updatingRows, _returningColumns) : Table.Empty;
            return result;
        }
        StatusCode = 404;
        return Table.Failed;
    }
}