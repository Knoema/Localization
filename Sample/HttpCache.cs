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

		public object Add(string key, object entry, DateTime utcExpiry)
		{
			return _cache.Add(key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}

		public object Get(string key)
		{
			return _cache.Get(key);
		}

		public void Remove(string key)
		{
			_cache.Remove(key);
		}

		public void Set(string key, object entry, DateTime utcExpiry)
		{
			_cache.Insert(key, entry, null, utcExpiry, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
		}

		public void Clear()
		{
			var items = _cache.GetEnumerator();

			while (items.MoveNext())
				_cache.Remove(items.Key.ToString());
		}
	}
}