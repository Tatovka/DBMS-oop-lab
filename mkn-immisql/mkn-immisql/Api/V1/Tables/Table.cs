using System;
using System.Collections.Generic;
using System.Linq;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql.Api.V1.Tables;

public class Table
{
    public readonly SqlColumn[] Columns;
    private static Table? SuccesfullTable;
    private static Table? FailedTable;
    private int RowCount;

    public void AddRow(ICollection<Word> words)
    {
        if (words.Count != Columns.Length) throw new ArgumentException("words.Count != Columns.Length");
        for (int i = 0; i < Columns.Length; i++)
            Columns[i].AddRow(words.ElementAt(i));
        RowCount++;
    }
    public Table(SqlColumn[] columns)
    {
        Columns = columns;
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
}

