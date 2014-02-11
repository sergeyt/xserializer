#if NUNIT
using System.Xml.Linq;
using TsvBits.Serialization.Tests.Rom;

namespace TsvBits.Serialization.Tests
{
	internal static class Rdl
	{
		public static readonly IScope Schema;

		static Rdl()
		{
			var schema = new Scope(Ns.Rdl2005)
				.Type(s => Length.Parse(s), x => x.IsValid ? x.ToString() : "")
				.Type(s => ExpressionInfo.Parse(s), x => x.ToString())
				.Enum(DataElementOutput.Auto);

			schema.Element<Report>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Elements()
				.Add(x => x.Width)
				.Add(x => x.Body);

			schema.Element<Body>()
				.Elements()
				.Add(x => x.Height)
				.Add(x => x.ReportItems);

			var item = schema.Element<ReportItem>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Elements()
				.Add(x => x.DataElementName)
				.Add(x => x.DataElementOutput)
				.End();

			item.Sub<TextBox>()
				.Elements()
				.Add(x => x.Value);

			item.Sub<Rectangle>()
				.Elements()
				.Add(x => x.ReportItems);

			Schema = schema;
		}
	}
}
#endif
