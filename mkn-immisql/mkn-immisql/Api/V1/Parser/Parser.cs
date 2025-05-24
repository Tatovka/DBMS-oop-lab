using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;

public static class Parser
{
    public static Block Parse(String query)
    { 
        Console.WriteLine(query);
        query = query.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        Regex findStrings = new Regex("'[^']*'", RegexOptions.CultureInvariant);
        List<String> strings = new List<String>();
        foreach (Match match in findStrings.Matches(query))
        {
            strings.Add(match.Value);
        }
        var woStrings = findStrings.Split(query).ToList();
        var words = new List<String>();
        for (int i = 0; i < strings.Count; i++)
        {
            words.AddRange(FindNames(woStrings[i]));
            words.Add(strings[i]);
        }
        words.AddRange(FindNames(woStrings.Last()));
        
        return new Block(words);
    }

    private static List<String> FindNames(String subStr)
    {
        Regex findNames = new Regex("\"[^\"]*\"", RegexOptions.CultureInvariant);
        var names = findNames.Matches(subStr).Select(m => m.Value).ToList();
        var woNames = findNames.Split(subStr).ToList();
        var words = new List<String>();
        var syms = new[] { ',', ';' };
        for (int i = 0; i < names.Count; i++)
        {
            words.AddRange(FindAll(woNames[i], syms));
            words.Add(names[i]);
        }
        words.AddRange(FindAll(woNames.Last(), syms));
        return words;
    }

    private static List<String> FindAll(String str, char[] delimiters)
    {
        var cur = FindCommas(str, delimiters[0]);
        foreach (var sym in  delimiters.Skip(1))
        {
            var cur2 = new List<String>();
            for (int j = 0; j < cur.Count; j++)
            {
                cur2.AddRange(FindCommas(cur[j], sym));
            }
            cur = cur2;
        }
        return cur;
    }
    private static List<String> FindCommas(String subStr, char sym)
    {
        var woCommas = subStr.Split(sym);
        var words = new List<String>();
        for (int i = 0; i < woCommas.Length - 1; i++)
        {
            words.AddRange(woCommas[i].Split(' ', StringSplitOptions.RemoveEmptyEntries));
            words.Add($"{sym}");
        }
        words.AddRange(woCommas.Last().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return words;
    }
    
    public static ICommand GetCommand(Block block)
    {
        var nodes = block.Children;
        if (nodes.Count() < 2 ) throw new Exception("Cannot found command");
        if (!nodes[0].AsWord.IsCommandName)
        {
            if (nodes[1].Equals("Left Join") || nodes[1].Equals("Right Join") 
                || nodes[1].Equals("Inner Join") || nodes[1].Equals("Join"))
                return new JoinCommand(nodes.ToList());
        }
        if (nodes[0].Equals("CREATE TABLE"))
            return new CreateTableCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("DROP TABLE"))
            return new DropTableCommand(nodes.Skip(1).ToList());

        if (nodes[0].Equals("INSERT INTO"))
            return new InsertCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("Select"))
            return new SelectCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("Delete From"))
            return new DeleteCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("Update"))
            return new UpdateCommand(nodes.Skip(1).ToList());
        throw new Exception($"Unknown command {nodes[0]} {nodes[1]}");
    }
}