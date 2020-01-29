using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blazor.Components.AspnetCorePatcher
{
    public class AssemblyPatcher : IDisposable
    {
        private readonly string inputFileName;
        private readonly string outputFileName;
        private readonly string[] references;
        private readonly bool useCustomResolver;
        private ModuleDefinition module = null;

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
            IAssemblyResolver resolver = useCustomResolver ? (IAssemblyResolver) new CustomAssemblyResolver(references) : new DefaultAssemblyResolver();
            module = ModuleDefinition.ReadModule(inputFileName, new ReaderParameters()
            {
                AssemblyResolver = resolver,
                ReadWrite = false,
            });

            var instantiateComponentMethod = (from t in module.GetTypes()
                                              where t.Name == "ComponentFactory"
                                              from m in t.Methods
                                              where m.Name == "InstantiateComponent"
                                              select m).Single();
            var assembly = module.AssemblyResolver.Resolve(module.AssemblyReferences.FirstOrDefault(x => x.Name == "Microsoft.Extensions.DependencyInjection.Abstractions"));


            var getServiceMethod = (from t in assembly.MainModule.GetTypes()
                                    where t.Name == "ActivatorUtilities"
                                    from m in t.Methods
                                    where m.Name == "GetServiceOrCreateInstance"
                                    && !m.HasGenericParameters
                                    select m).Single();

            var firstCall = instantiateComponentMethod.Body.Instructions.First(x => x.OpCode == OpCodes.Call);

            var processor = instantiateComponentMethod.Body.GetILProcessor();

            processor.InsertBefore(instantiateComponentMethod.Body.Instructions.First(), processor.Create(OpCodes.Ldarg_1));
            firstCall.Operand = module.ImportReference(getServiceMethod);

            module.Write(outputFileName);
        }
    }
}
