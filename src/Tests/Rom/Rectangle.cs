namespace TsvBits.Serialization.Tests.Rom
{
	internal class Rectangle : ReportItem
	{
		public Rectangle()
		{
			ReportItems = new ReportItemCollection();
		}

		public ReportItemCollection ReportItems { get; private set; }
	}
}