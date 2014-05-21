namespace EasyMerge
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;

	public class SlnSorter
	{
		private string fileName;

		public static void Sort(string fileName)
		{
			File.WriteAllLines(fileName, new SlnSorter(fileName).Sort());
		}

		public static List<string> GetSorted(string fileName)
		{
			return new SlnSorter(fileName).Sort();
		}

		private SlnSorter(string fileName)
		{
			this.fileName = fileName;
		}

		public List<string> Sort()
		{
			var lines = File.ReadAllLines(fileName)
				.Select((content, index) => new Line(content, index))
				.ToList();

			SortProjectsByGuid(lines);
			SortGlobalSections(lines);

			return lines.Select(line => line.Content).ToList();
		}

		private void SortProjectsByGuid(List<Line> lines)
		{
			var projects = lines
				.Where(line =>
					line.Content.StartsWith("Project(\"{", StringComparison.OrdinalIgnoreCase) &&
					lines.Count >= line.Index + 1 &&
					lines[line.Index + 1].Content.Equals("EndProject", StringComparison.OrdinalIgnoreCase))
				.OrderBy(line => line.Content)
				.ToList();

			if (projects.Count == 0)
				return;

			foreach (var index in projects.Select(line => line.Index).OrderByDescending(i => i))
			{
				// As well as its EndProject one.
				lines.RemoveAt(index + 1);
				// Remove original Project position
				lines.RemoveAt(index);
			}

			// Insert at the index of the original first one.
			var insertIndex = projects.OrderBy(line => line.Index).First().Index;
			var projectWithEnd = projects.SelectMany(line => new[] { line, new Line("EndProject", 0) }).ToList();

			lines.InsertRange(insertIndex, projectWithEnd);

			ResetIndexes(lines);
		}

		private void SortGlobalSections(List<Line> lines)
		{
			var begin = lines
				.Where(line => line.Content.Trim().StartsWith("GlobalSection(", StringComparison.OrdinalIgnoreCase))
				.ToList();

			var sections = begin.Select(line => new
				{
					Begin = line,
					Entries = lines.Skip(line.Index + 1)
						.TakeWhile(x => !x.Content.Trim().Equals("EndGlobalSection", StringComparison.OrdinalIgnoreCase))
						.OrderBy(x => x.Content)
						.ToList(),
					End = lines.Skip(line.Index + 1)
						.First(x => x.Content.Trim().Equals("EndGlobalSection", StringComparison.OrdinalIgnoreCase))
				}).ToList();

			foreach (var section in sections)
			{
				lines.RemoveRange(section.Begin.Index + 1, section.Entries.Count);
				lines.InsertRange(section.Begin.Index + 1, section.Entries);
			}

			ResetIndexes(lines);
		}

		private void ResetIndexes(List<Line> lines)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				lines[i].Index = i;
			}
		}

		class Line
		{
			public Line(string content, int index)
			{
				this.Content = content;
				this.Index = index;
			}

			public string Content { get; set; }
			public int Index { get; set; }

			public override string ToString()
			{
				return string.Format("{0,3:###}", Index) + ": " + Content;
			}
		}
	}
}
