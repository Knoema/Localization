using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

using Knoema.Localization;

namespace Sample
{
	public class HttpCache : ILocalizationCache
	{
		Cache _cache;

		public HttpCache()
		{
			_cache = HttpRuntime.Cache;
		}

		public object Add(string key, object entry, DateTime utcExpiry, string region = null)
		{
			return _cache.Add(key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}

		public object Get(string key, string region = null)
		{
			return _cache.Get(key);
		}

		public void Remove(string key, string region = null)
		{
			_cache.Remove(key);
		}

		public void Set(string key, object entry, DateTime utcExpiry, string region = null)
		{
			_cache.Insert(key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}

		public void Clear(string region = null)
		{
			var items = _cache.GetEnumerator();

			while (items.MoveNext())
				_cache.Remove(items.Key.ToString());
		}

		const string ChannelPrefix = "channel_";
		public void Subscribe(string channel, Action<string, string> callback)
		{
			var channelKey = ChannelPrefix + channel;
			var callbacks = (List<Action<string, string>>)_cache.Get(channelKey);
			if (callbacks == null)
				callbacks = new List<Action<string,string>>();
			callbacks.Add(callback);
			_cache.Insert(channelKey, callbacks);
		}

		public void Publish(string channel, string message)
		{
			var channelKey = ChannelPrefix + channel;
			var callbacks = (List<Action<string, string>>)_cache.Get(channelKey);
			if (callbacks == null)
				return;
			foreach(var callback in callbacks)
				callback(channel, message);
		}
	}
}