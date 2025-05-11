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

    public void TryInsertRow(List<List<Word>> cols, List<List<Word>> row)
    {
        Word[] values = new Word[Columns.Length];
        values = values.Select(x => Word.Default).ToArray();
        for (int i = 0; i < cols.Count; i++)
        {
            if (cols[i].Count != 1) throw new ArgumentException($"Wrong argument as column name {cols[i]}");
            var colName = cols[i][0];
            if (_colMap.TryGetValue(colName.ToString(), out int index))
            {   
                if(row[i].Count != 1) 
                    throw new ArgumentException($"Wrong argument as value {row[i]}");
                values[index] = row[i][0];
            }
            else throw new Exception($"Table does not contains column with name {colName}");
        }
        AddRow(values);
    }

    public Table Select(List<Word> colNames)
    {
        var returnColumns = new SqlColumn[colNames.Count];
        for (int i = 0; i < returnColumns.Length; i++)
        {
            if (_colMap.TryGetValue(colNames[i].ToString(), out int index))
                returnColumns[i] = Columns[index].CopyThis();
            else throw new Exception($"Table does not contains column with name {colNames[i]}");
        }
        var resTable = new Table(returnColumns);
        resTable.RowCount = RowCount;
        return resTable;
    }

    public Table SelectRows(List<Word> colNames, Int32[] rows)
    {
        var returnColumns = new SqlColumn[colNames.Count];
        for (int i = 0; i < returnColumns.Length; i++)
        {
            if (_colMap.TryGetValue(colNames[i].ToString(), out int index))
                returnColumns[i] = Columns[index].CopyRows(rows);
            else throw new Exception($"Table does not contains column with name {colNames[i]}");
        }
        var resTable = new Table(returnColumns);
        resTable.RowCount = rows.Length;
        return resTable;
    }
}

