using System;
using System.Reflection;

namespace CsvLibraryUtils
{
	public static class MethodInfoExtension
	{
		public static Delegate CreateGetDelegate(this MethodInfo methodInfo)
		{
			Type returnType = methodInfo.ReturnType;
			var delegateType = typeof(Func<>).MakeGenericType(returnType);
			var getDelegate = Delegate.CreateDelegate(delegateType, null, methodInfo);
			return getDelegate;
		}
	}
}