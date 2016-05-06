using System;

namespace Knoema.Localization
{
	public interface ILocalizationCache
	{
		object Add(string key, object entry, DateTime utcExpiry, string region = null);
		object Get(string key, string region = null);
		void Remove(string key, string region = null);
		void Set(string key, object entry, DateTime utcExpiry, string region = null);
		void Clear(string region = null);

		void Subscribe(string channel, Action<string, string> callback);
		void Publish(string channel, string message);
	}
}
