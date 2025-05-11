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
        query = query.Replace('\n', ' ').Replace('\r', ' ');
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
        
        if (nodes[0].Equals("CREATE TABLE"))
            return new CreateTableCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("DROP TABLE"))
            return new DropTableCommand(nodes.Skip(1).ToList());

        if (nodes[0].Equals("INSERT INTO"))
            return new InsertCommand(nodes.Skip(1).ToList());
        
        if (nodes[0].Equals("Select"))
            return new SelectCommand(nodes.Skip(1).ToList());
        
        throw new Exception($"Unknown command {nodes[0]} {nodes[1]}");
    }

    public static List<Word> GetArgList(IParserNode node, out Int32 count)
    {
        if (node is Block argsBlock)
        {
            if (argsBlock.HasBlocks) throw new Exception("Arguments list should not contain blocks");
            var colArgs = argsBlock.Children;
            count = argsBlock.CountArgs;
            if (colArgs.Count == 0 || !colArgs.Last().Equals(","))
            {
                colArgs.Add(new Word(","));
                count++;
            }
            var result = new List<Word>();
            foreach (var arg in colArgs)
            {
                result.Add((arg as Word)!);
            }
            return result;
        }
        throw new Exception("Invalid argument: expected Block as arguments list");
    }

    public static List<List<Word>> SplitArgList(List<Word> list)
    {
        List<List<Word>> result = new();
        List<Word> cur = new();
        foreach (var word in list)
        {
            if (word.Equals(","))
            {
                result.Add(cur);
                cur = new();
            }
            else cur.Add(word);
        }
        return result;
    }

    public static List<Word> FlatArgList(List<List<Word>> list)
    {
        var result = new List<Word>();
        foreach (var subl in list)
        {
            if (subl.Count != 1) throw new ArgumentException($"Required single word argument, but was {subl}");
            result.Add(subl[0]);
        }
        return result;
    }
}