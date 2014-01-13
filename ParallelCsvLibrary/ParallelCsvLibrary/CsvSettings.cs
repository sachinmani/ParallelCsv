namespace CsvLibraryUtils
{
	public class CsvSettings
	{
		public CsvSettings()
		{
			HeaderSeparator = ",";
			ValueSeparator = ",";
		}

		public string HeaderSeparator { get; set; }

		public string ValueSeparator { get; set; }

		public bool FlattenArray { get; set; }
	}
}