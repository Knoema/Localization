using System;
using System.Globalization;

namespace Knoema.Localization
{
	public static class StringExtensions
	{
		public static string Resource(this string value, Type type)
		{
			var scope = LocalizationManagerBase.InstanceBase.FormatScope(type);

			LocalizationManagerBase.InstanceBase.InsertScope(scope.ToLower());

			if (type == null)
				throw new ArgumentNullException();

			if (CultureInfo.CurrentCulture.IsDefault())
				return value;

			if (LocalizationManagerBase.Provider == null)
				return value;

			var translation = LocalizationManagerBase.InstanceBase.Translate(scope, value);
			return string.IsNullOrEmpty(translation)
					? value
					: translation;
		}

		public static string Resource(this string value, object obj)
		{
			return value.Resource(obj.GetType());
		}
	}
}
