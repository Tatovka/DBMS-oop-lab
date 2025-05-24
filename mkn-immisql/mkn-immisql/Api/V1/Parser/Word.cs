using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;

public class Word : IParserNode
{
    private readonly String _value;
    public static readonly Word Empty = new (String.Empty); 
    public static readonly Word Default = new ("Default"); 
    private static readonly Regex NameFormat = new (@"^[\w-_\.]+$");
    
    public bool IsString => (_value.Length > 0) && (_value[0] == '\'');
    
    public Word(String str)
    {
        _value = str;
    }
    
    public override string ToString()
    {
        return _value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Word otherWord) return _value.Equals(otherWord._value, StringComparison.InvariantCultureIgnoreCase);
        if (obj is String str) return Equals(str);
        return false;
    }

    public bool Equals(String? other)
    {
        if (other is null) return Equals(Empty);
        return other.Equals(_value, StringComparison.InvariantCultureIgnoreCase);;
    }

    public String GetName()
    {
        if (!IsName) throw new($"Invalid format: {_value} cannot be a name");
        if (_value[0] == '\"' && _value.Last() == '\"') return _value.Substring(1, _value.Length - 2);
        return _value;
    }
    
    public Word AsWord  => this;
    public bool IsName => (_value[0] == '"' && _value.Last() == '"') || NameFormat.IsMatch(_value);
    public bool IsKeyword => Keyword.KeywordList.Contains(this);

    public bool IsCommandName => Keyword.CommandNames.Contains(this);

    public Tuple<Word, Word> SplitTableCol
    {
        get
        {
            var splited = _value.Split('.', 2);
            return new (new Word(splited[0]), new Word(splited[1]));
        }
    }

    public override int GetHashCode()
    {
        return _value.ToLowerInvariant().GetHashCode();
    }
}