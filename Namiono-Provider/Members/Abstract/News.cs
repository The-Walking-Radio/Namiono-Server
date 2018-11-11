using System;

namespace Namiono_Provider.Members
{
	public sealed class News : Member<News>
	{
		public News(Guid id, string name)
			: base(id, name) { }

		public override void Remove(Guid id)
			=> base.Remove(id);

		public override void Add(string name, string target,
			string content, int level, bool active, bool locked)
				=> base.Add(name, target, content, level, active, locked);
	}
}
