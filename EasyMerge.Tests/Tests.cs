using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace EasyMerge
{
	public class Tests
	{
		const string XmlNsURI = "http://schemas.microsoft.com/developer/msbuild/2003";
		static readonly XNamespace XmlNs = XNamespace.Get(XmlNsURI);

		[Fact]
		public void when_checking_is_msbuild_then_succeeds_on_proj_file()
		{
			Assert.True(MsBuildSorter.IsMsBuild("Content\\SampleLibrary\\SampleLibrary.csproj"));
		}

		[Fact]
		public void when_checking_is_msbuild_then_fails_on_sln_file()
		{
			Assert.False(MsBuildSorter.IsMsBuild("Content\\SampleLibrary.sln"));
		}

		[Fact]
		public void when_sorting_csproj_then_does_not_group_conditional_reference()
		{
			var fileName = "Content\\SampleLibrary\\SampleLibrary.csproj";

			var doc = MsBuildSorter.GetSorted(fileName);

			var dataSetExtensions = doc.Descendants(XmlNs + "Reference")
				.Where(r => r.Attribute("Include").Value == "System.Data.DataSetExtensions")
				.First();

			Assert.True(dataSetExtensions.Parent.Attribute("Condition") != null);
			Assert.Equal(1, dataSetExtensions.Parent.Elements().Count());
		}

		[Fact]
		public void when_sorting_csproj_then_groups_references_across_item_groups()
		{
			var fileName = "Content\\SampleLibrary\\SampleLibrary.csproj";

			var doc = MsBuildSorter.GetSorted(fileName);

			var references = doc.Descendants(XmlNs + "Reference")
				.Where(r => r.Parent.Attribute("Condition") == null)
				.First()
				.Parent;

			Assert.Equal(7, references.Elements().Count());
		}

		[Fact]
		public void when_sorting_csproj_then_sorts_by_include_attribute()
		{
			var fileName = "Content\\SampleLibrary\\SampleLibrary.csproj";

			var doc = MsBuildSorter.GetSorted(fileName);

			var compiles = doc.Descendants(XmlNs + "Compile").First().Parent;

			Assert.Equal(5, compiles.Elements().Count());
			Assert.Equal("Class1.cs", compiles.Elements().First().Attribute("Include").Value);
			Assert.Equal("Properties\\AssemblyInfo.cs", compiles.Elements().Last().Attribute("Include").Value);

			Console.WriteLine(doc);
		}

		[Fact]
		public void when_sorting_sln_then_sorts_projects_by_contents_ordinal()
		{
			var fileName = "Content\\SampleLibrary.sln";

			var sorted = SlnSorter.GetSorted(fileName);

			Assert.True(sorted.FindIndex(s => s.Contains("SampleLibrary")) > sorted.FindIndex(s => s.Contains("ClassLibrary")));
		}

		[Fact]
		public void when_sorting_project_configuration_then_sorts_by_contents_ordinal()
		{
			var fileName = "Content\\SampleLibrary.sln";

			var sorted = SlnSorter.GetSorted(fileName);

			Assert.True(sorted.FindIndex(s => s.Contains("5736C923")) < sorted.FindIndex(s => s.Contains("63309442")));
		}

		[Fact]
		public void when_generating_msbuild_from_sln_then_succeeds ()
		{
			File.WriteAllText (@"..\..\Content\SampleLibrary.xml",
				Microsoft.Build.BuildEngine.SolutionWrapperProject.Generate ("Content\\SampleLibrary.sln",
				null, new Microsoft.Build.Framework.BuildEventContext (0, 0, 0, 0)));
		}
	}
}
