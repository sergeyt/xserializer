#if NUNIT
using System;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class ReflectorTests
	{
		[Test]
		public void IsNullable()
		{
			Assert.IsTrue(typeof(int?).IsNullable());
			Assert.IsFalse(typeof(int).IsNullable());
			Assert.IsTrue(typeof(StringComparison?).IsNullable());
			Assert.IsFalse(typeof(StringComparison).IsNullable());
		}
	}
}
#endif