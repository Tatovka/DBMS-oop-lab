using System.Linq;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql.Api.V1.Tables;
using System;
using System.Collections.Generic;

public class SqlColumn
{
    public class ColumnBuilder
    {
        private Word? _type;
        private string? _name;
        private bool _isPKey;
        private bool _isNullable = true;
        private Word? _defaultValue;
        
        public ColumnBuilder WithType(Word type)
        {
            _type = type;
            return this;
        }
        public ColumnBuilder WithName(Word name)
        {
            if (name.IsName)
            {
                _name = name.ToString();
                return this;
            }
            throw new Exception($"Invalid column name {name}");
        }

        public ColumnBuilder WithName(string name) => WithName(new Word(name));
        public ColumnBuilder WithType(string type) => WithType(new Word(type));
        public ColumnBuilder HasPrimaryKey { get { 
            _isPKey = true;
            _isNullable = false; 
            return this; 
        } }
        public ColumnBuilder WithDefault(Word value)
        {
            if(_defaultValue is null)
            {
                _defaultValue = value;
                return this;
            }
            throw new Exception("DEFAULT is given twice");
        }
        
        public ColumnBuilder NotNullable { get { _isNullable = false; return this; } }
        public SqlColumn Create()
        {
            if (_type is null) throw new Exception("Column type is not specified");
            if (_name is null) throw new Exception("Column name is not specified");
            SqlType colType;
            if (_type.Equals("BOOLEAN"))
                colType = new SqlBoolean(_isNullable);
            else if (_type.Equals("INTEGER"))
                colType = new SqlInteger(_isNullable);
            else if (_type.Equals("FLOAT"))
                colType = new SqlFloat(_isNullable);
            else if (_type.Equals("STRING")) 
                colType = new SqlString(_isNullable);
            else if (_type.Equals("SERIAL"))
            {
                if (!_isPKey) throw new Exception("Primary key is required with Serial type");
                if (_defaultValue is not null) throw new Exception("Cannot use default value with Serial");
                colType = new SqlSerial(_isNullable);
            }
            else throw new Exception($"Invalid type name: {_type}");
            DefaultSqlValue colDefaultValue = _defaultValue is not null? 
                DefaultSqlValue.WithValue(colType, _defaultValue) : DefaultSqlValue.NotSpecified;
            return _type.Equals("Serial")? new SerialColumn(_name) : 
                new SqlColumn(colType, _name, _isPKey, colDefaultValue);
        }
    }
    public readonly String Name;
    public readonly Boolean IsPKey;
    protected readonly List<ISqlValue> _rows = new ();
    public readonly SqlType Type;
    private readonly DefaultSqlValue _defaultValue;
    protected SqlColumn(SqlType type, String name, Boolean isPKey, DefaultSqlValue defaultValue)
    {
        Type = type;
        Name = name;
        IsPKey = isPKey;
        _defaultValue = defaultValue;
    }
    public void AddRow(Word value)
    {
        _rows.Add(Type.Parse(value));
    }

    public string AtRow(int index) => _rows[index].Value;

    public virtual ISqlValue Parse(Word word)
    {
        if (word.Equals("Default"))
        {
            if (_defaultValue.IsSpecified)
                _rows.Add(_defaultValue.Value);
            else throw new Exception("Cannot assign default value, it is not specified");
        }
        else _rows.Add(Type.Parse(word));
        return _rows.Last();
    }

    public TableSchemaColumnInfo GetSchema()
    {
        TableSchemaColumnInfo result = new ();
        result.Name = Name;
        result.IsPKey = IsPKey;
        result.DefaultValue = _defaultValue.GetSchema();
        result.Type = Type.TypeName;
        result.IsNullable = Type.IsNullable;
        return result;
    }
    public static ColumnBuilder GetBuilder => new ColumnBuilder();

    public SqlColumn CopyThis()
    {
        var result =  new SqlColumn(Type, Name, IsPKey, _defaultValue);
        foreach (var val in _rows)
        {
            result._rows.Add(val);
        }
        return result;
    }
}

public class SerialColumn : SqlColumn
{
    public SerialColumn(String name) : 
        base(new SqlSerial(false), name, true, DefaultSqlValue.NotSpecified) { }

    private Int64 _curValue;

    public override ISqlValue Parse(Word word)
    {
        if (!word.Equals(Word.Default)) throw new ArgumentException("Serial value sets automatically");
        _rows.Add(Type.Parse(new Word($"{++_curValue}")));
        return _rows.Last();
    }
}