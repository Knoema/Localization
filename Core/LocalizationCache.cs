using System;
using System.Collections.Concurrent;

namespace Knoema.Localization
{
	public static class LocalizationCache
	{
		private static ConcurrentDictionary<string, object> _memoryCache = new ConcurrentDictionary<string, object>();
		static ILocalizationCache _cache;
		static bool _subscribeSupported = false;

		const string LocalizationChannel = "localization";
		public static void Initialize(ILocalizationCache cache)
		{
			_cache = cache;
			_subscribeSupported = _cache.SubscribeAndRegionsSupported();
			if (_subscribeSupported)
				_cache.Subscribe(LocalizationChannel, OnClearKey);
		}

		public static T Get<T>(string key) where T : class
		{
			if (_subscribeSupported)
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
			else
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
		}

		public static void Insert<T>(string key, T value)
		{
			var timeStamp = DateTime.UtcNow;
			_cache.Set(key, value, timeStamp.AddDays(1));
			_memoryCache[key] = value;

			if (_subscribeSupported)
			{
				_cache.Publish(LocalizationChannel, key);
			}
			else
			{
				_cache.Set("stamp_" + key, timeStamp, timeStamp.AddDays(1));
				_memoryCache["stamp_" + key] = timeStamp;
			}
		}

		public static void Remove(string key)
		{
			_cache.Remove(key);
			if (_subscribeSupported)
			{
				object value;
				_memoryCache.TryRemove(key, out value);
				_cache.Publish(LocalizationChannel, key);
			}
			else
				_cache.Remove("stamp_" + key);
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
