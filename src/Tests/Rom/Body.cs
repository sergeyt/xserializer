namespace TsvBits.Serialization.Tests.Rom
{
	internal class Body
	{
		public Body()
		{
			ReportItems = new ReportItemCollection();
		}

		public Length Height { get; set; }

		public ReportItemCollection ReportItems { get; private set; }
	}
}