using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace SpritePacker
{
    public static class ExportGroups
    {
        public static Dictionary<string, List<string>> Parse(string src)
        {
            var dict = new Dictionary<string, List<string>>();
            var parser = new Parser { src = src };

            while (!parser.IsOver)
            {
                parser.SkipWhitespace();
                var entry = parser.ReadString();
                parser.SkipWhitespace();

                var matches = new List<string>();
                while (!parser.IsOver && parser.Current == '-')
                {
                    parser.Advance();
                    parser.SkipWhitespace();
                    matches.Add(parser.ReadString());
                    parser.SkipWhitespace();
                }

                dict.Add(entry, matches);
            }

            return dict;
        }


        public static bool TestPattern(string pattern, string str)
        {
             var regex = new Regex(
                 "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$");

            return regex.IsMatch(str);
        }


        private class Parser
        {
            public string src;
            public int index = 0;


            public char Current { get { return src[index]; } }
            public bool IsOver { get { return index >= src.Length; } }


            public void Advance()
            {
                index++;
            }



            public void SkipWhitespace()
            {
                while (!IsOver &&
                    (Current == ' ' || Current == '\t' || Current == '\n' || Current == '\r'))
                    Advance();
            }


            public string ReadString()
            {
                var result = "";

                if (!IsOver && Current == '\"')
                {
                    Advance();

                    while (!IsOver && Current != '\"')
                    {
                        result += Current;
                        Advance();
                    }

                    if (result == "")
                        throw new System.Exception("Group file syntax error");

                    if (IsOver || Current != '\"')
                        throw new System.Exception("Group file syntax error");

                    Advance();

                    return result;
                }
                else
                {
                    while (!IsOver &&
                        ((Current >= 'A' && Current <= 'Z') ||
                        (Current >= 'a' && Current <= 'z') ||
                        (Current >= '0' && Current <= '9') ||
                        Current == '/' || Current == '.' ||
                        Current == '-' || Current == '_' ||
                        Current == '*' || Current == '?'))
                    {
                        result += Current;
                        Advance();
                    }

                    if (result == "")
                        throw new System.Exception("Group file syntax error");

                    return result;
                }
            }
        }
    }
}