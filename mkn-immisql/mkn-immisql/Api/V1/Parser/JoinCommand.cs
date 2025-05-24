using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class OnCondition
{
    public readonly Tuple<Word,Word> LeftColName;
    public readonly Word Op;
    public readonly Tuple<Word,Word> RightColName;

    public OnCondition(ParserIterator it)
    {
        try
        {
            LeftColName = it.NextWord.SplitTableCol;
            Op = it.NextWord;
            RightColName = it.NextWord.SplitTableCol;
            
        } catch (Exception e)
        {
            throw new ArgumentException($"Cannot parse On condition: {e}");
        }
    }
}

public class JoinCommand : ParserIterator, ICommand
{
    public enum JoinType
    {
        Left,
        Right,
        Inner
    }
    private JoinType _type;
    
    private String _leftTableName;
    private String _rightTableName;
    private OnCondition _condition;

    public int StatusCode { get; private set; } = 200;

    public JoinCommand(List<IParserNode> args) : base(args)
    {
        _leftTableName = NextWord.AsWord.GetName();
        Word commandName = NextWord.AsWord;
        if (commandName.Equals("Join") || commandName.Equals("Inner Join"))
            _type = JoinType.Inner;
        else if (commandName.Equals("Right Join"))
            _type = JoinType.Right;
        else if (commandName.Equals("Left Join"))
            _type = JoinType.Left;
        else throw new ArgumentException($"Invalid command: {commandName}");
        _rightTableName = NextWord.AsWord.GetName();
        
        if (MoveNext())
        {
            if (CurrentWord.Equals("On"))
            {
                _condition = new OnCondition(this);
            }
            else throw new ArgumentException($"Expected On, but was: {CurrentWord}");
        }
        else throw new ArgumentException("Cannot find On in the Join operator");
    }

    public Table Execute()
    {
        
        if (Database.TryGetTable(_leftTableName, out var leftTable) &&
            Database.TryGetTable(_rightTableName, out var rightTable))
        {
            JoinTable resultTable = leftTable!
                .Concat(rightTable!, _leftTableName, _rightTableName, _type != JoinType.Inner);
            switch (_type)
            {
                case JoinType.Inner:
                    return resultTable.InnerJoin(_condition);
                case JoinType.Left:
                    return resultTable.LeftJoin(_condition);
                case JoinType.Right:
                    return resultTable.RightJoin(_condition);
            }
        }
        StatusCode = 404;
        return Table.Empty;
    }
}