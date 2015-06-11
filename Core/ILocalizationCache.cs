using System;

namespace Knoema.Localization
{
	public interface ILocalizationCache
	{
		object Add(string key, object entry, DateTime utcExpiry);
		object Get(string key);
		void Remove(string key);
		void Set(string key, object entry, DateTime utcExpiry);
		void Clear();
	}
}
