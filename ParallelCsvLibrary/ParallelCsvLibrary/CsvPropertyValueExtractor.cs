using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CsvLibraryUtils
{
	internal class CsvPropertyValueExtractor
	{
		private static InvocationMapping _mappings;
		private readonly CsvSettings _settings;

		public CsvPropertyValueExtractor(CsvSettings settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// Get the type and check and return if it is not derived from <see cref="ICsvBase"/> and if its
		/// properties details are all already extracted. Else passed to <see cref="InvocationMapping"/>
		/// for extraction.
		/// </summary>
		/// <param name="type">type whose information needs to retrieved or extracted.</param>
		private void ExtractPropertiesDetails(Type type)
		{
			var locker = new object();
			var objectType = type;
			if (!typeof(ICsvBase).IsAssignableFrom(objectType)) return;
			lock (locker)
			{
				if (_mappings != null && _mappings.ContainsType(type)) return;
			}

			if (_mappings == null)
			{
				//Can't make a direct call as GetTypeWrappers is a Generic method
				var methodInfo = typeof(InvocationMapping).GetMethod("GetTypeWrappers",
																														  BindingFlags.Static | BindingFlags.Public);
				var genericMethod = methodInfo.MakeGenericMethod(objectType);
				_mappings = (InvocationMapping)genericMethod.Invoke(null, null);
			}
			else
			{
				//Can't make a direct call as ExtractObjectProperties is a Generic method
				var methodInfo = typeof(InvocationMapping).GetMethod("ExtractObjectProperties",
																														  BindingFlags.Static | BindingFlags.Public);
				methodInfo.Invoke(null, new object[] { _mappings, objectType });
			}
		}

		/// <summary>
		/// Get all type properties, iterates over all properties and extract their values from the object
		/// instance and insert it to the <see cref="IDictionary"/>.
		/// </summary>
		/// <typeparam name="T">Type derived from <see cref="ICsvBase"/></typeparam>
		/// <param name="instance">instance of type<typeparamref name="T"/> whose properties value need to be extracted.</param>
		/// <param name="propertyValues"><see cref="IDictionary"/> containing the display order(position in the CSV) as key
		/// and property value as value</param>
		public void GetPropertysValue<T>(T instance, IDictionary<int, string> propertyValues)
		{
			var objectType = instance.GetType();
			ExtractPropertiesDetails(objectType);
			var properties = _mappings.GetPropertyList(objectType);
			foreach (var property in properties)
			{
				var propertyType = property.PropertyType;
				if (propertyType.IsValueType || propertyType == typeof(string))
				{
					var csvAttr = _mappings.GetPropertyCsvMetadata(property);
					var instanceValue = _mappings.Invoke(property.GetGetMethod().MethodHandle, instance, null);
					propertyValues.Add(csvAttr.DisplayOrder, instanceValue.ToString());
				}
				else if (typeof(ICsvBase).IsAssignableFrom(propertyType))
				{
					var instanceValue = _mappings.Invoke(property.GetGetMethod().MethodHandle, instance, null);
					GetPropertysValue(instanceValue, propertyValues);
				}
				else if (propertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyType))
				{
					GetCollectionPropertyValue(property, instance, propertyValues);
				}
				else if (propertyType == typeof(Tuple<>))
				{
				}
			}
		}

		/// <summary>
		/// This method determines the last generic arguement of the collection and format the collection accordingly.
		/// Collection of complex objects are formatted in the way shown below: [{a,b,c},{a,b,c},{a,b,c}]. And the collection of 
		/// simple types are formatted in the way specified in csvsettings [a,b,c] or a,b,c.
		/// </summary>
		/// <typeparam name="T">Type derived from <see cref="ICsvBase"/></typeparam>
		/// <param name="instance">instance of type<typeparamref name="T"/> whose properties value need to be extracted.</param>
		/// <param name="propertyValues"><see cref="IDictionary"/> containing the display order(position in the CSV) as key
		/// and property value as value</param>
		public void GetCollectionPropertyValue<T>(PropertyInfo property, T instance, IDictionary<int, string> values)
		{
			var collectionParamType = property.PropertyType.GetGenericArguments();
			if (typeof(ICsvBase).IsAssignableFrom(collectionParamType.Last()))
			{
				ExtractComplexCollectionValue(property, instance, values);
				return;
			}

			if (collectionParamType.Count() > 1)
			{
				var propertyValues = _mappings.Invoke(property.GetGetMethod().MethodHandle, instance, null);
				if (propertyValues is IDictionary)
				{
					var collectionValues = propertyValues.GetType().GetProperty("Values");
					var csvAttr = _mappings.GetPropertyCsvMetadata(property);
					values.Add(csvAttr.DisplayOrder, _settings.FlattenArray
											? string.Join(_settings.ValueSeparator, collectionValues)
											: string.Format("[{0}]", string.Join(_settings.ValueSeparator, collectionValues)));
				}
			}
			else
			{
				var propertyValues = _mappings.Invoke(property.GetGetMethod().MethodHandle, instance, null);
				if (propertyValues is IList)
				{
					var csvAttr = _mappings.GetPropertyCsvMetadata(property);
					values.Add(csvAttr.DisplayOrder, _settings.FlattenArray
											? string.Join(_settings.ValueSeparator, (IList<string>)propertyValues)
											: string.Format("[{0}]", string.Join(_settings.ValueSeparator, (IList<string>)propertyValues)));
				}
			}
		}

		/// <summary>
		/// Extract values from the property.
		/// </summary>
		/// <typeparam name="T">Type derived from <see cref="ICsvBase"/></typeparam>
		/// <param name="instance">instance of type<typeparamref name="T"/> whose properties value need to be extracted.</param>
		/// <param name="propertyValues"><see cref="IDictionary"/> containing the display order(position in the CSV) as key
		/// and property value as value</param>
		private void ExtractComplexCollectionValue<T>(PropertyInfo property, T instance, IDictionary<int, string> values)
		{
			var lockObj = new object();
			var propertyValues = _mappings.Invoke(property.GetGetMethod().MethodHandle, instance, null) as IEnumerable;
			var propertyValuesCsv = string.Empty;
			if (propertyValues != null)
			{
				Parallel.ForEach(propertyValues.Cast<object>(), new ParallelOptions(),
																	propertyValue =>
																	{
																		IDictionary<int, string> dictionary = new Dictionary<int, string>();
																		GetPropertysValue(propertyValue, dictionary);
																		var csvVal = string.Format("{{{0}}}", string.Join(_settings.ValueSeparator, dictionary.Values));
																		lock (lockObj)
																		{
																			propertyValuesCsv += csvVal;
																		}
																	});
			}
			var csvAttr = _mappings.GetPropertyCsvMetadata(property);
			values.Add(csvAttr.DisplayOrder, string.Format("[{0}]", propertyValuesCsv));
		}

		/// <summary>
		/// Constructing headers for the csv.
		/// </summary>
		/// <param name="headers"><see cref="IDictionary"/> containing the header string.</param>
		public void GetCsvHeaders(IDictionary<int, string> headers)
		{
			var propertyMetadatas = _mappings.GetAllPropertyCsvMetadata();
			foreach (KeyValuePair<PropertyInfo, CsvAttribute> propertyMetadata in propertyMetadatas)
			{
				var propertyType = propertyMetadata.Key.PropertyType;
				if (propertyType.IsGenericType &&
											typeof(ICsvBase).IsAssignableFrom(propertyType.GetGenericArguments().Last()))
				{
					var csvAttr = propertyMetadata.Value;
					headers.Add(csvAttr.DisplayOrder, csvAttr.DisplayName);
					continue;
				}
				if (propertyType.IsValueType || propertyType == typeof(string))
				{
					var csvAttr = propertyMetadata.Value;
					headers.Add(csvAttr.DisplayOrder, csvAttr.DisplayName);
					continue;
				}
				if (typeof(IEnumerable).IsAssignableFrom(propertyType))
				{
					if (_settings.FlattenArray)
					{
						var csvAttr = propertyMetadata.Value;
						var sb = new StringBuilder();
						for (int i = 0; i < csvAttr.CollectionSize; i++)
						{
							sb.Append(string.Format("{0}{1}{2}", csvAttr.DisplayName, i, i == csvAttr.CollectionSize - 1 ? string.Empty : _settings.HeaderSeparator));
						}
						headers.Add(csvAttr.DisplayOrder, sb.ToString());
					}
					else
					{
						var csvAttr = propertyMetadata.Value;
						headers.Add(csvAttr.DisplayOrder, csvAttr.DisplayName);
					}
				}
			}
		}
	}
}