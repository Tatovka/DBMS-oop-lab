using System.Collections.Generic;
using System.Linq;

namespace MknImmiSql.Api.V1.Parser;

public class Keyword
{
    public readonly Word Name;
    public Dictionary<Word, Keyword> PossibleContinues;
    
    private Keyword(Word name)
    {
        this.Name = name;
        this.PossibleContinues = new();
    }

    private void AddContinue(Keyword kw)
    {
        PossibleContinues.Add(kw.Name, kw);
    }
    public static void AddKeyword(Word word)
    {
        var splited = word.ToString().ToLowerInvariant().Split(' ').Select(str => new Word(str)).ToArray();
        Keyword cur = new Keyword(splited.Last());
        foreach (var el in splited.Reverse().Skip(1))
        {
            Keyword prev = cur;
            cur = new Keyword(el);
            cur.AddContinue(prev);
        }
        if (Beginings.ContainsKey(splited[0]))
            Beginings[splited[0]].AddContinue(cur.PossibleContinues.Values.Last());
        else
            Beginings[splited[0]] = cur;
    }
    


    public static Dictionary<Word, Keyword> Beginings = new();
    
    public bool IsLeaf => PossibleContinues.Count == 0;

}