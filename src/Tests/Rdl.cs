#if NUNIT
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

			var rdl = new[] {Ns.Rdl2005, Ns.Rdl2003, Ns.Rdl2008, Ns.Rdl2010};

			schema.Element<Report>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Elements(rdl)
				.Add(x => x.Width)
				.Add(x => x.Body)
				.End()
				.Use(rdl);

			schema.Element<Body>()
				.Elements(rdl)
				.Add(x => x.Height)
				.Add(x => x.ReportItems)
				.End()
				.Use(rdl);

			var item = schema.Element<ReportItem>()
				.Attributes()
				.Add(x => x.Name)
				.End()
				.Elements(rdl)
				.Add(x => x.DataElementName)
				.Add(x => x.DataElementOutput)
				.End();

			item.Sub<TextBox>()
				.Elements(rdl)
				.Add(x => x.Value)
				.End()
				.Use(rdl);

			item.Sub<Rectangle>()
				.Elements(rdl)
				.Add(x => x.ReportItems)
				.End()
				.Use(rdl);

			Schema = schema;
		}
	}
}
#endif
