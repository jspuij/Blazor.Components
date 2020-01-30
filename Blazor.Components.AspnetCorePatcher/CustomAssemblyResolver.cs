using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blazor.Components.AspnetCorePatcher
{
	/// <summary>
	/// Resolves assemblies from a list of files provided.
	/// </summary>
	public class CustomAssemblyResolver : AssemblyResolver
	{
		private readonly string[] paths;

		public CustomAssemblyResolver(string[] references)
		{
			this.paths = references.Select(x => Path.GetDirectoryName(x)).Distinct().ToArray();
		}

		protected override IEnumerable<string> GetModuleSearchPaths(ModuleDef module)
		{
			return paths.Union(base.GetModuleSearchPaths(module));
		}
	}
}
