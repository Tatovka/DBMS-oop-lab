using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace MknImmiSql.Api.V1;

public class ExampleInput
{
    [Required] public String Str1 { get; set; }
    [Required] public String Str2 { get; set; }
}

public class ExampleOutput
{
    [Required] public String Result { get; set; }
}

[Route( "/api/v1/example" )]
public class ExampleController : Controller
{
    // Для возможности останавливать сервис можно объявить в контроллере
    // конструктор, принимающий объект IHostApplicationLifetime и
    // вызвать у него метод завершения приложения, как в примере.
    // Если данный котроллер не предполагает наличия кода, который
    // будет завершать приложение, код взятый в скобки, можно не объявлять.
    // {
    private readonly IHostApplicationLifetime _lifetime;
    
    public ExampleController( IHostApplicationLifetime lifetime )
    {
        _lifetime = lifetime;
    }
    // }
    
    [HttpPost]
    public IActionResult Post( [FromBody] ExampleInput input )
    {
        // Данное условие проверяет, что модель входных данных соответсвует описанной вами
        // структуре, с учетом всех атрибутов, которые вы задали. Хорошей практикой является
        // проверка модели передаваемых пользователем данных.
        if( !ModelState.IsValid )
            return BadRequest( ModelState );

        // Попробуйте вызвать через swagger-интерфейс эту ручку с соответсвующим значением
        // параметра и посмотрите что произойдет, если выбросить исключение. Подумайте
        // хорошо ли выдавать наружу (пользователям) ту информацию, что отдает сервер?
        if( input.Str1 == "exception" )
            throw new Exception( "Unhandled endpoint exception" );
        
        // Этот "костыль" демонстрирует как можно завершить приложение по внешнему вызову.
        if( input.Str1 == "terminate" )
        {
            _lifetime.StopApplication();
            return Ok();
        }
        
        // Это штатный возврат результата из endpoint'а.
        return Ok( new ExampleOutput
        {
            Result = input.Str1 + "+" + input.Str2
        } );
    }
}
