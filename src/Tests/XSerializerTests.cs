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
			var schema = Scope.New("http://test.com")
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

			_serializer = XSerializer.New(schema);
		}

		[TestCase(Format.Xml, Result = "<Report xmlns=\"http://test.com\"><Body /></Report>")]
		[TestCase(Format.Json, Result = "{\"Body\":{}}")]
		[TestCase(Format.JsonML, Result = "[\"Report\",{\"xmlns\":\"http://test.com\"},[\"Body\"]]")]
		public string WriteDefaultReport(Format format)
		{
			var report = new Report();
			return _serializer.ToString(report, format);
		}

		[TestCase(Format.Xml,
			"<Report Name=\"report\" xmlns=\"http://test.com\"><Width>12in</Width><Body><ReportItems><TextBox Name=\"textbox1\"><DataElementOutput>NoContent</DataElementOutput><Value>hello</Value></TextBox><Rectangle><ReportItems><TextBox Name=\"textbox2\"><Value>world</Value></TextBox></ReportItems></Rectangle></ReportItems></Body></Report>"
			)]
		[TestCase(Format.Json,
			"{\"Name\":\"report\",\"Width\":\"12in\",\"Body\":{\"ReportItems\":[new TextBox({\"Name\":\"textbox1\",\"DataElementOutput\":\"NoContent\",\"Value\":\"hello\"}),new Rectangle({\"ReportItems\":[new TextBox({\"Name\":\"textbox2\",\"Value\":\"world\"})]})]}}"
			)]
		[TestCase(Format.JsonML,
			"[\"Report\",{\"xmlns\":\"http://test.com\",\"Name\":\"report\"},[\"Width\",\"12in\"],[\"Body\",[\"ReportItems\",[\"TextBox\",{\"Name\":\"textbox1\"},[\"DataElementOutput\",\"NoContent\"],[\"Value\",\"hello\"]],[\"Rectangle\",[\"ReportItems\",[\"TextBox\",{\"Name\":\"textbox2\"},[\"Value\",\"world\"]]]]]]]"
			)]
		public void WriteReadReport(Format format, string expectedString)
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

			var reportString = _serializer.ToString(report, format);
			Assert.AreEqual(expectedString, reportString);

			var report2 = _serializer.Parse<Report>(reportString, format);

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

		public enum DataElementOutput
		{
			Auto,
			NoContent
		}

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

		public class ExpressionInfo
		{
			private readonly string _expression;

			private ExpressionInfo(string expression)
			{
				_expression = expression;
			}

			public static ExpressionInfo Parse(string s)
			{
				return new ExpressionInfo(s);
			}

			public override string ToString()
			{
				return _expression;
			}
		}
	}
}
#endif