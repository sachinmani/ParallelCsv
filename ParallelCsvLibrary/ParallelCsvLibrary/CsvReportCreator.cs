using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CsvLibraryUtils
{
	/// <summary>
	/// Facade layer responsible for handling all CSV report related operations.
	/// </summary>
	public class CsvReportCreator
	{
		private readonly CsvSettings _csvSettings;

		public CsvReportCreator(CsvSettings settings)
		{
			_csvSettings = settings;
		}

		/// <summary>
		/// Takes a collection of items and returns a csv string.
		/// </summary>
		/// <typeparam name="T">Type derived from <see cref="ICsvBase"/></typeparam>
		/// <param name="items">List of items of type <typeparamref name="T"/></param>
		/// <param name="createCsvFile"></param>
		/// <returns>csv string</returns>
		public void GetCsvString<T>(IList<T> items, Action<string> createCsvFile)
		{
			var csvPve = new CsvPropertyValueExtractor(_csvSettings);
			const double maxSize = 10000 * 7000;
			int numProcs = Environment.ProcessorCount;
			int concurrencyLevel = numProcs * 2;
			bool headerCreated = false;
			var stringBuilder = new StringBuilder();
			try
			{
				var headers = new SortedDictionary<int, string>();
				var locker = new object();
				if (true)
				{
					Parallel.ForEach(items, new ParallelOptions { /*MaxDegreeOfParallelism = concurrencyLevel*/ }, () => new StringBuilder(), (item, loopState, csvBuilder) =>
						{
							var values = new SortedDictionary<int, string>();
							csvPve.GetPropertysValue(item, values);
							csvBuilder.Append(string.Join(_csvSettings.ValueSeparator, values.Values));
							csvBuilder.Append(Environment.NewLine);
							return csvBuilder;
						},
						csvBuilder =>
						{
							lock (locker)
							{
								stringBuilder.Append(csvBuilder);
								if ((stringBuilder.Length * sizeof(Char)) <= (maxSize)) return;
								var formattedVal = stringBuilder.ToString();
								if (!headerCreated)
								{
									csvPve.GetCsvHeaders(headers);
									formattedVal = string.Format("{0}\n{1}", string.Join(_csvSettings.HeaderSeparator, headers.Values), stringBuilder);
									headerCreated = true;
								}
								var createFileAction = createCsvFile;
								createFileAction.Invoke(formattedVal);
								formattedVal = null;
								headers = null;
								stringBuilder.Clear();
							}
						});
				}
			}
			finally
			{
				createCsvFile(stringBuilder.ToString());
			}
		}
	}
}