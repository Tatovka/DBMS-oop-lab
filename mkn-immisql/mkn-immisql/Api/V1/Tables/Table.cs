using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql.Api.V1.Tables;

public class Table
{
    public SqlColumn[] Columns;
    internal ISqlValue[][] _data;
    private static Table? SuccesfullTable;
    private static Table? FailedTable;
    public int RowCount { get; private set; }

    private readonly Dictionary<String, Int32> _colMap = new();

    public void AddRow(ICollection<Word> words)
    {
        if (words.Count != Columns.Length) throw new ArgumentException("words.Count != Columns.Length");
        ISqlValue[] row = new ISqlValue[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
            row[i] = Columns[i].Parse(words.ElementAt(i));
        _data = _data.Append(row).ToArray();
        RowCount++;
    }
    public void AddRow(ICollection<ISqlValue> values)
    {
        if (values.Count != Columns.Length) throw new ArgumentException("values.Count != Columns.Length");
        ISqlValue[] row = new ISqlValue[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
        {
            Columns[i].AddRow(values.ElementAt(i));
            row[i] = values.ElementAt(i);
        }
        _data = _data.Append(row).ToArray();
        RowCount++;
    }
    
    public Table(SqlColumn[] columns)
    {
        Columns = columns;
        for (int i = 0; i < Columns.Length; i++)
        {
            if (!_colMap.TryAdd(columns[i].Name, i)) 
                throw new ArgumentException($"Column with name {columns[i].Name} already exists");
        }
        _data = Array.Empty<ISqlValue[]>();
    }
    public static readonly Table Empty = new Table(Array.Empty<SqlColumn>());
    public static Table Success
    {
        get
        {
            if (SuccesfullTable is null)
            {
                SqlColumn col = SqlColumn.GetBuilder.WithName("result").WithType("boolean").NotNullable.Create();
                SuccesfullTable = new Table( new []{col});
                SuccesfullTable.AddRow(new Word[]{ new ("true") });
            }
            return SuccesfullTable;
        }
    }

    public static Table Failed
    {
        get
        {
            if (FailedTable is null)
            {
                SqlColumn col = SqlColumn.GetBuilder.WithName("result").WithType("boolean").NotNullable.Create();
                FailedTable = new Table(new []{col}); 
                FailedTable.AddRow(new Word[]{ new ("false") });
            }
            return FailedTable;
        }
    }

    public PostTablesSchemaOutput GetSchema()
    {
        TableSchemaInfo schema = new TableSchemaInfo();
        schema.Columns = new TableSchemaColumnInfo[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
            schema.Columns[i] = Columns[i].GetSchema();
        var result = new PostTablesSchemaOutput();
        result.Schema = schema;
        return result;
    }

    public String[][] Rows
    {
        get
        {
            var result = new String[RowCount][];
            for (int row = 0; row < RowCount; row++)
            {
                result[row] = new string[Columns.Length];
                for (int col = 0; col < Columns.Length; col++)
                {
                    result[row] [col] = Columns[col].AtRow(row);
                }
            }
            return result;
        }
    }

    public void InsertRow(List<Word> cols, List<Word> row)
    {
        Word[] values = new Word[Columns.Length];
        values = values.Select(x => Word.Default).ToArray();
        for (int i = 0; i < cols.Count; i++)
        {
            var colName = cols[i];
            if (_colMap.TryGetValue(colName.ToString(), out int index))
                values[index] = row[i];
            else throw new Exception($"Table does not contains column with name {colName}");
        }
        AddRow(values);
    }
    

    public Table SelectColumns(List<Word> colNames, Int32[] rows, List<Word> resultNames)
    {
        var returnColumns = new SqlColumn[colNames.Count];
        for (int i = 0; i < returnColumns.Length; i++)
        {
            returnColumns[i] = FindColumn(colNames[i]).CopyRows(rows, resultNames[i].ToString());
        }
        var resTable = new Table(returnColumns);
        resTable.RowCount = rows.Length;
        return resTable;
    }

    public Table SelectAllColumns(Int32[] rows)
    {
        var returnColumns = new SqlColumn[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
        {
            returnColumns[i] = Columns[i].CopyRows(rows, Columns[i].Name);
        }
        var resTable = new Table(returnColumns);
        resTable.RowCount = rows.Length;
        return resTable;
    }

    public void RemoveRows(Int32[] indexes)
    {
        if (indexes.Any(el => el >= RowCount)) throw new ArgumentException("Row index is out of range");
        foreach (var col in Columns)
            col.RemoveRows(indexes);
        RowCount -= indexes.Length;
    }

    public Int32[] AllRows =>  Enumerable.Range(0, RowCount).ToArray();
    
    public void UpdateRows(List<SetExpression> setExpressions, Int32[] indexes)
    {
        if (indexes.Any(el => el >= RowCount)) throw new ArgumentException("Row index is out of range");
        var oldTable = SelectAllColumns(AllRows);
        try
        {
            foreach (var expr in setExpressions)
            {
                var column = FindColumn(expr.ColumnName);
                column.UpdateRows(expr.Value, indexes);
            }
        }
        catch (Exception)
        {
            Columns = oldTable.Columns;
            throw;
        }
    }
    
    public Int32[] RowsWhere(WhereCondition? condition)
    {
        if (condition is null) return Enumerable.Range(0, RowCount).ToArray();
        SqlColumn column = FindColumn(condition.ColName);
        return column.RowsWhere(condition);
    }
    public Int32[] RowsOn(WhereCondition? condition)
    {
        if (condition is null) return Enumerable.Range(0, RowCount).ToArray();
        SqlColumn column = FindColumn(condition.ColName);
        return column.RowsWhere(condition);
    }
    public Int32[] OrderRowsBy(OrderCondition? condition, Int32[] indexes)
    {
        if (condition is null) return indexes;
        SqlColumn column = FindColumn(condition.ColName);
        return column.OrderRowsByThis(condition.Direction, indexes);
    }

    public SqlColumn FindColumn(Word name)
    {
        if (_colMap.TryGetValue(name.GetName(), out int index))
            return Columns[index];
        throw new Exception($"Table does not contains column with name {name}");
    }

    public JoinTable Concat(Table right, String name1, String name2, bool nullable)
    {
        SqlColumn[] cols = new SqlColumn[right.Columns.Length + Columns.Length];
        for (int col = 0; col < Columns.Length; col++)
            cols[col] = Columns[col].JoinCopy(name1, nullable);
        for (int col = 0; col < right.Columns.Length; col++)
            cols[col + Columns.Length] = right.Columns[col].JoinCopy(name2, nullable);
        var result = new JoinTable(cols, this, right, name1, name2);
        return result;
    }

    
}

public class JoinTable : Table
{
    private Table _lTable;
    private Table _rTable;
    private String _lName;
    private String _rName;

    public JoinTable(SqlColumn[] cols, Table left, Table right, String name1, String name2) : base(cols)
    {
        _lTable = left;
        _rTable = right;
        _lName = name1;
        _rName = name2;
    }

    private Table GetTable(string name)
    {
        if (name == _lName) return _lTable;
        if (name == _rName) return _rTable;
        throw new ArgumentException($"Table with name {name} doesn't exists in current context");
    }
    
    private SqlColumn GetColumn(Tuple<Word, Word> tuple) => 
        GetTable(tuple.Item1.GetName()).FindColumn(tuple.Item2);
    
    
    public Table InnerJoin(OnCondition condition)
    {
        var leftColumn = GetColumn(condition.LeftColName);
        var rightColumn = GetColumn(condition.RightColName);
        var lcTable = condition.LeftColName.Item1.GetName();
        var rcTable = condition.RightColName.Item1.GetName();
        for (int rL = 0; rL < _lTable._data.Length; ++rL)
        {
            var lRow = _lTable._data[rL];
            for (int rR = 0; rR < _rTable._data.Length; ++rR)
            {
                var rRow = _rTable._data[rR];
                if (leftColumn.ValueAtRow(lcTable.Equals(_lName)? rL : rR)
                    .CompareWith(condition.Op, rightColumn.ValueAtRow(rcTable.Equals(_lName)? rL : rR)))
                    AddRow(lRow.Concat(rRow).ToArray());
            }
        }
        return this;
    }
    public Table LeftJoin(OnCondition condition)
    {
        var leftColumn = GetColumn(condition.LeftColName);
        var rightColumn = GetColumn(condition.RightColName);
        var lcTable = condition.LeftColName.Item1.GetName();
        var rcTable = condition.RightColName.Item1.GetName();
        var nulls = new ISqlValue[_rTable.Columns.Length];
        Array.Fill(nulls, SqlNull.Instance);
        for (int rL = 0; rL < _lTable._data.Length; ++rL)
        {
            bool shouldAddNull = true;
            var lRow = _lTable._data[rL];
            for (int rR = 0; rR < _rTable._data.Length; ++rR)
            {
                var rRow = _rTable._data[rR];
                if (leftColumn.ValueAtRow(lcTable.Equals(_lName)? rL : rR)
                    .CompareWith(condition.Op, rightColumn.ValueAtRow(rcTable.Equals(_lName)? rL : rR)))
                {
                    AddRow(lRow.Concat(rRow).ToArray());
                    shouldAddNull = false;
                }
            }
            if (shouldAddNull) AddRow(lRow.Concat(nulls).ToArray());;
        }
        return this;
    }
    public Table RightJoin(OnCondition condition)
    {
        var leftColumn = GetColumn(condition.LeftColName);
        var rightColumn = GetColumn(condition.RightColName);
        var lcTable = condition.LeftColName.Item1.GetName();
        var rcTable = condition.RightColName.Item1.GetName();
        var nulls = new ISqlValue[_lTable.Columns.Length];
        Array.Fill(nulls, SqlNull.Instance);
        for (int rR = 0; rR < _rTable._data.Length; ++rR)
        {
            bool shouldAddNull = true;
            var rRow = _rTable._data[rR];
            for (int rL = 0; rL < _lTable._data.Length; ++rL)
            {
                var lRow = _lTable._data[rL];
                if (leftColumn.ValueAtRow(lcTable.Equals(_lName)? rL : rR)
                    .CompareWith(condition.Op, rightColumn.ValueAtRow(rcTable.Equals(_lName)? rL : rR)))
                {
                    AddRow(lRow.Concat(rRow).ToArray());
                    shouldAddNull = false;
                }
            }
            if (shouldAddNull) AddRow(nulls.Concat(rRow).ToArray());;
        }
        return this;
    }
    
}
