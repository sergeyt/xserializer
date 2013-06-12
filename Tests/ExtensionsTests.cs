#if NUNIT
using System;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class ExtensionsTests
	{
		[Test]
		public void TestUnboxNullable()
		{
			object nil = null;
			Assert.IsNull(nil.UnboxNullable());
			Assert.AreEqual("", "".UnboxNullable());

			TestUnboxNullable(false);
			TestUnboxNullable(true);
			TestUnboxNullable('a');
			TestUnboxNullable<sbyte>(123);
			TestUnboxNullable<byte>(123);
			TestUnboxNullable<short>(123);
			TestUnboxNullable<ushort>(123);
			TestUnboxNullable(123);
			TestUnboxNullable(123u);
			TestUnboxNullable(123l);
			TestUnboxNullable(123ul);
			TestUnboxNullable(3.14f);
			TestUnboxNullable(3.14);
			TestUnboxNullable(DateTime.Now);
			TestUnboxNullable(StringComparison.Ordinal);
			TestUnboxNullable(new MyStruct {Value = 123});
		}

		private static void TestUnboxNullable<T>(T value) where T:struct
		{
			T? wrap = value;
			var unboxed = ((object)wrap).UnboxNullable();
			Assert.AreEqual(value, unboxed);
			Assert.AreEqual(typeof(T), unboxed.GetType());
		}

		struct MyStruct
		{
			public int Value;
		}
	}
}
#endif