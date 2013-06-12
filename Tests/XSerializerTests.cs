#if NUNIT
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class XSerializerTests
	{
		private XSerializer _serializer;

		[SetUp]
		public void Init()
		{
			var scope = Scope.New("http://test.com")
			                 .Type(s => Length.Parse(s), x => x.IsValid ? x.ToString() : "")
			                 .Enum(DataElementOutput.Auto);

			scope.Elem<Report>()
			     .Attr(x => x.Name)
			     .Elem(x => x.Width)
			     .Elem(x => x.Body);

			scope.Elem<Body>()
			     .Elem(x => x.Height)
			     .Elem(x => x.ReportItems);

			var item = scope.Elem<ReportItem>()
			                .Attr(x => x.Name)
			                .Elem(x => x.DataElementName)
			                .Elem(x => x.DataElementOutput);

			item.Sub<TextBox>()
			    .Elem(x => x.Value);

			item.Sub<Rectangle>()
			    .Elem(x => x.ReportItems);

			_serializer = XSerializer.New(scope);
		}

		[TestCase(Format.Xml, Result = "<Report xmlns=\"http://test.com\"><Body /></Report>")]
		[TestCase(Format.Json, Result = "{\"Body\":{}}")]
		public string WriteDefaultReport(Format format)
		{
			var report = new Report();
			return _serializer.ToString(report, format);
		}

		[Test]
		public void WriteReadReport()
		{
			var textbox1 = new TextBox {Name = "textbox1", Value = "hello", DataElementOutput = DataElementOutput.NoContent};
			var textbox2 = new TextBox {Name = "textbox2", Value = "world"};
			var rect1 = new Rectangle
				{
					ReportItems = {textbox2}
				};
			var report = new Report
				{
					Name = "report",
					Width = "12in",
					Body =
						{
							ReportItems = {textbox1, rect1}
						}
				};
			
			var xml = _serializer.ToXmlString(report, true);
			Assert.AreEqual("<Report Name=\"report\" xmlns=\"http://test.com\"><Width>12in</Width><Body><ReportItems><TextBox Name=\"textbox1\"><DataElementOutput>NoContent</DataElementOutput><Value>hello</Value></TextBox><Rectangle><ReportItems><TextBox Name=\"textbox2\"><Value>world</Value></TextBox></ReportItems></Rectangle></ReportItems></Body></Report>", xml);

			var report2 = _serializer.Parse<Report>(xml);

			Assert.AreEqual(report.Name, report2.Name);
			Assert.AreEqual(report.Width, report2.Width);
			Assert.AreEqual(report.Body.ReportItems.Count, report2.Body.ReportItems.Count);

			var tb1 = report2.Body.ReportItems[0] as TextBox;
			Assert.NotNull(tb1);
			Assert.AreEqual(textbox1.Name, tb1.Name);
			Assert.AreEqual(textbox1.Value, tb1.Value);
			Assert.AreEqual(textbox1.DataElementOutput, tb1.DataElementOutput);

			var rect2 = report2.Body.ReportItems[1] as Rectangle;
			Assert.NotNull(rect2);
			Assert.AreEqual(rect1.ReportItems.Count, rect2.ReportItems.Count);

			var tb2 = rect2.ReportItems[0] as TextBox;
			Assert.NotNull(tb2);
			Assert.AreEqual(textbox2.Name, tb2.Name);
			Assert.AreEqual(textbox2.Value, tb2.Value);
			Assert.AreEqual(textbox2.DataElementOutput, tb2.DataElementOutput);
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
				ReportItems = new ReportItemCollection();
			}

			public Length Height { get; set; }

			public ReportItemCollection ReportItems { get; private set; }
		}

		public enum DataElementOutput { Auto, NoContent }

		public abstract class ReportItem
		{
			public string Name { get; set; }
			public string DataElementName { get; set; }
			public DataElementOutput DataElementOutput { get; set; }
		}

		public class ReportItemCollection : IEnumerable<ReportItem>
		{
			private readonly List<ReportItem> _list = new List<ReportItem>();

			public int Count
			{
				get { return _list.Count; }
			}

			public ReportItem this[int index]
			{
				get { return _list[index]; }
			}

			public IEnumerator<ReportItem> GetEnumerator()
			{
				return _list.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void Add(ReportItem item)
			{
				_list.Add(item);
			}

			// intentionally for testing
			public void Add(TextBox item)
			{
				_list.Add(item);
			}
		}

		public class TextBox : ReportItem
		{
			public string Value { get; set; }
		}

		public class Rectangle : ReportItem
		{
			public Rectangle()
			{
				ReportItems = new ReportItemCollection();
			}

			public ReportItemCollection ReportItems { get; private set; }
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