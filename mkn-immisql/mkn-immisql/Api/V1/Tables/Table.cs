using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql.Api.V1.Tables;

public class Table
{
    public readonly SqlColumn[] Columns;
    private ISqlValue[][] _data;
    private static Table? SuccesfullTable;
    private static Table? FailedTable;
    public int RowCount { get; private set; }
    private SqlColumn IdColumn;

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
    public Table(SqlColumn[] columns)
    {
        Columns = columns;
        for (int i = 0; i < Columns.Length; i++)
        {
            _colMap[columns[i].Name] = i;
        }
            
        _data = Array.Empty<ISqlValue[]>();
    }
    
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
    

    public Table SelectColumns(List<Word> colNames, Int32[] rows)
    {
        var returnColumns = new SqlColumn[colNames.Count];
        for (int i = 0; i < returnColumns.Length; i++)
        {
            returnColumns[i] = FindColumn(colNames[i]).CopyRows(rows);
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
            returnColumns[i] = Columns[i].CopyRows(rows);
        }
        var resTable = new Table(returnColumns);
        resTable.RowCount = rows.Length;
        return resTable;
    }
    
    public Int32[] RowsWhere(WhereCondition? condition)
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

    private SqlColumn FindColumn(Word name)
    {
        if (_colMap.TryGetValue(name.ToString(), out int index))
            return Columns[index];
        throw new Exception($"Table does not contains column with name {name}");
    }
}

