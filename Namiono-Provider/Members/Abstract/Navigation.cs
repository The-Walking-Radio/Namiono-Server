using Namiono_Provider.Members.Abstract;
using System;

namespace Namiono_Provider.Members
{
	public class Navigation : Member<Navigation>
	{
		public Navigation(Guid id, string name)
			: base(id, name)
		{ }

		public override void Remove(Guid id)
			=> base.Remove(id);

		public override void Init()
			=> base.Init();

		public override void Add(string name, string target,
			string content, int level, bool active, bool locked)
			=> base.Add(name, target, content, level, active, locked);
	}
}
