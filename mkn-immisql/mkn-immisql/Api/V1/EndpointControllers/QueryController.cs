using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MknImmiSql.Api.V1.Parser;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1;

public class QueryRequest
{ 
    [Required] public String Query { get; set; }
}

public class QueryResponse
{
    [Required] public PostTablesSchemaOutput Schema { get; set; }
    [Required] public String[][] TableRows { get; set; }

    public QueryResponse(Table table)
    {
        Schema = table.GetSchema();
        TableRows = table.Rows;
    }
}

[Route("/api/v1/query")]
public class QueryController : Controller
{
    [HttpPost]
    public IActionResult Post([FromBody] QueryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new QueryResponse(Table.Failed));
        ICommand command;
        try
        {
            Block mainBlock = Parser.Parser.Parse(request.Query);
            command = Parser.Parser.GetCommand(mainBlock);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(new QueryResponse(Table.Failed));
        }
        return Ok(new QueryResponse(command.Execute()));
    }
}