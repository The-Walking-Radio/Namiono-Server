using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namiono_Provider.Members.Abstract
{
	public sealed class ContentBox : IDisposable
	{
		public ContentBox(string title ="")
		{
			Id = Guid.NewGuid();
			Title = title;
		}

		public string Output
		{
			get
			{
				var output = string.Format("<div id=\"##Ident##\" class=\"{0}-box margin_b_10 fadeIn\">" +
							"<div class=\"box-header\"><div align=\"left\" width=\"50%\"><h3>{1}</h3></div>"+
							"<div align=\"right\">{2}</div></div>" +
							"<div class=\"box-content\">{3}</div></div>",
							Name, Title, Date, Content).Replace("##Ident##", Id.ToString());
				return output;
			}

			set => Content = value;
		}

		public string Name { get; set; }

		public string Title { get; set; }

		public string Date { get; set; }

		public string Content { get; set; }

		public Guid Id { get; private set; }

		public void Dispose() {}
	}
}
