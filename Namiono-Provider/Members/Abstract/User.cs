using System;

namespace Namiono_Provider.Members
{
	public sealed class User : Member<User>
	{
		public User(Guid id, string name)
			: base(id, name) {}

		public override int Count()
			=> base.Count();

		public override void Init() { }

		public override void Remove(Guid id)
			=> base.Remove(id);

		public override bool Login(string name, string pass)
			=> base.Login(name, pass);

		public override void Add(string name, string target,
			string content, int level, bool active, bool locked)
			=> base.Add(name, target, content, level, active, locked);

		public override void Add(Guid id, string name, string target,
			string content, int level, bool active, bool locked)
			=> base.Add(name, target, content, level, active, locked);
	}
}
