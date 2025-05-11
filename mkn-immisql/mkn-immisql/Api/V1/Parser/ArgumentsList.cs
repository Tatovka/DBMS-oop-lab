using System;
using System.Collections.Generic;
using System.Linq;

namespace MknImmiSql.Api.V1.Parser;

public class ArgumentsList
{
    public readonly List<List<Word>> Data;

    public Int32 Size => Data.Count;
    public ArgumentsList(List<Word> data)
    {
        if (data.Count == 0 || !data.Last().Equals(","))
            data.Add(new Word(","));
        
        Data = new();
        List<Word> cur = new();
        foreach (var word in data)
        {
            if (word.Equals(","))
            {
                Data.Add(cur);
                cur = new();
            }
            else cur.Add(word);
        }
    }
    public List<Word> Flatten
    {
        get
        {
            var result = new List<Word>();
            foreach (var sublist in Data)
            {
                if (sublist.Count != 1) 
                    throw new ArgumentException($"Required single word argument, but was {sublist}");
                result.Add(sublist[0]);
            }
            return result;
        }
    }
    public static ArgumentsList UntilKeyword(ParserIterator it)
    {
        List<Word> blockWords = new();
        while (it.MoveNext() && !it.CurrentWord.IsKeyword)
            blockWords.Add(it.CurrentWord);
        return new ArgumentsList(blockWords);
    }

    public static ArgumentsList FromBlock(Block block)
    {
        if (block.HasBlocks) throw new Exception("Arguments must not to be blocks");
        List<Word> blockWords = new();
        foreach (var word in block.Children)
        {
            blockWords.Add((Word) word);
        }
        return new ArgumentsList(blockWords);
    }

    public static ArgumentsList UntilEnd(ParserIterator it)
    {
        List<Word> blockWords = new();
        while (it.MoveNext())
            blockWords.Add(it.CurrentWord);
        return new ArgumentsList(blockWords);
    }
}