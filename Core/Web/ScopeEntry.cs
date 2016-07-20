
namespace Knoema.Localization.Web
{
	public class ScopeEntry
	{
		public string Name { get; set; }
		public string Scope { get; set; }
		public bool NotTranslated { get; set; }
		public ScopeEntryCollection Children { get; set; }

		public ScopeEntry()
		{
			Children = new ScopeEntryCollection();
		}

		public override string ToString()
		{
			return Scope;
		}
	}
}
