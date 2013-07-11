using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Knoema.Localization
{
	public static class LocalizationCache
	{
		private const string _region = "Localization";

		public static bool Available
		{
			get
			{
				return HttpContext.Current != null && HttpContext.Current.Cache != null;
			}
		}

		public static T Get<T>(string key)
		{
			return (T)HttpContext.Current.Cache.Get(_region + key);
		}

		public static void Insert(string key, object value)
		{
			HttpContext.Current.Cache.Insert(_region + key, value);
		}

		public static void Remove(string key)
		{
			HttpContext.Current.Cache.Remove(_region + key);
		}

		public static void Clear()
		{
			var keys = new List<string>();
			var enumerator = HttpContext.Current.Cache.GetEnumerator();

			while (enumerator.MoveNext())
				if (enumerator.Key.ToString().StartsWith(_region))
					keys.Add(enumerator.Key.ToString());

			foreach (var key in keys)
				HttpContext.Current.Cache.Remove(key);
		}
	}
}
