using System.Collections.Generic;
using TsvBits.Serialization.Utils;
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

		[TestCase(typeof(int[]), Result = true)]
		[TestCase(typeof(IList<int>), Result = true)]
		[TestCase(typeof(object), Result = false)]
		public bool FindIEnumerableT(Type type)
		{
			return Reflector.FindIEnumerableT(type) != null;
		}
	}
}
#endif