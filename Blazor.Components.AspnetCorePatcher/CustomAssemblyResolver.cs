using Mono.Cecil;
using System;
using System.IO;

namespace Blazor.Components.AspnetCorePatcher
{
	/// <summary>
	/// Resolves assemblies from a list of files provided.
	/// </summary>
	public class CustomAssemblyResolver : IAssemblyResolver
	{
		private readonly string[] references;

		public CustomAssemblyResolver(string[] references)
		{
			this.references = references;
		}

		public void Dispose()
		{
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return Resolve(name, new ReaderParameters());
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			parameters.AssemblyResolver = this;

			foreach (var reference in references)
			{
				var fileInfo = new FileInfo(reference);

				if (!fileInfo.Exists)
				{
					throw new InvalidOperationException($"File {reference} does not exist on disk.");
				}

				if (Path.GetFileNameWithoutExtension(fileInfo.FullName) == name.Name)
				{
					return ModuleDefinition.ReadModule(reference, parameters).Assembly;
				}
			}

			throw new InvalidOperationException($"Assenbly {name} not in collection of referenced assemblies.");
		}
	}
}
