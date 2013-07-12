using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Knoema.Localization.EFProvider
{
	public class LocalizedObject: ILocalizedObject
	{
		private const string _mark = "[x]";

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Key { get; set; }

		public int LocaleId {get; set;}

		public string Scope {get; set;}

		public string Text {get; set;}

		public string Hash {get; set;}

		public string Translation {get; set;}

		[NotMapped]
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
