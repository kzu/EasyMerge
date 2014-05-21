namespace EasyMerge
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Xml;
	using System.Xml.Linq;

	public class MsBuildSorter
	{
		const string XmlNsURI = "http://schemas.microsoft.com/developer/msbuild/2003";
		static readonly XNamespace XmlNs = XNamespace.Get(XmlNsURI);

		string fileName;

		public static bool IsMsBuild(string fileName)
		{
			try
			{
				using (var reader = XmlReader.Create(fileName))
				{
					while (reader.Read() && reader.NodeType != XmlNodeType.Element)
					{ }

					return !reader.EOF &&
						reader.NamespaceURI == XmlNsURI &&
						reader.LocalName == "Project";
				}
			}
			catch (XmlException)
			{
				return false;
			}
		}

		public static void Sort(string fileName)
		{
			GetSorted(fileName).Save(fileName);
		}

		public static XDocument GetSorted(string fileName)
		{
			return new MsBuildSorter(fileName).Sort();
		}

		private MsBuildSorter(string fileName)
		{
			this.fileName = fileName;
		}

		public XDocument Sort()
		{
			var xdoc = XDocument.Load(fileName);

			SortPropertyGroups(xdoc);
			SortItemGroupsByItemType(xdoc);
			SortItemsByInclude(xdoc);

			return xdoc;
		}

		private void SortPropertyGroups(XDocument xdoc)
		{
			var groups = xdoc.Root.Elements(XmlNs + "PropertyGroup");
			foreach (var group in groups)
			{
				var sorted = group.Elements().OrderBy(e => e.Name.LocalName).ToList();
				group.ReplaceNodes(sorted);
			}
		}

		private void SortItemGroupsByItemType(XDocument xdoc)
		{
			var groupsWithNoCondition = xdoc.Root.Elements(XmlNs + "ItemGroup")
				// We only re-group among item groups without a condition.
				.Where(group => group.Attribute("Condition") == null)
				.ToList();

			var groupedItems = groupsWithNoCondition
				.SelectMany(group => group.Elements())
				.GroupBy(element => element.Name.LocalName)
				.OrderBy(group => group.Key)
				.ToList();

			// Remove all existing item groups.
			foreach (var group in groupsWithNoCondition)
			{
				group.Remove();
			}

			// Insert new groups after the last property group we have.
			var insertAfter = xdoc.Root.Elements(XmlNs + "PropertyGroup").Last();

			foreach (var group in groupedItems)
			{
				var newGroup = new XElement(XmlNs + "ItemGroup", group.ToList());
				insertAfter.AddAfterSelf(newGroup);
				insertAfter = newGroup;
			}
		}

		private void SortItemsByInclude(XDocument xdoc)
		{
			var groups = xdoc.Root.Elements(XmlNs + "ItemGroup");
			foreach (var group in groups)
			{
				var sorted = group.Elements().OrderBy(e => e.Attribute("Include").Value).ToList();
				group.ReplaceNodes(sorted);
			}
		}
	}
}
