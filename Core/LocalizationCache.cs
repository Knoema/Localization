using System.Collections.Concurrent;

namespace Knoema.Localization
{
	public static class LocalizationCache
	{
		private static ConcurrentDictionary<string, object> _cache =  new ConcurrentDictionary<string, object>();

		public static T Get<T>(string key)
		{
			object value;
			return (T)(_cache.TryGetValue(key, out value) ? value : null);
		}

		public static void Insert(string key, object value)
		{
			_cache[key] = value;
		}

		public static void Clear()
		{
			_cache.Clear();
		}
	}
}
