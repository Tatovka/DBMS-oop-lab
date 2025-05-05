using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MknImmiSql.Api.V1.Parser;

public abstract class ParserNode
{
   public abstract void Print();
   public abstract bool Equals(ParserNode other);

   public virtual bool Equals(String str)
   {
       return false;
   }
   
}

public class Word : ParserNode
{
    public readonly String value;
    public static readonly Word Empty = new Word(""); 
    
    private static readonly Regex nameFormat = new Regex(@"^[\w-_]+$");
    
    public bool IsString
    {
        get => (value.Length > 0) && (value[0] == '\'');
    }
    public override void Print()
    {
        Console.WriteLine(value);
    }
    public Word(String str)
    {
        value = str;
    }

    public override bool Equals(ParserNode other)
    {
        if (other is Word) 
            return value.Equals((other as Word).value, StringComparison.CurrentCultureIgnoreCase);
        return false;
    }
    public override bool Equals(String str) => str.Equals(value, StringComparison.CurrentCultureIgnoreCase);
    
    public override string ToString()
    {
        return value;
    }
    
    
    public bool GetBoolean(out bool res) => bool.TryParse(value, out res);
    public bool GetInteger(out Int64 res) => Int64.TryParse(value, out res);
    public bool GetFloat(out double res) => double.TryParse(value, out res);
    public bool GetString(out string res)
    {
        res = value.Substring(1, value.Length - 1);
        return IsString;
    }
    
    public bool IsName => (value[0] == '"' && value.Last() == '"') || nameFormat.IsMatch(value);
    
}

public class Block : ParserNode
{
    public List<ParserNode> children = new();

    public readonly bool HasBlocks;
    public Block(List<String> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            if (!words[i].Contains('(') && !words[i].Contains(')') || words[i][0]=='\'' || words[i][0] == '"')
                children.Add(new Word(words[i]));
            
            else if (words[i].Contains('('))
            {
                Int32 index = words[i].IndexOf('(');
                if (index > 0)
                    children.Add(new Word(words[i].Substring(0,index)));
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
                children.Add(new Block(blockWords));
                HasBlocks = true;
                i = wordIndex;
            }
        }
        if (children.Last().Equals(";")) children.RemoveAt(children.Count - 1);
    }
    public override void Print()
    {
        Console.WriteLine("Block start");
        foreach (var child in children)
        {
            child.Print();
        }
        Console.WriteLine("Block end");
    }

    public override bool Equals(ParserNode other)
    {
        return other is Block;
    }
    
}

public class Parser
{
    public static readonly String[] Typenames = { "BOOLEAN", "INTEGER", "FLOAT", "STRING", "SERIAL"};
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

    public static List<String> FindNames(String subStr)
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

    public static List<String> FindCommas(String subStr)
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
        var nodes = block.children;
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