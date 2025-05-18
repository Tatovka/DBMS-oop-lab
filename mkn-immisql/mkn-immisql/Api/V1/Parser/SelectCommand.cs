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

    public WhereCondition(ParserIterator it)
    {
        try
        {
            ColName = it.NextWord;
            Op = it.NextWord;
            Value = it.NextWord;
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

    public OrderCondition(ParserIterator it)
    {
        try
        {
            ColName = it.NextWord;
            Direction = it.NextWord;
        } catch (Exception e)
        {
            throw new ArgumentException($"Cannot parse order condition: {e}");
        }
    }
}

public class SelectCommand : ParserIterator, ICommand
{
    private readonly String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private readonly List<Word> _columnsNames = new ();
    private readonly List<Word> _columnsResultNames = new ();
    private readonly bool _selectAll;
    
    private readonly OrderCondition? _orderCondition;
    private readonly WhereCondition? _whereConditions;
    private readonly Int64? _limit;
    private ICommand workingTable;
    public SelectCommand(List<IParserNode> args) : base(args)
    { 
        if (args[0].Equals("*")) 
        { 
            _selectAll = true; 
            MoveNext(); MoveNext();
        }
        else GetNames(ArgumentsList.Until(this, "From"));
        if (StreamEnds || !CurrentWord.Equals("From")) 
            throw new FormatException("From keyword not found in a select command");
        var tableArg = Next;
        if (tableArg is Word word)
        {
            if (word.IsKeyword)
            {
                workingTable = Parser.GetCommand(new Block(this));
                return;
            }
            if (word.IsName) workingTable = new NameCommand(word);
            
            else throw new ArgumentException($"Cannot parse table from: {word}");
        }
        else workingTable = Parser.GetCommand(tableArg as Block);
        //Parse flags
        while (MoveNext())
        {
            if (CurrentWord.Equals("Where"))
            {
                if (_whereConditions is null)
                    _whereConditions = new WhereCondition(this);
                else throw new ArgumentException("Where condition was given twice");
            }
            else if (CurrentWord.Equals("Order By"))
            {
                if (_orderCondition is null)
                    _orderCondition = new OrderCondition(this);
                else throw new ArgumentException("Order condition was given twice");
            }
            else if (CurrentWord.Equals("Limit"))
            {
                if (_limit is null) 
                    _limit = Int64.Parse(NextWord.ToString());
                else throw new ArgumentException("Limit condition was given twice");
            }
            else throw new ArgumentException($"Unknown Select command flag: {CurrentWord}");
        }
    }

    void GetNames(ArgumentsList names)
    {
        while (names.MoveNext)
        {
            var curList = names.Current;
            if (curList.Count != 1 && curList.Count != 3) 
                throw new Exception("Column name statement doesn't template Name [As ResultName]");
            Word name = new(curList[0].GetName());
            Word resultName = name;
            if (curList.Count == 3)
            {
                if (curList[1].Equals("As"))
                    resultName = new(curList[2].GetName());
                else throw new Exception($"As expected, but found: {curList[1]}");
            }
            _columnsNames.Add(name);
            _columnsResultNames.Add(resultName);
        }
    }
    
    public Table Execute()
    {
        Table table = workingTable.Execute();
        if (workingTable.StatusCode == 200)
        {
            StatusCode = 200;
            var rows = table!.RowsWhere(_whereConditions);
            rows = table.OrderRowsBy(_orderCondition, rows);
            if (_limit is not null) rows = rows.Take((int)_limit.Value).ToArray();
            return _selectAll
                ? table.SelectAllColumns(rows)
                : table.SelectColumns(_columnsNames, rows, _columnsResultNames);
        }
        StatusCode = 404;
        return Table.Failed;
    }
}