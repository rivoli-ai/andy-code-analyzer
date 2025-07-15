using System;
using System.Threading.Tasks;

namespace Examples
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify which example to run:");
                Console.WriteLine("  dotnet run CSharpAnalysisExample");
                Console.WriteLine("  dotnet run PythonAnalysisExample");
                Console.WriteLine("  dotnet run EventsExample");
                return;
            }

            var exampleName = args[0];
            
            switch (exampleName)
            {
                case "CSharpAnalysisExample":
                    await CSharpAnalysisExample.Main(args[1..]);
                    break;
                    
                case "PythonAnalysisExample":
                    await PythonAnalysisExample.Main(args[1..]);
                    break;
                    
                case "EventsExample":
                    await EventsExample.Main(args[1..]);
                    break;
                    
                default:
                    Console.WriteLine($"Unknown example: {exampleName}");
                    Console.WriteLine("Available examples:");
                    Console.WriteLine("  CSharpAnalysisExample");
                    Console.WriteLine("  PythonAnalysisExample");
                    Console.WriteLine("  EventsExample");
                    break;
            }
        }
    }
}