using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rso.DotNetUtilities;

public class ServiceFinder
{
    private string _folder;
    public ServiceFinder(string folder)
    {
        _folder = folder;
    }

    public void Restart(List<DotNetInfo.Version> changes)
    {
        List<string> runtimeFiles = new List<String>();
        findRuntimeFiles(_folder, runtimeFiles, changes);
        foreach (var f in runtimeFiles)
        {
            var serviceName = findServiceName(f);
            if (serviceName != null)
            {
                Console.Write($"Restarting service [${serviceName}] ...");
                var exe = new Execute();
                var code = exe.Run("sudo", $"systemctl restart {serviceName}");
                if (code == 0)
                {
                    Console.WriteLine($"Restarted OK");
                }
                else
                {
                    Console.WriteLine($"Failed to restart [{code}] [{exe.StandardError}]");
                }
            }
            else
            {
                Console.WriteLine($"Could not find service for file [{f}]");
            }
        }
    }

    private string? findServiceName(string path)
    {
        var df = dllFile(path);
        var serviceFiles = Directory.GetFiles("/etc/systemd/system", "*.service");
        foreach (var sf in serviceFiles)
        {
            var lines = File.ReadLines(sf);
            foreach (var line in lines)
            {
                // ExecStart=/usr/bin/dotnet /home/rob/ImageViewer/ImageViewer.server.dll --urls="http://*:5020" 
                //
                if (line.StartsWith("ExecStart=") && line.Contains("dotnet") && line.Contains(df))
                {
                    return Path.GetFileName(sf);
                }
            }
        }
        return null;
    }


    private void findRuntimeFiles(string folder, List<string> paths, List<DotNetInfo.Version> changes)
    {
        var files = Directory.GetFiles(folder, "*.runtimeconfig.json");
        foreach (var f in files)
        {
            if (dllFile(f) != null && isUsingDotnet(f, changes))
            {
                paths.Add(f);
            }
        }

        var folders = Directory.GetDirectories(folder);
        foreach (var f in folders)
        {
            // Check if the file's own name is hidden
            string fileName = Path.GetFileName(f);
            if (!fileName.StartsWith("."))
            {
                findRuntimeFiles(f, paths, changes);
            }
        }
    }

    private string? dllFile(string path)
    {
        var fn = path.Replace(".runtimeconfig.json", ".dll");
        return File.Exists(fn) ? fn : null;
    }

    private bool isUsingDotnet(string path, List<DotNetInfo.Version> changes)
    {
        var jsonStr = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<RuntimeConfig>(jsonStr);
        if (config != null && config.runtimeOptions!=null)
        {
            var frameworks = config.runtimeOptions.frameworks;
            if (frameworks != null)
            {
                //  multi frameworks defined
                // see if the changes are in the list of frameworks for this item
                var fw = changes.Where(m => frameworks.Where(n => n.name == m.FrameworkName && n.version.StartsWith(m.Major.ToString())).FirstOrDefault() != null).FirstOrDefault();
                return fw != null ? true : false;
            }
            else
            {
                // single framwork defined
                var framework = config.runtimeOptions.framework;
                if (framework != null)
                {
                    var fw = changes.Where(m => framework.name == m.FrameworkName && framework.version.StartsWith(m.Major.ToString())).FirstOrDefault();
                    return fw != null ? true : false;
                }
                else
                {
                    Console.WriteLine($"Unexpected runtimeOptions.json found [{path}]");
                    return false;
                }
            }
        }
        else
        {
            return false;
        }
    }

    private class RuntimeConfig
    {
        public class Options {
            public List<Framework>? frameworks { get; set; }
            public Framework? framework { get; set; }
            public class Framework
            {
                public string name { get; set; }
                public string version { get; set; }
            }
        }
        public Options runtimeOptions { get; set; }
    }


}