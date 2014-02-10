using System.Collections.Generic;

namespace TsvBits.Serialization.Tests.Rom
{
	internal abstract class ReportItem
	{
		public string Name { get; set; }
		public string DataElementName { get; set; }
		public DataElementOutput DataElementOutput { get; set; }
	}

	internal class ReportItemCollection : List<ReportItem>
	{
	}

	internal enum DataElementOutput
	{
		Auto,
		NoContent
	}
}