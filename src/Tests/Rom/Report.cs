#if NUNIT
namespace TsvBits.Serialization.Tests.Rom
{
	internal class Report
	{
		private readonly Body _body = new Body();

		public string Name { get; set; }
		public Length Width { get; set; }

		public Body Body
		{
			get { return _body; }
		}
	}
}
#endif