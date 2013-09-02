using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Knoema.Localization
{
	public static class LocalizedObjectExtension
	{
		private const string _mark = "[x]";

		public static bool IsDisabled(this ILocalizedObject obj)
		{
			return !string.IsNullOrEmpty(obj.Translation) && obj.Translation.StartsWith(_mark);
		}

		public static void Disable(this ILocalizedObject obj)
		{
			if (!string.IsNullOrEmpty(obj.Translation))
			{
				if (!obj.Translation.StartsWith(_mark))
					obj.Translation = _mark + obj.Translation;
			}
			else
				obj.Translation = _mark;
		}
	}
}
