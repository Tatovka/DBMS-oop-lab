using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MknImmiSql.Api.V1.Parser;

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
        Children = GroupKeyWords(Children);
    }

    public Block(List<Word> words)
    {
        foreach (var word in words)
        {
            Children.Add(word);
        }
        HasBlocks = false;
    }
    List<IParserNode> GroupKeyWords(List<IParserNode> oldWords)
    {
        List<IParserNode> result = new();

        for (int i = 0; i < oldWords.Count; i++)
        {
            if (!(oldWords[i] is Word))
            {
                result.Add(oldWords[i]);
                continue;
            }

            var curWord = oldWords[i] as Word;
            Keyword kw;
            if (!Keyword.Beginings.TryGetValue(curWord, out kw))
            {
                result.Add(oldWords[i]);
                continue;
            }

            int shift = 0;
            String resultStr = curWord.ToString();
            for (int j = i + 1; j < oldWords.Count; ++j)
            {
                if (oldWords[j] is Word next && kw.PossibleContinues.TryGetValue(next, out kw))
                {
                    resultStr += $" {kw.Name}";
                    shift++;
                    if (kw.IsLeaf) break;
                }
                else
                {
                    shift = 0;
                    break;
                }
            }
            if (shift == 0) result.Add(curWord);
            else result.Add(new Word(resultStr));
            i += shift;
        }
        
        return result;
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

    public int CountArgs => Children.Count(word => word.Equals(","));
}