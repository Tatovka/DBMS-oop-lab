using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1;

public class SchemaControllerInput
{
    [Required] public string Name { get; set; }   
}

[Route("/api/v1/tables/schema")]
public class SchemaController: Controller
{
    [HttpPost]
    public IActionResult Post([FromBody] SchemaControllerInput input)
    {
        if(Database.TryGetTable(input.Name, out Table? output))
            return Ok(output!.GetSchema());
        return BadRequest(Table.Failed);
    }
}