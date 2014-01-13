using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CsvLibraryUtils;

namespace CsvParallelLibrary
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var timer = new Stopwatch();

			List<Report> reports = new List<Report>();
			for (int i = 0; i < 1000000; i++)
			{
				Report report = new Report { A = 32, B = string.Format("{0}{1}", "string", i) };
				List<string> list = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M" };
				report.C = list;
				SubSubReport subSubReport = new SubSubReport { A = i, A1 = i, B = string.Format("{0}{1}", "string", i), B1 = string.Format("{0}{1}", "string", i), C = list, C1 = list };
				report.Subreports = new List<SubReport> { new SubReport { C = list, ADR = 34, SubSubReport = subSubReport }, new SubReport { C = list, ADR = 34, SubSubReport = subSubReport } };
				report.SubReport = new SubReport { ADR = 35, C = list, SubSubReport = subSubReport };

				reports.Add(report);
			}

			timer.Start();
			var reportCreator = new CsvReportCreator(new CsvSettings { FlattenArray = true }, true);
			reportCreator.GetCsvString(reports, str => File.AppendAllText(@"C:\Sample.txt", str, Encoding.ASCII));
			reports = null;
			timer.Stop();
			Console.WriteLine(timer.Elapsed);
			Console.Read();
		}
	}

	public class Report : ICsvBase
	{
		[Csv(DisplayName = "Hello", DisplayOrder = 1)]
		public int A { get; set; }

		[Csv(DisplayName = "World", DisplayOrder = 2)]
		public string B { get; set; }

		[Csv(DisplayName = "Cool", DisplayOrder = 3, CollectionSize = 13)]
		public IList<string> C { get; set; }

		[Csv(DisplayName = "ASA", DisplayOrder = 4)]
		public List<SubReport> Subreports { get; set; }

		[Csv]
		public SubReport SubReport { get; set; }
	}

	public class SubReport : ICsvBase
	{
		[Csv(DisplayName = "Cool2", DisplayOrder = 5, CollectionSize = 13)]
		public IList<string> C { get; set; }

		[Csv(DisplayName = "Cool2", DisplayOrder = 6)]
		public int ADR { get; set; }

		[Csv]
		public SubSubReport SubSubReport { get; set; }
	}

	public class SubSubReport : ICsvBase
	{
		[Csv(DisplayName = "Hello", DisplayOrder = 7)]
		public int A { get; set; }

		[Csv(DisplayName = "World", DisplayOrder = 8)]
		public string B { get; set; }

		[Csv(DisplayName = "Cool", DisplayOrder = 9, CollectionSize = 13)]
		public IList<string> C { get; set; }

		[Csv(DisplayName = "Hello", DisplayOrder = 10)]
		public int A1 { get; set; }

		[Csv(DisplayName = "World", DisplayOrder = 11)]
		public string B1 { get; set; }

		[Csv(DisplayName = "Cool", DisplayOrder = 12, CollectionSize = 13)]
		public IList<string> C1 { get; set; }
	}
}