using System;
using System.Collections.Generic;
using System.Text;

namespace PR
{
    class Logger
    {
        public static void Writeln(string output, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(output);
        }

        public static void Write(string output, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(output);
        }
    }
}
