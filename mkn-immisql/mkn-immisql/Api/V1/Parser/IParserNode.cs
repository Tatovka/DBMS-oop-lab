using System;

namespace MknImmiSql.Api.V1.Parser;

public interface  IParserNode
{
    public bool Equals(IParserNode other);

    public bool Equals(String str) => false;

}