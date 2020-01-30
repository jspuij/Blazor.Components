using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Blazor.Components.AspnetCorePatcher
{
    public class AssemblyPatcher : IDisposable
    {
        private readonly string inputFileName;
        private readonly string outputFileName;
        private readonly string[] references;
        private readonly bool useCustomResolver;
        private ModuleDefMD module;

        public AssemblyPatcher(string inputFileName, string outputFileName, string[] references, bool useCustomResolver)
        {
            this.inputFileName = inputFileName;
            this.outputFileName = outputFileName;
            this.references = references;
            this.useCustomResolver = useCustomResolver;
        }

        public void Dispose()
        {
            if (module != null)
            {
                module.Dispose();
                module = null;
            }
        }

        public void Run()
        {
            // Create a default assembly resolver and type resolver and pass it to Load().
            // If it's a .NET Core assembly, you'll need to disable GAC loading and add
            // .NET Core reference assembly search paths.
            ModuleContext modCtx = ModuleDef.CreateModuleContext();

            IAssemblyResolver resolver = new CustomAssemblyResolver(references);
            modCtx.AssemblyResolver = resolver;

            module = ModuleDefMD.Load(inputFileName, modCtx);
            Importer importer = new Importer(module);

            var instantiateComponentMethod = (from t in module.GetTypes()
                                              where t.Name == "ComponentFactory"
                                              from m in t.Methods
                                              where m.Name == "InstantiateComponent"
                                              select m).Single();

            var assembly = resolver.Resolve(module.GetAssemblyRefs().FirstOrDefault(x => x.Name == "Microsoft.Extensions.DependencyInjection.Abstractions"), module);

            var getServiceMethod = (from t in assembly.ManifestModule.GetTypes()
                                    where t.Name == "ActivatorUtilities"
                                    from m in t.Methods
                                    where m.Name == "GetServiceOrCreateInstance"
                                    && !m.HasGenericParameters
                                    select m).Single();

            var firstCall = instantiateComponentMethod.Body.Instructions.First(x => x.OpCode == OpCodes.Call);

            instantiateComponentMethod.Body.Instructions.Insert(0, OpCodes.Ldarg_1.ToInstruction());
            firstCall.Operand = importer.Import(getServiceMethod);

           

            if (module.IsILOnly)
            {
                // Create writer options
                var opts = new ModuleWriterOptions(module);

                // Open or create the strong name key
                var signatureKey = new StrongNameKey(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Key.snk"));

                // This method will initialize the required properties
                opts.InitializeStrongNameSigning(module, signatureKey);

                module.Write(outputFileName, opts);
            } else
            {
                // Create writer options
                var opts = new NativeModuleWriterOptions(module, false);

                // Open or create the strong name key
                var signatureKey = new StrongNameKey(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Key.snk"));

                // This method will initialize the required properties
                opts.InitializeStrongNameSigning(module, signatureKey);

                module.NativeWrite(outputFileName, opts);
            }
        }
    }
}
