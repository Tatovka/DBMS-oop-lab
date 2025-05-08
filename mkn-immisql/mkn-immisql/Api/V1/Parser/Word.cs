using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;

public class Word : IParserNode
{
    private readonly String _value;
    public static readonly Word Empty = new (String.Empty); 
    private static readonly Regex NameFormat = new (@"^[\w-_]+$");
    
    public bool IsString => (_value.Length > 0) && (_value[0] == '\'');
    
    public Word(String str)
    {
        _value = str;
    }
    
    public override string ToString()
    {
        return _value;
    }

    public override bool Equals(object obj)
    {
        if (obj is Word otherWord) return _value.Equals(otherWord._value, StringComparison.InvariantCultureIgnoreCase);
        if (obj is String str) return str.Equals(_value, StringComparison.InvariantCultureIgnoreCase);
        return false;
    }
    public bool IsName => (_value[0] == '"' && _value.Last() == '"') || NameFormat.IsMatch(_value);
    public override int GetHashCode()
    {
        return _value.ToLowerInvariant().GetHashCode();
    }
}