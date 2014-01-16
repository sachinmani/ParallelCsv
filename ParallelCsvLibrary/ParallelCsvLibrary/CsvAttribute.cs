using System;

namespace CsvLibraryUtils
{
	[AttributeUsage(AttributeTargets.Property)]
	public class CsvAttribute : Attribute
	{
		public int DisplayOrder { get; set; }

		public string DisplayName { get; set; }

		public string ResourceName { get; set; }

		public string ResourceKey { get; set; }

		public int CollectionSize { get; set; }

		public bool FlattenCollection { get; set; } 
	}
}