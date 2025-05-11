using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class WhereCondition
{
    public readonly Word ColName;
    public readonly Word Op;
    public readonly Word Value;

    public WhereCondition(IEnumerator<IParserNode> it)
    {
        try
        {
            ColName = ICommand.Next(it).AsWord;
            Op = ICommand.Next(it).AsWord;
            Value = ICommand.Next(it).AsWord;
        } catch (Exception e)
        {
            throw new ArgumentException($"Cannot parse where condition: {e}");
        }
    }
}
public class OrderCondition
{
    public readonly Word ColName;
    public readonly Word Direction;

    public OrderCondition(IEnumerator<IParserNode> it)
    {
        try
        {
            ColName = ICommand.Next(it).AsWord;
            Direction = ICommand.Next(it).AsWord;
        } catch (Exception e)
        {
            throw new ArgumentException($"Cannot parse order condition: {e}");
        }
    }
}

public class SelectCommand : ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly List<Word> _columnsNames = new ();
    private readonly bool _selectAll;
    
    private readonly OrderCondition? _orderCondition;
    private readonly WhereCondition? _whereConditions;
    private readonly Int64? _limit;
    public SelectCommand(List<IParserNode> args)
    {
        IEnumerator<IParserNode> it = args.GetEnumerator();
        if (ICommand.Next(it).Equals("*"))
        {
            _selectAll = true;
            if (!ICommand.Next(it).Equals("From")) 
                throw new ArgumentException($"Expected From, but found: {it.Current}");
        } else
        {
            List<Word> blockWords = new();
            while (!it.Current.Equals("From"))
            {
                if (it.Current is Word colName)
                    blockWords.Add(colName);
                else throw new ArgumentException($"Expected column name, but was {it.Current}");
                if (!it.MoveNext()) throw new Exception("From is not found after Select");
            }
            var retBlock = new Block(blockWords);
            _columnsNames = Parser.FlatArgList(Parser.SplitArgList(Parser.GetArgList(retBlock, out int _)));
        }
        
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
                
                else if (kWord.Equals("Order By"))
                {
                    if (_orderCondition is null)
                        _orderCondition = new OrderCondition(it);
                    else throw new ArgumentException("Order condition was given twice");
                }
                
                else if (kWord.Equals("Limit"))
                {
                    if (_limit is null) 
                        _limit = Int64.Parse(ICommand.Next(it).AsWord.ToString());
                    else throw new ArgumentException("Limit condition was given twice");
                }
                else throw new ArgumentException("Unknown Select argument");
            }
            else throw new ArgumentException($"Expected keyword, but was {it.Current}");
        }
    }
    
    public Table Execute()
    {
        if (Database.TryGetTable(_tableName, out var table))
        {
            StatusCode = 200;
            var rows = table!.RowsWhere(_whereConditions);
            rows = table.OrderRowsBy(_orderCondition, rows);
            if (_limit is not null) rows = rows.Take((int)_limit.Value).ToArray();
            return _selectAll? table.SelectAllColumns(rows) : table.SelectColumns(_columnsNames, rows);
        }
        StatusCode = 404;
        return Table.Failed;
    }
}