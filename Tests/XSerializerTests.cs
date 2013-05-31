#if NUNIT
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using NUnit.Framework;

namespace XmlSerialization.Tests
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
			                       .Attr(x => x.Name)
			                       .Elem(x => x.Width)
			                       .Elem(x => x.Body);

			var body = ElementDef.New<Body>(ns + "Body")
			                     .Elem(x => x.Height)
			                     .Elem(x => x.ReportItems);

			var item = ElementDef.New<ReportItem>(ns + "ReportItemBase")
			                     .Attr(x => x.Name);

			var textbox = item.Sub<TextBox>(ns + "TextBox")
			                  .Elem(x => x.Value);

			_serializer = XSerializer.New(report, body, textbox)
			                         .Type(s => Length.Parse(s), x => x.IsValid ? x.ToString() : "");
		}

		[Test]
		public void WriteDefaultReport()
		{
			var report = new Report();
			var xml = _serializer.ToXmlString(report, true);
			Assert.AreEqual("<Report xmlns=\"http://test.com\"><Body /></Report>", xml);
		}

		[Test]
		public void SimpleReport()
		{
			var textbox1 = new TextBox {Name = "textbox1", Value = "hello"};
			var report = new Report
				{
					Name = "report",
					Width = "12in",
					Body =
						{
							ReportItems = {textbox1}
						}
				};
			
			var xml = _serializer.ToXmlString(report, true);
			Assert.AreEqual("<Report Name=\"report\" xmlns=\"http://test.com\"><Width>12in</Width><Body><ReportItems><TextBox Name=\"textbox1\"><Value>hello</Value></TextBox></ReportItems></Body></Report>", xml);

			var report2 = _serializer.Parse<Report>(xml);
			Assert.AreEqual(report.Name, report2.Name);
			Assert.AreEqual(report.Width, report2.Width);
			Assert.AreEqual(report.Body.ReportItems.Count, report2.Body.ReportItems.Count);
			var textbox2 = report2.Body.ReportItems[0] as TextBox;
			Assert.NotNull(textbox2);
			Assert.AreEqual(textbox1.Name, textbox2.Name);
			Assert.AreEqual(textbox1.Value, textbox2.Value);
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
			public readonly float Value;
			public readonly string Unit;

			public Length(float value, string unit)
			{
				Value = value;
				Unit = unit;
			}

			public bool IsValid
			{
				get { return !string.IsNullOrEmpty(Unit); }
			}

			public static implicit operator Length(string s)
			{
				return Parse(s);
			}

			public static Length Parse(string s)
			{
				if (string.IsNullOrEmpty(s) || s.Length <= 2)
					return default(Length);

				float value;
				if (!float.TryParse(s.Substring(0, s.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
					return default(Length);

				var unit = s.Substring(s.Length - 2);
				return new Length(value, unit);
			}

			public override string ToString()
			{
				return IsValid ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", Value, Unit) : "invalid";
			}
		}
	}
}
#endif