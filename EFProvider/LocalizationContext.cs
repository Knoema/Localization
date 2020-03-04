using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace Knoema.Localization.EFProvider
{
#if false
	public class LocalizationContext : IDisposable
	{
		public class LocalizedObjectList : IEnumerable<LocalizedObject>
		{
			private readonly object _sync;
			private readonly List<LocalizedObject> _objects;
			private int _keyIncrementer;

			public LocalizedObjectList()
			{
				_sync = new object();
				_objects = new List<LocalizedObject>();
				_keyIncrementer = 0;
			}

			private IEnumerator<LocalizedObject> GetEnum()
			{
				lock (_sync)
				{
					return ((IEnumerable<LocalizedObject>)(_objects.ToArray())).GetEnumerator();
				}
			}

			public IEnumerator<LocalizedObject> GetEnumerator()
			{
				return GetEnum();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnum();
			}

			public void AddOrUpdate(IEnumerable<LocalizedObject> items)
			{
				lock (_sync)
				{
					foreach (var item in items)
					{
						var index = Find(item);
						if (index >= 0)
							_objects[index] = item;
						else
						{
							if (item.Key == 0)
								item.Key = ++_keyIncrementer;
							_objects.Add(item);
						}
					}
				}
			}

			public void Remove(LocalizedObject item)
			{
				lock (_sync)
				{
					var index = Find(item);
					if (index >= 0)
						_objects.RemoveAt(index);
				}
			}

			private int Find(LocalizedObject item)
			{
				var itemKey = item.Key;
				if (itemKey != 0)
				{
					for (var i = 0; i < _objects.Count; i++)
					{
						if (_objects[i].Key == itemKey)
							return i;
					}
				}
				return -1;
			}
		}

		public LocalizedObjectList Objects { get; } = new LocalizedObjectList();

		public LocalizationContext()
		{
		}

		public LocalizationContext(string nameOrConnectionString)
		{
		}

		public LocalizationContext(DbConnection existingConnection, bool contextOwnsConnection)
		{
		}

		void IDisposable.Dispose()
		{
		}

		public void SaveChanges()
		{
		}
	}
#else
	public class LocalizationContext: DbContext
	{
		public IDbSet<LocalizedObject> Objects { get; set; }

		public LocalizationContext()
		{
		}

		public LocalizationContext(string nameOrConnectionString) :
			base(nameOrConnectionString)
		{
		}

		public LocalizationContext(DbConnection existingConnection, bool contextOwnsConnection) :
			base(existingConnection, contextOwnsConnection)
		{
		}
	}
#endif
}
