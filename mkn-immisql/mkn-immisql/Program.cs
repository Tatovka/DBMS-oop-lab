using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MknImmiSql.Api.V1;
using MknImmiSql.Api.V1.Parser;

namespace MknImmiSql;

public static class Program
{
    public static void Main( String[] argv )
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        Keyword.AddKeyword(new Word("IF NOT EXISTS"));
        Keyword.AddKeyword(new Word("IF EXISTS"));
        Keyword.AddKeyword(new Word("CREATE TABLE"));
        Keyword.AddKeyword(new Word("PRIMARY KEY"));
        Keyword.AddKeyword(new Word("DROP TABLE"));
        Keyword.AddKeyword(new Word("DEFAULT"));
        Keyword.AddKeyword(new Word("NOT NULL"));
        Keyword.AddKeyword(new Word("INSERT INTO"));
        // "query": "INSERT INTO t1 (c1, c2, c3, c4) Values (true, 1, 1.2, 'aboba')"
        
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
