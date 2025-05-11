using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;
using Tables;

public interface ICommand
{
    public Int32 StatusCode { get; }
    Table Execute();

    public static IParserNode Next(IEnumerator<IParserNode> it)
    {
        if (!it.MoveNext()) throw new("Command body ends too early");
        return it.Current;
    }
}