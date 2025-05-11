using System;

namespace MknImmiSql.Api.V1.Parser;

public interface  IParserNode
{
    bool Equals(String? other) => false;

    Word AsWord
    {
        get
        {
            throw new Exception("Expected Word but conversion not found");
        }
    }
}