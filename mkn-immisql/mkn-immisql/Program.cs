using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MknImmiSql.Api.V1;

namespace MknImmiSql;

public static class Program
{
    public static void Main( String[] argv )
    {
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder( argv );

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddControllers();
            ServiceContext.GetInstance();
            using( WebApplication app = builder.Build() )
            {
                if( app.Environment.IsDevelopment() )
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.MapControllers();
                app.Run();
            }
        }
        catch( Exception e )
        {
            Console.WriteLine( $"Fatal error: {e.Message}" );
            Console.WriteLine( e.StackTrace );
        }
    }
}
