using CommandLine;
using Rso.DotNetUtilities;
using System;

class Program
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Value(0,Required=false,HelpText="Folder to search for services", Default = "")]
        public string Folder { get; set; }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options => RunOptionsAndReturnExitCode(options))
            .WithNotParsed(errors => HandleParseError(errors));
    }

    static int RunOptionsAndReturnExitCode(Options options)
    {
        var dni = new DotNetInfo();
        dni.ReadCurrent();
        foreach (var rt in dni.Runtimes)
        {
            Console.WriteLine($"NETCore = {rt}");
        }
        //
        var prevDni = DotNetInfo.GetPrevious();
        // Not run before so save current and exit
        if (prevDni == null)
        {
            DotNetInfo.Save(dni);
            Console.WriteLine("No previous .NET info available");
            return 0;
        }
        else
        {
            var changes = dni.Changes(prevDni);
            if (changes.Count > 0)
            {
                Console.WriteLine(".NET changes detected. Restarting services...");
                //?? DotNetInfo.Save(dni);
                restartServices(options, changes);
                return 0;
            }
            else
            {
                Console.WriteLine("No .NET changes detected.");
                return 0;
            }
        }
    }

    private static void restartServices(Options options, List<DotNetInfo.Version> changes)
    {
        var folder = options.Folder;
        if (string.IsNullOrEmpty(folder))
        {
            folder = Directory.GetCurrentDirectory();
        }
        var serviceFinder = new ServiceFinder(folder);
        serviceFinder.Restart(changes);
        
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
        Console.Error.WriteLine("Error parsing arguments.");
        // You can inspect the errors to provide more specific feedback
    }
}
