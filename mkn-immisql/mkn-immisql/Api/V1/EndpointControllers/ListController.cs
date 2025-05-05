using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MknImmiSql.Api.V1.Tables;

namespace MknImmiSql.Api.V1;

public class GetTablesOutput
{
    [Required] public String[] Tables => Database.ListTables;
}

[Route("/api/v1/tables/list")]
public class ListController : Controller
{
    [HttpGet]
    public GetTablesOutput Get() => new();
}