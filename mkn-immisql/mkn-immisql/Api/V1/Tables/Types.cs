using System;
using System.Collections.Generic;
using System.Globalization;
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

    public bool CompareWith(Word op, ISqlValue other) => false;
    
}

public class SqlValueComparer : IComparer<ISqlValue>
{
    public int Compare(ISqlValue x, ISqlValue y)
    {
        if (x.CompareWith(new Word("<"), y)) return -1;
        if (x.CompareWith(new Word(">"), y)) return 1;
        return 0;
    }

    public static SqlValueComparer Comparer { get; } = new();
}
public readonly struct SqlNull : ISqlValue
{
    public string Value => "NULL";

    public bool CompareWith(Word op, ISqlValue other)
    {
        if (op.Equals("=")) return other is SqlNull;
        if (op.Equals("!=")) return other is not SqlNull;
        if (op.Equals("=") || op.Equals("!=")) return false;
        throw new ArgumentException("Cannot compare Null with ordinal operators");
    }
    
}

public class SqlBoolean : SqlType
{
    public readonly struct SqlBoolValue : ISqlValue
    {
        private readonly Boolean _value;
        public String Value => _value.ToString().ToLower();
        public SqlBoolValue(Boolean value)
        {
            _value = value;
        }
        public bool CompareWith(Word op, ISqlValue other)
        {
            if (op.Equals("=") && other.Value == Value) return true;
            if (op.Equals("!=") && other.Value != Value) return true;
            if (op.Equals("=") || op.Equals("!=")) return false;
            throw new ArgumentException("Cannot compare Boolean with ordinal operators");
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
    public readonly struct SqlIntegerValue : ISqlValue
    {
        private readonly Int64 _value;
        public String Value => _value.ToString();
        public SqlIntegerValue(Int64 value)
        {
            _value = value;
        }
        public bool CompareWith(Word op, ISqlValue other)
        {
            if (other is SqlIntegerValue otherSqlVal)
            {
                Int64 otherVal = otherSqlVal._value;
                if (op.Equals("=")) return otherVal.Equals(_value);
                if (op.Equals("!=")) return !otherVal.Equals(_value);
                if (op.Equals(">")) return _value > otherVal;
                if (op.Equals("<")) return _value < otherVal;
                if (op.Equals(">=")) return _value >= otherVal;
                if (op.Equals("<=")) return _value <= otherVal;
                throw new ArgumentException("Unknown operator");
            }
            if (op.Equals("=")) return false;
            if (op.Equals("!=")) return true;
            throw new ArgumentException($"Cannot compare integer with {other.Value}");
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
    public readonly struct SqlFloatValue : ISqlValue
    {
        private readonly Double _value;
        public String Value => _value.ToString(CultureInfo.InvariantCulture);
        public SqlFloatValue(Double value)
        {
            _value = value;
        }
        public bool CompareWith(Word op, ISqlValue other)
        {
            if (other is SqlFloatValue otherSqlVal)
            {
                Double otherVal = otherSqlVal._value;
                if (op.Equals("=")) return otherVal.Equals(_value);
                if (op.Equals("!=")) return !otherVal.Equals(_value);
                if (op.Equals(">")) return _value > otherVal;
                if (op.Equals("<")) return  _value >  otherVal;
                if (op.Equals(">=")) return _value <= otherVal;
                if (op.Equals("<=")) return _value >= otherVal;
                throw new ArgumentException("Unknown operator");
            }
            if (op.Equals("=")) return false;
            if (op.Equals("!=")) return true;
            throw new ArgumentException($"Cannot compare float with {other.Value}");
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
    public readonly struct SqlStringValue : ISqlValue
    {
        private readonly String _value;
        public String Value => _value;
        public SqlStringValue(String value)
        {
            _value = value;
        }

        public bool CompareWith(Word op, ISqlValue other)
        {
            if (other is SqlStringValue)
            {
                string otherVal = other.Value;
                int compareResult = String.Compare(Value, otherVal, false, CultureInfo.InvariantCulture);
                if (op.Equals("=")) return compareResult == 0;
                if (op.Equals("!=")) return compareResult != 0;
                if (op.Equals(">")) return compareResult > 0;
                if (op.Equals("<")) return compareResult < 0;
                if (op.Equals(">=")) return compareResult >= 0;
                if (op.Equals("<=")) return compareResult <= 0;
                throw new ArgumentException("Unknown operator");
            }
            if (op.Equals("=")) return false;
            if (op.Equals("!=")) return true;
            throw new ArgumentException($"Cannot compare string with {other.Value}");
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

public class SqlSerial : SqlInteger
{
    public override String TypeName => "serial";
    public SqlSerial(bool isNullable) : base(isNullable) { }
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
