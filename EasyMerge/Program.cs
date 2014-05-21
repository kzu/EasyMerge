namespace EasyMerge
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	class Program
	{
		const string FilePattern = "*.sln;*.csproj";

		static void Main(string[] args)
		{
			if (args != null && args.Length == 1 && (args[0] == "?" || args[0] == "-?"))
			{
				Console.WriteLine("Usage: {0} -recursive", Path.GetFileName(typeof(Program).Assembly.ManifestModule.FullyQualifiedName));
				Console.WriteLine("If -recursive is specified, the application will try to find files matching {0} in the current directory and all subdirectories.");
				Console.ReadLine();
				return;
			}

			var searchOption = args != null && args.Length >= 1 && args[0] == "-recursive" ?
				SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			IEnumerable<string> files = FilePattern.Split(';')
				.SelectMany(searchPattern => Directory.EnumerateFiles(Directory.GetCurrentDirectory(), searchPattern, searchOption));

			foreach (var file in files)
			{
				Console.WriteLine("Preparing {0}", file);
				if (MsBuildSorter.IsMsBuild(file))
					MsBuildSorter.Sort(file);
				else
					SlnSorter.Sort(file);

			}

			Console.WriteLine("Done!");
		}
	}
}
