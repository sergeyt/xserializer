#if NUNIT
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class BlobTests
	{
		[TestCase(Format.Xml, (byte[]) null, "<Item />")]
		[TestCase(Format.Xml, new byte[] { 1, 2, 3 }, "<Item><Data>AQID</Data></Item>")]
#if FULL
		[TestCase(Format.Json, (byte[])null, "{}")]
		[TestCase(Format.Json, new byte[] { 1, 2, 3 }, "{\"Data\":\"AQID\"}")]
		[TestCase(Format.JsonML, (byte[])null, "[\"Item\"]")]
		[TestCase(Format.JsonML, new byte[] { 1, 2, 3 }, "[\"Item\",[\"Data\",\"AQID\"]]")]
#endif
		public void Simple(Format format, byte[] data, string expectedSerial)
		{
			var schema = new Scope();
			schema.Element<Item>()
				.Elements().Add(x => x.Data);

			var item = new Item {Data = data};
			var serial = schema.ToString(item, format);
			Assert.AreEqual(expectedSerial, serial);

			var item2 = schema.Parse<Item>(serial, format);
			Assert.NotNull(item2);
			Assert.AreEqual(item.Data, item2.Data);
		}

		private sealed class Item
		{
			public byte[] Data { get; set; }
		}
	}
}
#endif