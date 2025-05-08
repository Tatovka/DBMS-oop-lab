using System;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;
using Tables;

public interface ICommand
{
    public Int32 StatusCode { get; }
    Table Execute();
}