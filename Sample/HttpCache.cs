using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;

using Knoema.Localization;

namespace Sample
{
	public class HttpCache : ILocalizationCache
	{
		Cache _cache;
		Dictionary<string, List<Action<string, string>>> _callbacks;
		object _callbacksLock = new object();

		public HttpCache()
		{
			_cache = HttpRuntime.Cache;
			_callbacks = new Dictionary<string, List<Action<string, string>>>(StringComparer.OrdinalIgnoreCase);
		}

		public object Add(string key, object entry, DateTime utcExpiry, string region = null)
		{
			return _cache.Add((region ?? string.Empty) + key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}

		public object Get(string key, string region = null)
		{
			return _cache.Get((region ?? string.Empty) + key);
		}

		public void Remove(string key, string region = null)
		{
			_cache.Remove((region ?? string.Empty) + key);
		}

		public void Set(string key, object entry, DateTime utcExpiry, string region = null)
		{
			_cache.Insert((region ?? string.Empty) + key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}
		
		public void Clear(string region = null)
		{
			var items = _cache.GetEnumerator();

			while (items.MoveNext())
			{
				if (region == null || items.Key.ToString().StartsWith(region))
					_cache.Remove(items.Key.ToString());
			}
		}

		const string ChannelPrefix = "channel_";
		public void Subscribe(string channel, Action<string, string> callback)
		{
			var channelKey = ChannelPrefix + channel;
			lock (_callbacksLock)
			{
				List<Action<string, string>> callbacks;
				_callbacks.TryGetValue(channelKey, out callbacks);
				if (callbacks == null)
					callbacks = new List<Action<string, string>>();
				callbacks.Add(callback);
				_callbacks[channelKey] = callbacks;
			}
		}

		public void Publish(string channel, string message)
		{
			var channelKey = ChannelPrefix + channel;
			List<Action<string, string>> callbacks;
			lock (_callbacksLock)
			{
				_callbacks.TryGetValue(channelKey, out callbacks);
				if (callbacks == null)
					return;

				callbacks = new List<Action<string, string>>(callbacks);
			}

			foreach (var callback in callbacks)
				callback(channel, message);
		}

		public bool SubscribeAndRegionsSupported()
		{
			return true;
		}
	}
}