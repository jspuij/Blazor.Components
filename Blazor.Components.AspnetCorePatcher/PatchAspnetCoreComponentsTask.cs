using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Blazor.Components.AspnetCorePatcher
{
    /// <summary>
    /// MSBuild task that executes the patch for either client side or server side blazor.
    /// </summary>
    public class PatchAspnetCoreComponentsTask : Task
    {
        [Required]
        public string InputFiles { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        public override bool Execute()
        {
            bool isWebassembly = InputFiles.Contains("_bin");

            string aspnetCoreComponentsPath = null;
            string outputPath = null;

            List<string> otherFiles = new List<string>();

            if (isWebassembly)
            {
                foreach (var file in Directory.GetFiles(InputFiles, "*.dll"))
                {
                    if (file.EndsWith("Microsoft.AspNetCore.Components.dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        aspnetCoreComponentsPath = file;
                        outputPath = $"{file}.patched";
                    } else
                    {
                        otherFiles.Add(file);
                    }
                }
            } else
            {
                foreach (var file in References.Select(x => x.ItemSpec))
                {
                    if (file.EndsWith("Microsoft.AspNetCore.Components.dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (file.Contains("dotnet"))
                        {
                            var dotnetpath = file.Substring(0, file.LastIndexOf("dotnet") + 6);

                            var latest = Directory.GetDirectories(Path.Combine(dotnetpath, "shared", "Microsoft.AspNetCore.App")).Last();
                            aspnetCoreComponentsPath = Path.Combine(latest, Path.GetFileName(file));
                        } else
                        {
                            aspnetCoreComponentsPath = file;
                        }

                        Log.LogWarning(aspnetCoreComponentsPath);
                        outputPath = Path.Combine(InputFiles, $"{Path.GetFileName(file)}.patched");
                    }
                    else
                    {
                        if (file.EndsWith("Microsoft.Extensions.DependencyInjection.Abstractions.dll", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log.LogWarning(file);
                        }
                        otherFiles.Add(file);
                    }
                }
            }

            try
            {
                using (var assemblyPatcher = new AssemblyPatcher(aspnetCoreComponentsPath, outputPath, otherFiles.ToArray(), true))
                {
                    assemblyPatcher.Run();
                }
                var targetFileName = outputPath.Replace(".patched", "");
                File.Delete(targetFileName);
                File.Move(outputPath, targetFileName);
                return true;
            }
            catch (Exception ex)
            {
                this.Log.LogError($"There was an error patching Microsoft.AspNetCore.Components.dll. The error is: {ex.Message}");
                this.Log.LogError($"Stack trace: {ex.StackTrace}");
            }
            return false;
        }
    }
}
