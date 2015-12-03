using System;
using System.Collections.Generic;


namespace Util
{
    public class CommandParser
    {
        private class Command
        {
            public string name;
            public string description;
            public string def;
        }


        private List<Command> commands = new List<Command>();


        public void AddCommand(string name, string description, string def)
        {
            var comm = new Command();
            comm.name = name;
            comm.description = description;
            comm.def = def;
            this.commands.Add(comm);
        }


        public void PrintHelp()
        {
            var longestNameLength = 0;
            foreach (var comm in this.commands)
            {
                if (comm.name.Length > longestNameLength)
                    longestNameLength = comm.name.Length;
            }

            foreach (var comm in this.commands)
            {
                Console.Out.Write("  -" + (comm.name + ":").PadRight(longestNameLength + 2) + comm.description);
                if (comm.def != null)
                    Console.Out.Write(" (Default: " + (comm.def == "" ? "<empty>" : comm.def) + ")");
                Console.Out.WriteLine();
            }
        }


        public bool ParseCommands(string[] commandLineArgs, Dictionary<string, string> dict)
        {
            foreach (var arg in commandLineArgs)
            {
                foreach (var comm in this.commands)
                {
                    if (arg.StartsWith("-" + comm.name + "=") ||
                        arg.StartsWith("-" + comm.name + ":"))
                    {
                        string a = arg.Substring(comm.name.Length + 2, arg.Length - comm.name.Length - 2);
                        if (a.StartsWith("\""))
                            a = a.Substring(1, a.Length - 2);

                        dict.Add(comm.name, a);
                        goto next;
                    }
                }

                if (!dict.ContainsKey(""))
                {
                    string a = arg;
                    if (a.StartsWith("\""))
                        a = a.Substring(1, a.Length - 2);

                    dict.Add("", a);
                }

            next:
                continue;
            }


            foreach (var comm in this.commands)
            {
                if (!dict.ContainsKey(comm.name))
                {
                    if (comm.def == null)
                        return false;
                    else
                        dict.Add(comm.name, comm.def);
                }
            }

            return true;
        }
    }
}
