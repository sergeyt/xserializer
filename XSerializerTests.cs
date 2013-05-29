using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;

namespace XmlSerialization
{
	[TestFixture]
	public class XSerializerTests
	{
		private XSerializer _serializer;

		[SetUp]
		public void Init()
		{
			var ns = XNamespace.Get("http://test.com");
			var report = ElementDef.New<Report>(ns + "Report")
								   .Attr(o => o.Name, (o, v) => o.Name = v)
								   .Elem(o => o.Width, (o, v) => o.Width = v)
								   .Elem(o => o.Body);

			var body = ElementDef.New<Body>(ns + "Body")
								 .Elem(o => o.Height, (o, v) => o.Height = v)
								 .Elem(o => o.ReportItems);

			var item = ElementDef.New<ReportItem>(ns + "ReportItemBase")
								 .Attr(x => x.Name, (x, v) => x.Name = v);

			var textbox = item.Sub<TextBox>(ns + "TextBox");

			_serializer = new XSerializer()
				.Elem(report)
				.Elem(body)
				.Elem(textbox);
		}

		[Test]
		public void DefaultReport()
		{
			var report = new Report();
			var xml = _serializer.ToXmlString(report);
			Assert.AreEqual("<Report xmlns=\"http://test.com\"/>", xml);
		}

		public class Report
		{
			private readonly Body _body = new Body();

			public string Name { get; set; }
			public Length Width { get; set; }

			public Body Body
			{
				get { return _body; }
			}
		}

		public class Body
		{
			public Body()
			{
				ReportItems = new List<ReportItem>();
			}

			public Length Height { get; set; }

			public IList<ReportItem> ReportItems { get; private set; }
		}

		public abstract class ReportItem
		{
			public string Name { get; set; }
		}

		public class TextBox : ReportItem
		{
			public string Value { get; set; }
		}

		public struct Length
		{
			public float Value;
			public string Unit;

			public Length Parse(string s)
			{
				throw new NotImplementedException();
			}
		}
	}
}