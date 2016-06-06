using System;
using System.Collections.Concurrent;

namespace Knoema.Localization
{
	public static class LocalizationCache
	{
		private static ConcurrentDictionary<string, object> _memoryCache = new ConcurrentDictionary<string, object>();
		static ILocalizationCache _cache;

		const string LocalizationChannel = "localization";
		public static void Initialize(ILocalizationCache cache)
		{
			_cache = cache;
			_cache.Subscribe(LocalizationChannel, OnClearKey);
		}

		public static T Get<T>(string key) where T : class
		{
			object objValue;
			_memoryCache.TryGetValue(key, out objValue);
			var value = (T)objValue;
			if (value == null)
			{
				value = (T)_cache.Get(key);
				if (value != null)
					_memoryCache[key] = value;
			}
			return value;
		}

		public static void Insert<T>(string key, T value)
		{
			var timeStamp = DateTime.UtcNow;
			_cache.Set(key, value, timeStamp.AddDays(1));
			_memoryCache[key] = value;

			_cache.Publish(LocalizationChannel, key);
		}

		public static void Remove(string key)
		{
			_cache.Remove(key);
			object value;
			_memoryCache.TryRemove(key, out value);

			_cache.Publish(LocalizationChannel, key);
		}

		static void OnClearKey(string channel, string key)
		{
			if (channel != LocalizationChannel)
				return;
			object value;
			_memoryCache.TryRemove(key, out value);
		}
	}
}
