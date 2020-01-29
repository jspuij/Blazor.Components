using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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
            foreach (var reference in References)
            {
                this.Log.LogWarning($"{reference.ItemSpec}");
            }

            string aspnetCoreComponentsPath = null;
            string outputPath = null;
            List<string> otherFiles = new List<string>();

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

            try
            {
                using (var assemblyPatcher = new AssemblyPatcher(aspnetCoreComponentsPath, outputPath, otherFiles.ToArray(), aspnetCoreComponentsPath.Contains("_bin")))
                {
                    assemblyPatcher.Run();
                }
                File.Delete(aspnetCoreComponentsPath);
                File.Move(outputPath, aspnetCoreComponentsPath);
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
