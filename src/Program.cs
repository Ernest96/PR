using System;
using System.Threading;
using System.Threading.Tasks;

namespace PR
{
    class Program
    {
        static void Main(string[] args)
        {
            do
            {
                Console.Clear();

                Logger.Writeln("\nF/f - Fetch data ", ConsoleColor.White);
                Logger.Writeln("C/c - Load cached data", ConsoleColor.White);
                Logger.Writeln("Esc - Exit\n", ConsoleColor.White);

                var key = Console.ReadKey(true);

                try
                {
                    Report report = new Report();

                    switch(key.Key)
                    {
                        case ConsoleKey.F:
                            report.Fetch();
                            break;
                        case ConsoleKey.C:
                            report.LoadCache();
                            break;
                        case ConsoleKey.Escape:
                            return;
                        default:
                            continue;
                    }

                    report.Print();
                }
                catch (Exception e)
                {
                    Logger.Writeln(e.Message, ConsoleColor.Red);
                }

                Logger.Write("\nPress Enter to run again... ", ConsoleColor.White);
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter)
                {
                    return;
                }
            }
            while (true);
        }

    }
}