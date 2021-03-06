﻿#if NUNIT
namespace TsvBits.Serialization.Tests
{
	using NUnit.Framework;
	using Rom;

	[TestFixture]
	public class RdlTests
	{
		[TestCase(Format.Xml, Result = "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\"><Body /></Report>")]
#if FULL
		[TestCase(Format.Json, Result = "{\"Body\":{}}")]
		[TestCase(Format.JsonML, Result = "[\"Report\",{\"xmlns\":\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\"},[\"Body\"]]")]
#endif
		public string WriteDefaultReport(Format format)
		{
			var report = new Report();
			return Rdl.Schema.ToString(report, format);
		}

		[TestCase(Format.Xml,
			"<Report Name=\"report\" xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\"><Width>12in</Width><Body><ReportItems><TextBox Name=\"textbox1\"><DataElementOutput>NoContent</DataElementOutput><Value>hello</Value></TextBox><Rectangle><ReportItems><TextBox Name=\"textbox2\"><Value>world</Value></TextBox></ReportItems></Rectangle></ReportItems></Body></Report>"
			)]
#if FULL
		[TestCase(Format.Json,
			"{\"Name\":\"report\",\"Width\":\"12in\",\"Body\":{\"ReportItems\":[new TextBox({\"Name\":\"textbox1\",\"DataElementOutput\":\"NoContent\",\"Value\":\"hello\"}),new Rectangle({\"ReportItems\":[new TextBox({\"Name\":\"textbox2\",\"Value\":\"world\"})]})]}}"
			)]
		[TestCase(Format.JsonML,
			"[\"Report\",{\"xmlns\":\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\",\"Name\":\"report\"},[\"Width\",\"12in\"],[\"Body\",[\"ReportItems\",[\"TextBox\",{\"Name\":\"textbox1\"},[\"DataElementOutput\",\"NoContent\"],[\"Value\",\"hello\"]],[\"Rectangle\",[\"ReportItems\",[\"TextBox\",{\"Name\":\"textbox2\"},[\"Value\",\"world\"]]]]]]]"
			)]
#endif
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

			var reportString = Rdl.Schema.ToString(report, format);
			Assert.AreEqual(expectedString, reportString);

			var report2 = Rdl.Schema.Parse<Report>(reportString, format);

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

		[TestCase(@"<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2003/10/reportdefinition""><Body><ReportItems><TextBox><Value>test</Value></TextBox></ReportItems></Body></Report>")]
		[TestCase(@"<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""><Body><ReportItems><TextBox><Value>test</Value></TextBox></ReportItems></Body></Report>")]
		public void ReadSimpleReport(string xml)
		{
			var report = Rdl.Schema.Parse<Report>(xml, Format.Xml);
			Assert.AreEqual(1, report.Body.ReportItems.Count);
			var textbox = report.Body.ReportItems[0] as TextBox;
			Assert.IsNotNull(textbox);
			Assert.AreEqual("test", textbox.Value);
		}
	}
}
#endif