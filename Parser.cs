namespace Util
{
    public class Parser
    {
        private string src;
        private int index = 0;
        private int line = 1;


        public Parser(string src)
        {
            this.src = src;
            this.index = 0;
            this.line = 1;
        }


        public bool IsOver()
        {
            return index >= src.Length;
        }


        private void AdvanceIndex()
        {
            if (Current() == '\n')
                line++;

            index++;
        }


        public void Advance()
        {
            AdvanceIndex();
        }


        public int Line()
        {
            return line;
        }


        public char Current()
        {
            if (IsOver())
                return '\0';

            return src[index];
        }


        public char Next()
        {
            if (index + 1 >= src.Length)
                return '\0';

            return src[index + 1];
        }


        public void SkipWhitespace()
        {
            while (!IsOver())
            {
                if (src[index] == ' ' || src[index] == '\t' || src[index] == '\r' || src[index] == '\n')
                    Advance();
                else if (Current() == '/' && Next() == '/')
                {
                    AdvanceIndex();
                    AdvanceIndex();
                    while (!IsOver() && Current() != '\n')
                        AdvanceIndex();
                }
                else if (Current() == '/' && Next() == '*')
                {
                    AdvanceIndex();
                    AdvanceIndex();
                    while (!IsOver() && !(Current() == '*' && Next() == '/'))
                        AdvanceIndex();
                    AdvanceIndex();
                    AdvanceIndex();
                }
                else
                    break;
            }
        }


        public bool TryMatch(char c)
        {
            if (Current() != c)
                return false;

            Advance();
            return true;
        }


        public void Match(char c)
        {
            if (Current() != c)
                RaiseError("expected '" + c + "'");

            Advance();
        }


        public void Match(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (Current() != str[i])
                    RaiseError("expected '" + str + "'");

                Advance();
            }
        }


        public bool CurrentIsIdentifier()
        {
            return (!IsOver() &&
                    ((Current() >= 'A' && Current() <= 'Z') ||
                    (Current() >= 'a' && Current() <= 'z') ||
                    (Current() == '_')));
        }


        public bool CurrentIsNumber()
        {
            return (!IsOver() &&
                    ((Current() >= '0' && Current() <= '9') || Current() == '-'));
        }


        public bool CurrentIsIdentifier(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (index + i >= src.Length)
                    return false;

                if (src[index + i] != str[i])
                    return false;
            }

            return true;
        }


        public string ReadIdentifier()
        {
            string result = "";

            while (!IsOver() &&
                    ((Current() >= 'A' && Current() <= 'Z') ||
                    (Current() >= 'a' && Current() <= 'z') ||
                    (Current() >= '0' && Current() <= '9') ||
                    (Current() == '_')))
            {
                result += Current();
                Advance();
            }

            if (result.Length == 0)
                RaiseError("expected string");

            return result;
        }


        public string ReadStringLiteral()
        {
            string result = "";

            Match('"');

            while (!IsOver() && Current() != '"')
            {
                result += Current();
                Advance();
            }

            Match('"');

            return result;
        }


        public string ReadMultiStringLiteral()
        {
            var result = ReadStringLiteral();
            SkipWhitespace();
            while (Current() == '"')
            {
                result += ReadStringLiteral();
                SkipWhitespace();
            }
            return result;
        }


        public double ReadNumber()
        {
            double result = 0.0;
            double sign = 1;

            if (TryMatch('-'))
                sign = -1;

            while (!IsOver() &&
                    ((Current() >= '0' && Current() <= '9')))
            {
                result = result * 10.0 + (Current() - '0');
                Advance();
            }

            if (Current() == '.')
            {
                Advance();
                double frac = 0.0;
                double divider = 1.0;

                while (!IsOver() &&
                        ((Current() >= '0' && Current() <= '9')))
                {
                    frac = frac * 10.0 + (Current() - '0');
                    divider *= 10.0;
                    Advance();
                }

                result += frac / divider;
            }

            return result * sign;
        }


        public void RaiseError(string msg)
        {
            throw new System.Exception(
                "parser error at line " +
                line + ", " +
                msg + " (next chars: \"" +
                src.Substring(index, System.Math.Min(20, src.Length - index)) + "\")");
        }
    }
}