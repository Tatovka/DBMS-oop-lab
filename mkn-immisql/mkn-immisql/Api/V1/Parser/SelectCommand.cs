using System;
using System.Collections.Generic;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1.Parser;

public class SelectCommand : ICommand
{
    public static String CommandName => "Select";
    private String _tableName;
    public Int32 StatusCode { get; private set; }
    
    private List<Word> columnsNames;

    public SelectCommand(IParserNode[] args)
    {
        
    }
    
    public Table Execute()
    {
        return Table.Success;
    }
}