using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CsvLibraryUtils
{
	public class InvocationMapping
	{
		private static ConcurrentDictionary<RuntimeMethodHandle, Delegate> _getPropertiesCol;
		private static ConcurrentDictionary<Type, IList<PropertyInfo>> _getPropertiesInfoCol;
		private static ConcurrentDictionary<PropertyInfo, CsvAttribute> _getPropertiesMetadataCol;
		private static bool _go;
		private static readonly object Locker = new object();

		public InvocationMapping()
		{
			int numProcs = Environment.ProcessorCount;
			int concurrencyLevel = numProcs * 2;
			_getPropertiesCol = new ConcurrentDictionary<RuntimeMethodHandle, Delegate>(concurrencyLevel, 50);
			_getPropertiesInfoCol = new ConcurrentDictionary<Type, IList<PropertyInfo>>(concurrencyLevel, 50);
			_getPropertiesMetadataCol = new ConcurrentDictionary<PropertyInfo, CsvAttribute>(concurrencyLevel, 50);
		}

		public static InvocationMapping GetTypeWrappers<T>()
		{
			var mappings = new InvocationMapping();
			Type objectType = typeof(T);
			ExtractObjectProperties(mappings, objectType);
			return mappings;
		}

		public static void ExtractObjectProperties(InvocationMapping mappings, Type templateType)
		{
			lock (Locker)
			{
				_go = false;
			}
			var objectType = templateType;
			var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
										.Where(property => property.GetCustomAttributes(typeof(CsvAttribute), false).SingleOrDefault() != null);
			var propertiesEnumerated = properties.ToList();
			_getPropertiesInfoCol.TryAdd(objectType, propertiesEnumerated);
			foreach (var propertyInfo in propertiesEnumerated)
			{
				var csvAttribute = propertyInfo.GetCustomAttributes(typeof(CsvAttribute), false).OfType<CsvAttribute>().SingleOrDefault();
				var getGetMethod = propertyInfo.GetGetMethod(false);
				var getDelegate = getGetMethod.CreateGetDelegate();
				_getPropertiesCol.TryAdd(getGetMethod.MethodHandle, getDelegate);
				_getPropertiesMetadataCol.TryAdd(propertyInfo, csvAttribute);
			}
			lock (Locker)
			{
				_go = true;
				Monitor.PulseAll(Locker);
			}
		}

		public object Invoke(RuntimeMethodHandle methodHandle, object instance, params object[] parameters)
		{
			var getMethod = _getPropertiesCol[methodHandle].Method;
			return getMethod.Invoke(instance, parameters);
		}

		public bool ContainsType(Type type)
		{
			return _getPropertiesInfoCol.ContainsKey(type);
		}

		public IList<PropertyInfo> GetPropertyList(Type type)
		{
			lock (Locker)
			{
				while (!_go)
				{
					Monitor.Wait(Locker);
				}
			}
			return _getPropertiesInfoCol[type];
		}

		public Delegate GetMethodInfoFromRuntimeMethodHandle(RuntimeMethodHandle handle)
		{
			lock (Locker)
			{
				while (!_go)
				{
					Monitor.Wait(Locker);
				}
			}
			return _getPropertiesCol[handle];
		}

		public CsvAttribute GetPropertyCsvMetadata(PropertyInfo propertyInfo)
		{
			lock (Locker)
			{
				while (!_go)
				{
					Monitor.Wait(Locker);
				}
			}
			return _getPropertiesMetadataCol[propertyInfo];
		}

		public ConcurrentDictionary<PropertyInfo, CsvAttribute> GetAllPropertyCsvMetadata()
		{
			return _getPropertiesMetadataCol;
		}
	}
}