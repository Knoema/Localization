
namespace Knoema.Localization.Repository
{
	internal class LocalizedObject: ILocalizedObject
	{
		private const string _mark = "[x]";
		public int Key { get; set; }
		public int LocaleId { get; set; }
		public string Hash { get; set; }
		public string Scope { get; set; }
		public string Text { get; set; }
		public string Translation { get; set; }

		public bool IsDisabled
		{
			get
			{
				return !string.IsNullOrEmpty(Translation) && Translation.StartsWith(_mark);
			}
		}

		public void Disable()
		{
			if (!string.IsNullOrEmpty(Translation) && !Translation.StartsWith(_mark))
				Translation = _mark + Translation;
		}
	}
}
