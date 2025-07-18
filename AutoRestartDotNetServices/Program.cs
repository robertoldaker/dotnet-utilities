using CommandLine;
using Rso.DotNetUtilities;
using System;
using System.Reflection;

class Program
{
    public class Options
    {
        [Option('v', "version", Required = false, HelpText = "Returns version and exits")]
        public bool Version { get; set; }

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
        if (options.Version)
        {
            var version = getVersion();
            Console.WriteLine($"Version={version}");
            return 0;
        }
        var dni = new DotNetInfo();
        dni.ReadCurrent();
        foreach (var rt in dni.Runtimes)
        {
            Console.WriteLine($"{rt}");
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
                DotNetInfo.Save(dni);
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

    private static string getVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Get the AssemblyInformationalVersionAttribute
        // This usually holds the value of the <Version> or <VersionPrefix> with <VersionSuffix> from .csproj
        string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return informationalVersion;

    }

    private static void restartServices(Options options, List<DotNetInfo.Version> changes)
    {
        //
        // Note writing to STDERROR so that when running as a cron job we can get an email using the MAILTO mechanism
        //
        Console.Error.WriteLine("These .NET changes detected:-");
        foreach (var chg in changes)
        {
            Console.Error.WriteLine($"     {chg}");
        }
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
