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
	}
}