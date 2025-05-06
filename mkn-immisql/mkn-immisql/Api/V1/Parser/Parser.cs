using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;



public class Word : IParserNode
{
    private readonly String _value;
    public static readonly Word Empty = new (String.Empty); 
    private static readonly Regex NameFormat = new (@"^[\w-_]+$");
    
    public bool IsString => (_value.Length > 0) && (_value[0] == '\'');
    
    public Word(String str)
    {
        _value = str;
    }

    public bool Equals(IParserNode other)
    {
        if (other is Word otherWord) 
            return _value.Equals(otherWord._value, StringComparison.InvariantCultureIgnoreCase);
        return false;
    }
    public bool Equals(String str) => str.Equals(_value, StringComparison.InvariantCultureIgnoreCase);
    
    public override string ToString()
    {
        return _value;
    }
    public bool IsName => (_value[0] == '"' && _value.Last() == '"') || NameFormat.IsMatch(_value);
    
}

public class Block : IParserNode
{
    public List<IParserNode> Children = new();

    public readonly bool HasBlocks;
    public Block(List<String> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            if (!words[i].Contains('(') && !words[i].Contains(')') || words[i][0]=='\'' || words[i][0] == '"')
                Children.Add(new Word(words[i]));
            
            else if (words[i].Contains('('))
            {
                Int32 index = words[i].IndexOf('(');
                if (index > 0)
                    Children.Add(new Word(words[i].Substring(0,index)));
                if (index == words[i].Length - 1) words.RemoveAt(i);
                else words[i] = words[i].Substring(index + 1, words[i].Length - index - 1);
                Int32 bracketsOpen = 1;
                Int32 wordIndex = 0;
                List<String> blockWords = new List<string>();
                for (wordIndex = i; wordIndex < words.Count; wordIndex++)
                {
                    for (int k = 0; k < words[wordIndex].Length; k++)
                    {
                        if (words[wordIndex][k]=='(') bracketsOpen++;
                        else if (words[wordIndex][k]==')') bracketsOpen--;
                        if (bracketsOpen == 0)
                        {
                            var oldWord = words[wordIndex];
                            if (k != oldWord.Length - 1)
                                words.Insert(wordIndex + 1, oldWord.Substring(k + 1, oldWord.Length - k - 1));
                            blockWords = words.GetRange(i, wordIndex - i);
                            if (k > 0) blockWords.Add(oldWord.Substring(0, k));
                            break;
                        }
                    }
                    if (bracketsOpen == 0) break;
                }
                if (bracketsOpen != 0) throw new Exception();
                Children.Add(new Block(blockWords));
                HasBlocks = true;
                i = wordIndex;
            }
        }
        if (Children.Last().Equals(";")) Children.RemoveAt(Children.Count - 1);
    }
    public override String ToString()
    {
        StringBuilder result = new ();
        result.AppendLine("Block start");
        foreach (var child in Children)
        {
            result.Append($"{child.ToString()} ");
        }
        result.AppendLine("Block end");
        return result.ToString();
    }
    public bool Equals(IParserNode other)
    {
        return other is Block;
    }
}

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
        for (int i = 0; i < names.Count; i++)
        {
            words.AddRange(FindCommas(woNames[i]));
            words.Add(names[i]);
        }
        words.AddRange(FindCommas(woNames.Last()));
        return words;
    }

    private static List<String> FindCommas(String subStr)
    {
        var woCommas = subStr.Split(',');
        var words = new List<String>();
        for (int i = 0; i < woCommas.Length - 1; i++)
        {
            words.AddRange(woCommas[i].Split(' ', StringSplitOptions.RemoveEmptyEntries));
            words.Add(",");
        }
        words.AddRange(woCommas.Last().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return words;
    }
    
    public static ICommand GetCommand(Block block)
    {
        var nodes = block.Children;
        if (nodes.Count() < 2 ) throw new Exception("Cannot found command");
        
        if (nodes[1].Equals("Table"))
        {
            if (nodes[0].Equals("Create"))
                return new CreateTableCommand(nodes.Skip(2).ToList());
            
            if (nodes[0].Equals(new Word("Drop")))
                return new DropTableCommand(nodes.Skip(2).ToList());
        }
        throw new Exception($"Unknown command {nodes[0]} {nodes[1]}");
    }
}