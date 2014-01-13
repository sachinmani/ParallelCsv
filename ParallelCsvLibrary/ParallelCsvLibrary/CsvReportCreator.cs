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
		private readonly bool _runParallel;

		public CsvReportCreator(CsvSettings settings, bool runParallel)
		{
			_csvSettings = settings;
			_runParallel = runParallel;
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
				if (_runParallel)
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
				else
				{
					foreach (var item in items)
					{
						var values = new SortedDictionary<int, string>();
						csvPve.GetPropertysValue(item, values);
						stringBuilder.Append(string.Join(_csvSettings.ValueSeparator, values.Values));
						stringBuilder.Append(Environment.NewLine);
					}
				}
			}
			finally
			{
				createCsvFile(stringBuilder.ToString());
			}
		}
	}
}