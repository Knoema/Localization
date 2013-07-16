using System.Globalization;

namespace Knoema.Localization
{
	public static class DefaultCulture
	{
		private static CultureInfo _culture = new CultureInfo(1033);
		public static CultureInfo Value
		{
			get { return _culture; }
		}

		public static bool IsDefault(this string name)
		{
			return name == DefaultCulture.Value.Name;
		}

		public static bool IsDefault(this CultureInfo culture)
		{
			return culture.Name == DefaultCulture.Value.Name;
		}
	}
}