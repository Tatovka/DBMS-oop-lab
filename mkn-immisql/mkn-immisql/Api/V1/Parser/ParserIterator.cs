using System;
using System.Collections.Generic;

namespace MknImmiSql.Api.V1.Parser;

public class ParserIterator
{
    private int _streamIndex = -1;
    private List<IParserNode> _sourceList;
    public IParserNode Next
    {
        get
        {
            if (++_streamIndex >= _sourceList.Count) 
                throw new IndexOutOfRangeException("Failed to parse command: stream ends");
            return _sourceList[_streamIndex];
        }
    }
    public Word NextWord => Next.AsWord;
    public IParserNode Current => _sourceList[_streamIndex];
    public Word CurrentWord => Current.AsWord;
    public bool StreamEnds => _streamIndex >= _sourceList.Count;

    public bool MoveNext()
    {
        if (_streamIndex < _sourceList.Count)
        {
            ++_streamIndex;
            return _streamIndex < _sourceList.Count;
        }
        return false;
    }

    public ParserIterator(List<IParserNode> list)
    {
        _sourceList = list;
    }
}