using System;
using System.Data;
using System.Reflection;
using System.Threading;

namespace BendroCorpBackgroundAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting BendroCorp task runner...");
            Runner runner = new Runner();
            runner.Run();
        }
    }
}
