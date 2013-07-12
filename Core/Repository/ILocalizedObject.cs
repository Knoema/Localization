
namespace Knoema.Localization
{
	public interface ILocalizedObject
	{
		int Key { get; set; }
		int LocaleId { get; set; }		
		string Scope { get; set; }
		string Text { get; set; }
		string Hash { get; set; }
		string Translation { get; set; }
		void Disable();
		bool IsDisabled { get; }
	}
}
