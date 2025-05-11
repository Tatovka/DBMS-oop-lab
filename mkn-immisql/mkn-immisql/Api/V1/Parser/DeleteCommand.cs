using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class DeleteCommand : ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly WhereCondition? _whereConditions;
    private readonly List<Word>? _returningColumns;
    
    public DeleteCommand(List<IParserNode> args)
    {
        IEnumerator<IParserNode> it = args.GetEnumerator();
        if (ICommand.Next(it) is Word nameWord)
            _tableName = nameWord.GetName();
        else throw new ArgumentException("Expected word as table name");

        while (it.MoveNext())
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
                    while (it.MoveNext() && !it.Current.Equals("Where"))
                    {
                        if (it.Current is Word colName)
                            blockWords.Add(colName);
                        else throw new ArgumentException($"Expected column name, but was { it.Current }");
                    }
                    var retBlock = new Block(blockWords);
                    _returningColumns = Parser.FlatArgList(Parser.SplitArgList(Parser.GetArgList(retBlock, out int _)));
                }
                else throw new ArgumentException($"Unknown Delete argument {it.Current}");
            }
            else throw new ArgumentException($"Expected keyword, but was {it.Current}");
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