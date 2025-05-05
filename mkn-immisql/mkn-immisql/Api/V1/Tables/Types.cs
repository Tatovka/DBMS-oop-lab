using System;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql.Api.V1.Tables;

public abstract class SqlType
{
    public bool IsNullable { get; }
    
    public abstract String TypeName { get; }

    public abstract ISqlValue Parse(Word word);

    public SqlType(Boolean isNullable)
    {
        IsNullable = isNullable;
    }
}

public interface ISqlValue
{
    public string Value { get; }
}

public class SqlNull : ISqlValue
{
    public string Value => "NULL";
}

public class SqlBoolean : SqlType
{
    public class SqlBoolValue : ISqlValue
    {
        public readonly Boolean _value;
        public String Value => _value.ToString().ToLower();
        public SqlBoolValue(Boolean value)
        {
            _value = value;
        }
    }
    public override String TypeName => "boolean";
    public override ISqlValue Parse(Word word)
    {
        if (word.Equals("NULL"))
        {
            if (IsNullable) return new SqlNull();
            throw new Exception($"Cannot assign null to non-nullable {TypeName}");
        }
        if (bool.TryParse(word.ToString(), out bool result))
            return new SqlBoolValue(result);
        throw new ArgumentException($"Cannot parse {TypeName} from value {word}");
    }
    public SqlBoolean(bool isNullable) : base(isNullable){ }
}

public class SqlInteger : SqlType
{
    public class SqlIntegerValue : ISqlValue
    {
        public readonly Int64 _value;
        public String Value => _value.ToString();
        public SqlIntegerValue(Int64 value)
        {
            _value = value;
        }
    }
    public override String TypeName => "integer";
    public override ISqlValue Parse(Word word)
    {
        if (word.Equals("NULL"))
        {
            if (IsNullable) return new SqlNull();
            throw new Exception($"Cannot assign null to non-nullable {TypeName}");
        }
        if (Int64.TryParse(word.ToString(), out Int64 result))
            return new SqlIntegerValue(result);
        throw new ArgumentException($"Cannot parse {TypeName} from value {word}");
    }
    public SqlInteger(bool isNullable) : base(isNullable){ }
}

public class SqlFloat : SqlType
{
    public class SqlFloatValue : ISqlValue
    {
        public readonly Double _value;
        public String Value => _value.ToString();
        public SqlFloatValue(Double value)
        {
            _value = value;
        }
    }
    public override String TypeName => "float";
    public override ISqlValue Parse(Word word)
    {
        if (word.Equals("NULL"))
        {
            if (IsNullable) return new SqlNull();
            throw new Exception($"Cannot assign null to non-nullable {TypeName}");
        }
        if (Double.TryParse(word.ToString(), out Double result))
            return new SqlFloatValue(result);
        throw new ArgumentException($"Cannot parse {TypeName} from value {word}");
    }
    public SqlFloat(bool isNullable) : base(isNullable){ }
}

public class SqlString : SqlType
{
    public class SqlStringValue : ISqlValue
    {
        public readonly String _value;
        public String Value => _value;
        public SqlStringValue(String value)
        {
            _value = value;
        }
    }
    public override String TypeName => "string";
    public override ISqlValue Parse(Word word)
    {
        if (word.Equals("NULL"))
        {
            if (IsNullable) return new SqlNull();
            throw new Exception($"Cannot assign null to non-nullable {TypeName}");
        }
        if (word.IsString)
        {
            var value = word.ToString();
            return new SqlStringValue(value.Substring(1, value.Length - 2));
        }
        throw new ArgumentException($"Cannot parse {TypeName} from value {word}");
    }
    public SqlString(bool isNullable) : base(isNullable){ }
}

public class SqlSerial : SqlType
{
    public class SqlSerialValue : ISqlValue
    {
        public readonly Int64 _value;
        public String Value => _value.ToString();
        public SqlSerialValue(Int64 value)
        {
            _value = value;
        }
    }
    public override String TypeName => "serial";
    public override ISqlValue Parse(Word word)
    {
        if (word.Equals("NULL"))
        {
            if (IsNullable) return new SqlNull();
            throw new Exception($"Cannot assign null to non-nullable {TypeName}");
        }
        if (Int64.TryParse(word.ToString(), out Int64 result))
            return new SqlSerialValue(result);
        throw new ArgumentException($"Cannot parse {TypeName} from value {word}");
    }
    public SqlSerial(bool isNullable) : base(isNullable){ }
}
public class DefaultSqlValue
{
    public readonly bool IsSpecified;
    public readonly ISqlValue Value;
    private DefaultSqlValue(SqlType type, bool isSpecified, Word value)
    {
        IsSpecified = isSpecified;
        Value = type.Parse(value);
    }
    private DefaultSqlValue(bool isSpecified)
    {
        IsSpecified = isSpecified;
        Value = new SqlNull();
    }
    public static DefaultSqlValue WithValue(SqlType type, Word val) => new (type, true, val);
    public static DefaultSqlValue NotSpecified => new (false);

    public DefaultValueInfo GetSchema()
    {
        var result = new DefaultValueInfo();
        result.IsSpecified = IsSpecified;
        if (IsSpecified)
        {
            if (Value is SqlNull) result.IsNull = true;
            else result.Value = Value.Value;
        }
        return result;
    }
}


