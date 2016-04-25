using System;
using System.Collections.Generic;


namespace Util
{
    public class ParameterParser
    {
        public class Parameter
        {
            public string name;
            public string description;
            public string defaultValue;
            public string value;


            public bool HasValue()
            {
                return this.value != null || this.defaultValue != null;
            }


            public string GetString()
            {
                if (this.value == null)
                    return this.defaultValue;

                return this.value;
            }


            public int GetInt()
            {
                var value = this.defaultValue;
                if (this.value != null)
                    value = this.value;

                return ParseInt(value);
            }


            public bool GetBool()
            {
                var value = this.defaultValue;
                if (this.value != null)
                    value = this.value;

                return value == "on" || value == "true";
            }


            private int ParseInt(string value)
            {
                if (value.StartsWith("$"))
                    return value[1];

                if (value.StartsWith("0x"))
                    return Convert.ToInt32(value.Substring(2), 16);

                if (value.StartsWith("0o"))
                    return Convert.ToInt32(value.Substring(2), 8);

                if (value.StartsWith("0b"))
                    return Convert.ToInt32(value.Substring(2), 2);

                return int.Parse(value);
            }


            public List<int> GetIntList()
            {
                var value = this.defaultValue;
                if (this.value != null)
                    value = this.value;

                var list = new List<int>();

                var ranges = value.Split(',');
                foreach (var range in ranges)
                {
                    var limits = range.Split('-');

                    if (limits.Length == 1)
                    {
                        list.Add(ParseInt(limits[0]));
                    }
                    else
                    {
                        for (var i = ParseInt(limits[0]); i <= ParseInt(limits[1]); i++)
                            list.Add(i);
                    }
                }

                return list;
            }
        }


        private List<Parameter> parameters = new List<Parameter>();
        private List<string> unnamedArgs = new List<string>();


        public Parameter Add(string name, string defaultValue, string description)
        {
            var param = new Parameter();
            param.name = name;
            param.description = description;
            param.defaultValue = defaultValue;
            param.value = null;
            this.parameters.Add(param);
            return param;
        }


        public List<string> GetUnnamed()
        {
            return this.unnamedArgs;
        }


        public void PrintHelp(string indentation)
        {
            var longestNameLength = 0;
            foreach (var param in this.parameters)
            {
                if (param.name.Length > longestNameLength)
                    longestNameLength = param.name.Length;
            }

            foreach (var param in this.parameters)
            {
                Console.Out.Write(indentation + "--" + (param.name + ":").PadRight(longestNameLength + 2) + param.description);
                if (param.defaultValue != null)
                    Console.Out.Write(" (Default: " + (param.defaultValue == "" ? "<empty>" : param.defaultValue) + ")");
                Console.Out.WriteLine();
            }
        }


        public bool Parse(string[] commandLineArgs)
        {
            foreach (var arg in commandLineArgs)
            {
                if (!arg.StartsWith("--"))
                {
                    var finalArg = arg;
                    if (finalArg.StartsWith("\""))
                        finalArg = finalArg.Substring(1, finalArg.Length - 2);

                    unnamedArgs.Add(finalArg);
                }
                else
                {
                    foreach (var param in this.parameters)
                    {
                        if (arg.StartsWith("--" + param.name + "="))
                        {
                            var finalArg = arg.Substring(param.name.Length + 3);
                            if (finalArg.StartsWith("\""))
                                finalArg = finalArg.Substring(1, finalArg.Length - 2);

                            param.value = finalArg;
                            goto next;
                        }
                    }

                    return false;

                next:
                    continue;
                }
            }

            return true;
        }
    }
}
