using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MknImmiSql.Api.V1;

public class ServiceInfo
{
    [Required] public String Timestamp { get; }
    [Required] public Int32 ProcessId { get; }

    public ServiceInfo()
    {
        Timestamp = DateTime.Now.ToString( "O" );
        ProcessId = Environment.ProcessId;
    }
}

[Route( "/api/v1/info" )]
public class InfoController : Controller
{
    [HttpGet]
    public ServiceInfo Get()
    {
        return new ServiceInfo();
    }
}
