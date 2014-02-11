#if NUNIT
namespace TsvBits.Serialization.Tests
{
	using NUnit.Framework;
	using Rom;

	[TestFixture]
	public class RdlTests
	{
		private XSerializer _serializer;

		[SetUp]
		public void Init()
		{
			var schema = new Scope("http://test.com")
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
	}
}
#endif