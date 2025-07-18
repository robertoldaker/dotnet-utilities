using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rso.DotNetUtilities;

public class DotNetInfo
{
    public DotNetInfo()
    {
        Runtimes = new List<Version>();
    }

    public void ReadCurrent()
    {
        var regex = new Regex(@"\s+(Microsoft\.[\w\.]+)\s(\d+)\.(\d+)\.(\d+)");
        var execute = new Execute();
        execute.Run("dotnet", "--info");
        var lines = execute.StandardOutput.Split('\n');
        Runtimes = new List<Version>();
        foreach (var line in lines)
        {
            if (regex.IsMatch(line))
            {
                var frameworkName = regex.Match(line).Groups[1].Value;
                var major = int.Parse(regex.Match(line).Groups[2].Value);
                var minor = int.Parse(regex.Match(line).Groups[3].Value);
                var patch = int.Parse(regex.Match(line).Groups[4].Value);
                Runtimes.Add(new Version(frameworkName, major, minor, patch));
            }
        }
    }

    public class Version
    {
        public Version()
        {
            FrameworkName = "";
        }
        public Version(string fn, int major, int minor, int patch)
        {
            FrameworkName = fn;
            Major = major;
            Minor = minor;
            Patch = patch;
        }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string FrameworkName { get; set; }

        public bool IsNewVersion(Version v)
        {
            return FrameworkName == v.FrameworkName && Major == v.Major && (v.Patch > Patch || v.Minor > Minor);
        }

        public override string ToString()
        {
            return $"{FrameworkName} {Major}.{Minor}.{Patch}";
        }

    }

    public List<Version> Runtimes { get; set; }

    public List<Version> Changes(DotNetInfo dni)
    {
        var diffs = Runtimes.Where(m => dni.Runtimes.Where( n=>n.IsNewVersion(m)).FirstOrDefault()!=null).ToList();
        return diffs;
    }

    private const string JSON_FILE = "DotNetInfo.json";
    public static DotNetInfo? GetPrevious()
    {
        if (Path.Exists(JSON_FILE))
        {
            string jsonStr = File.ReadAllText(JSON_FILE);
            var dni = JsonSerializer.Deserialize<DotNetInfo>(jsonStr);
            return dni;
        }
        else
        {
            return null;
        }
    }

    public static void Save(DotNetInfo dni)
    {
        string jsonStr = JsonSerializer.Serialize(dni, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText(JSON_FILE, jsonStr);
    }


}