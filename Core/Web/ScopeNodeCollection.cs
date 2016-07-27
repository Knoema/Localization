using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Knoema.Localization.Web
{
	public class ScopeEntryCollection : Dictionary<string, ScopeEntry>
	{
		public ScopeEntryCollection()
		{
			Separator = "/";
		}

		public string Separator { get; set; }

		public void AddEntry(string entry, bool notTranslated)
		{
			AddEntry(entry, 0, notTranslated);
		}

		/// <summary>
		/// Parses and adds the entry to the hierarchy, creating any parent entries as required.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="startIndex">The start index.</param>
		public void AddEntry(string entry, int startIndex, bool notTranslated)
		{
			if (entry.IndexOf(Separator) == 0)
				entry = entry.Substring(1);

			if (startIndex >= entry.Length)		
				return;	

			var endIndex = entry.IndexOf(Separator, startIndex);
			if (endIndex == -1)
				endIndex = entry.Length;

			var key = entry.Substring(startIndex, endIndex - startIndex);

			if (string.IsNullOrEmpty(key))
				return;

			ScopeEntry item;

			if (ContainsKey(key.ToLower()))
				item = this[key.ToLower()];
			else
			{
				item = new ScopeEntry 
				{
					Name = key,
					Scope = entry,
					NotTranslated = notTranslated
				};

				Add(key.ToLower(), item);
			}

			// Now add the rest to the new item's children
			item.Children.AddEntry(entry, endIndex + 1, notTranslated);
		}
	}
}
