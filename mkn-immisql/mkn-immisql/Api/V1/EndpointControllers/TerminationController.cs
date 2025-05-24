using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace MknImmiSql.Api.V1;

public class TerminateToken
{
    public String? Token { get; set; } = String.Empty;
    public TerminateToken? token { get; set; }
    public override bool Equals(object other)
    {
        if (other is TerminateToken)
             return Token == (other as TerminateToken).Token;
        return false;
    }
}

public class NestedToken
{
    public TerminateToken Token { get; set; }
}

[Route("api/v1/terminate")]
public class TerminateController : Controller
{
    private readonly IHostApplicationLifetime _lifetime;
    public TerminateController(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }
    
    [HttpPost]
    public IActionResult Post( [FromBody] TerminateToken token )
    {
        if ( !ModelState.IsValid)
            return BadRequest( ModelState );
        
        if (token.Equals(ServiceContext.GetInstance().TerminationToken))
        {
            _lifetime.StopApplication();
            return Ok();
        }
        return StatusCode(403);
    }

}