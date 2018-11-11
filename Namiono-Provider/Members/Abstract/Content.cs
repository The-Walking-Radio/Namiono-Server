using System;

namespace Namiono_Provider.Members
{
	public sealed class Content : Member<Content>
	{
		public Content(Guid id, string name)
			: base(id, name) {}

		public override void Add(string name, string target,
			string content, int level, bool active, bool locked)
				=> base.Add(name, target, content, level, active, locked);
	}
}
