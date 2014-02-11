#if NUNIT
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class BlobTests
	{
		[TestCase(Format.Xml, (byte[]) null, "<Item />")]
		[TestCase(Format.Xml, new byte[] { 1, 2, 3 }, "<Item><Data>AQID</Data></Item>")]
		[TestCase(Format.Json, (byte[])null, "{}")]
		[TestCase(Format.Json, new byte[] { 1, 2, 3 }, "{\"Data\":\"AQID\"}")]
		[TestCase(Format.JsonML, (byte[])null, "[\"Item\"]")]
		[TestCase(Format.JsonML, new byte[] { 1, 2, 3 }, "[\"Item\",[\"Data\",\"AQID\"]]")]
		public void Simple(Format format, byte[] data, string expectedSerial)
		{
			var schema = new Scope();
			schema.Element<Item>()
				.Elements().Add(x => x.Data);

			var serializer = XSerializer.New(schema);

			var item = new Item {Data = data};
			var serial = serializer.ToString(item, format);
			Assert.AreEqual(expectedSerial, serial);

			var item2 = serializer.Parse<Item>(serial, format);
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