using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;
using System;
using System.Collections.Generic;

public class SqlColumn
{
    public class ColumnBuilder
    {
        private Word? _type;
        private string? _name;
        private bool _isPKey = false;
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
            _isPKey=true;
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
                if(!_isPKey) throw new Exception("Primary key is required with Serial type");
                colType = new SqlSerial(_isNullable);
            }
            else throw new Exception($"Invalid type name: {_type}");
            DefaultSqlValue colDefaultValue = _defaultValue is not null? 
                DefaultSqlValue.WithValue(colType, _defaultValue) : DefaultSqlValue.NotSpecified;
            return new SqlColumn(colType, _name, _isPKey, colDefaultValue);
        }
    }
    public readonly String Name;
    public readonly Boolean IsPKey;
    private List<ISqlValue> rows = new List<ISqlValue>();
    public readonly SqlType Type;
    public readonly DefaultSqlValue DefaultValue;
    private SqlColumn(SqlType type, String name, Boolean isPKey, DefaultSqlValue defaultValue)
    {
        Type = type;
        Name = name;
        IsPKey = isPKey;
        DefaultValue = defaultValue;
    }
    public void AddRow(Word value)
    {
        rows.Add(Type.Parse(value));
    }

    public string AtRow(int index) => rows[index].Value;
    
    public TableSchemaColumnInfo GetSchema()
    {
        TableSchemaColumnInfo result = new ();
        result.Name = Name;
        result.IsPKey = IsPKey;
        result.DefaultValue = DefaultValue.GetSchema();
        result.Type = Type.typeName;
        return result;
    }
    public static ColumnBuilder GetBuilder => new ColumnBuilder(); 
}