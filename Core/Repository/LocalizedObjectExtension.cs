using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Knoema.Localization
{
	public static class LocalizedObjectExtension
	{
		private const string _mark = "[x]";

		public static bool IsDeleted(this ILocalizedObject obj)
		{
			return !string.IsNullOrEmpty(obj.Translation) && obj.Translation.StartsWith(_mark);
		}

		public static void Delete(this ILocalizedObject obj)
		{
			if (!string.IsNullOrEmpty(obj.Translation))
			{
				if (!obj.Translation.StartsWith(_mark))
					obj.Translation = _mark + obj.Translation;
			}
			else
				obj.Translation = _mark;
		}

		public static void Recover(this ILocalizedObject obj)
		{
			if (!string.IsNullOrEmpty(obj.Translation) && obj.Translation.StartsWith(_mark))
				obj.Translation = obj.Translation.Substring(_mark.Length);
		}
	}
}
