using System;

namespace Knoema.Localization.Repository
{
	[Serializable]
	internal class LocalizedObject : ILocalizedObject
	{
		public int Key { get; set; }
		public int LocaleId { get; set; }
		public string Hash { get; set; }
		public string Scope { get; set; }
		public string Text { get; set; }
		public string Translation { get; set; }
	}
}
