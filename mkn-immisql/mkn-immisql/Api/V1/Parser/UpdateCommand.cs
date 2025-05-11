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

public class UpdateCommand : ICommand
{
        private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly WhereCondition? _whereConditions;
    private readonly List<Word>? _returningColumns;
    private readonly List<SetExpression> _setExpressions = new();
    
    public UpdateCommand(List<IParserNode> args)
    {
        IEnumerator<IParserNode> it = args.GetEnumerator();
        if (ICommand.Next(it) is Word nameWord)
            _tableName = nameWord.GetName();
        else throw new ArgumentException("Expected word as table name");
        
        if (!ICommand.Next(it).Equals("Set")) throw new ArgumentException("Set not found in Update request");

        bool shouldContinue = false;
        List<Word> setBlockWords = new();
        while (it.MoveNext())
        {
            if (it.Current.AsWord.IsKeyword)
            {
                shouldContinue = true;
                break;
            }
            if (it.Current is Word colName)
                setBlockWords.Add(colName);
            else throw new ArgumentException($"Expected column name, but was { it.Current }");
        }
        var block = new Block(setBlockWords);
        var setArgs = Parser.SplitArgList(Parser.GetArgList(block, out int _));
        foreach (var setExpr in setArgs)
            _setExpressions.Add(new SetExpression(setExpr));
        
        if (shouldContinue) do
        {
            if (it.Current is Word kWord)
            {
                if (kWord.Equals("Where"))
                {
                    if (_whereConditions is null)
                        _whereConditions = new WhereCondition(it);
                    else throw new ArgumentException("Where condition was given twice");
                }
                else if (kWord.Equals("Returning"))
                {
                    if (_returningColumns is not null) 
                        throw new ArgumentException("Returning was given twice");
                    List<Word> blockWords = new();
                    while (it.MoveNext() && !it.Current.AsWord.IsKeyword)
                    {
                        if (it.Current is Word colName)
                            blockWords.Add(colName);
                        else throw new ArgumentException($"Expected column name, but was { it.Current }");
                    }
                    var retBlock = new Block(blockWords);
                    _returningColumns = Parser.FlatArgList(Parser.SplitArgList(Parser.GetArgList(retBlock, out int _)));
                }
                else throw new ArgumentException($"Unknown Update argument {it.Current}");
            }
            else throw new ArgumentException($"Expected keyword, but was {it.Current}");
        } while (it.MoveNext());
        
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
                table.SelectColumns(_returningColumns, updatingRows) : Table.Empty;
            return result;
        }
        StatusCode = 404;
        return Table.Failed;
    }
}