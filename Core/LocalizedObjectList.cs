using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Knoema.Localization
{
	[Serializable]
	public class LocalizedObjectList : Dictionary<int, List<ILocalizedObject>>
	{
		public LocalizedObjectList()
		{ }

		public LocalizedObjectList(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

		public void Add(ILocalizedObject item)
		{
			List<ILocalizedObject> itemList;
			if (!base.TryGetValue(item.Hash.GetHashCode(), out itemList))
			{
				itemList = new List<ILocalizedObject>();
				base.Add(item.Hash.GetHashCode(), itemList);
			}
			itemList.Add(item);
		}

		public void AddRange(LocalizedObjectList list)
		{
			foreach (var item in list)
			{
				List<ILocalizedObject> itemList;
				if (!base.TryGetValue(item.Key, out itemList))
				{
					itemList = new List<ILocalizedObject>();
					base.Add(item.Key, itemList);
				}
				itemList.AddRange(item.Value);
			}
		}

		public ILocalizedObject FindItemByCache(string Hash)
		{
			List<ILocalizedObject> itemList;
			if (!base.TryGetValue(Hash.GetHashCode(), out itemList))
				return null;
			return itemList.FirstOrDefault(o => o.Hash == Hash);
		}

		public IEnumerable<ILocalizedObject> ToEnumerable()
		{
			foreach (var list in this)
				foreach (var item in list.Value)
					yield return item;
		}
	}
}
