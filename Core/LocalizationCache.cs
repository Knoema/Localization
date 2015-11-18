using System;
using System.Collections.Concurrent;

namespace Knoema.Localization
{
	public static class LocalizationCache
	{
		private static ConcurrentDictionary<string, object> _memoryCache = new ConcurrentDictionary<string, object>();
		static ILocalizationCache _cache;
		public static void Initialize(ILocalizationCache cache)
		{
			_cache = cache;
		}

		public static T Get<T>(string key) where T : class
		{
			var timeStamp = (DateTime?)_cache.Get("stamp_" + key);
			if (!timeStamp.HasValue)
				return null;
			object localTimeStamp;
			if (!_memoryCache.TryGetValue("stamp_" + key, out localTimeStamp))
				localTimeStamp = null;
			T value = null;
			if (localTimeStamp == null || (DateTime?)localTimeStamp < timeStamp)
			{
				value = (T)_cache.Get(key);
				if (value != null)
				{
					_memoryCache[key] = value;
					_memoryCache["stamp_" + key] = timeStamp;
				}
			}
			else
				value = (T)_memoryCache[key];
			return value;
		}

		public static void Insert<T>(string key, T value)
		{
			var timeStamp = DateTime.UtcNow;
			_cache.Set("stamp_" + key, timeStamp, timeStamp.AddDays(1));
			_cache.Set(key, value, timeStamp.AddDays(1));
			_memoryCache["stamp_" + key] = timeStamp;
			_memoryCache[key] = value;
		}

		public static void Remove(string key)
		{
			_cache.Remove(key);
			_cache.Remove("stamp_" + key);
			object value;
			_memoryCache.TryRemove(key, out value);
			_memoryCache.TryRemove("stamp_" + key, out value);
		}
	}
}
