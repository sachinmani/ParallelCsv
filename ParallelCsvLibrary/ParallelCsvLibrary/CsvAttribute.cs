using System;

namespace CsvLibraryUtils
{
	public class CsvAttribute : Attribute
	{
		public int DisplayOrder { get; set; }

		public string DisplayName { get; set; }

		public string ResourceName { get; set; }

		public string ResourceKey { get; set; }

		public int CollectionSize { get; set; }
	}
}