#if NUNIT
using System;
using System.Reflection;
using NUnit.Framework;

namespace TsvBits.XmlSerialization.Tests
{
	[TestFixture]
	public class FieldSerializationTests
	{
		[TestCase(true, 0f, 0f)]
		[TestCase(true, 1.2f, 2.3f)]
		[TestCase(true, -1.2f, -2.3f)]
		[TestCase(false, 0f, 0f)]
		[TestCase(false, 1.2f, 2.3f)]
		[TestCase(false, -1.2f, -2.3f)]
		[TestCase(true, 0, 1)]
		[TestCase(true, 0, -1)]
		[TestCase(false, 0, 1)]
		[TestCase(false, 0, -1)]
		[TestCase(true, "a", "b")]
		[TestCase(false, "a", "b")]
		public void TestPoint(bool asAttrs, object x, object y)
		{
			var type = typeof(FieldSerializationTests);
			var method = type.GetMethod("TestPointT", BindingFlags.NonPublic | BindingFlags.Static);
			method = method.MakeGenericMethod(x.GetType());
			method.Invoke(null, new [] {asAttrs, x, y});
		}

		private static void TestPointT<T>(bool asAttrs, T x, T y)
		{
			var scope = Scope.New("");

			var def = scope.Elem<Point<T>>()
			               .Init<T, T>((x1, y1) => new Point<T>(x1, y1));
			if (asAttrs)
			{
				def.Attr(p => p.X, 0)
				   .Attr(p => p.Y, 1);
			}
			else
			{
				def.Elem(p => p.X, 0)
				   .Elem(p => p.Y, 1);
			}

			var serializer = XSerializer.New(scope);

			var pt = new Point<T>(x, y);
			var xml = serializer.ToXmlString(pt, true);
			var pt2 = serializer.Parse<Point<T>>(xml);

			Assert.AreEqual(pt, pt2);
		}

		[TestCase(true, 0, 0)]
		[TestCase(true, 1, 2)]
		[TestCase(true, -1, -2)]
		[TestCase(false, 0, 0)]
		[TestCase(false, 1, 2)]
		[TestCase(false, -1, -2)]
		public void TestClass(bool asAttrs, float x, float y)
		{
			var scope = Scope.New("");

			var def = scope.Elem<CPoint>();
			if (asAttrs)
			{
				def.Attr(p => p.X)
				   .Attr(p => p.Y);
			}
			else
			{
				def.Elem(p => p.X)
				   .Elem(p => p.Y);
			}

			var serializer = XSerializer.New(scope);

			var pt = new CPoint(x, y);
			var xml = serializer.ToXmlString(pt, true);
			var pt2 = serializer.Parse<CPoint>(xml);

			Assert.AreEqual(pt, pt2);
		}

		private struct Point<T> : IEquatable<Point<T>>
		{
			public readonly T X;
			public readonly T Y;

			public Point(T x, T y)
			{
				X = x;
				Y = y;
			}

			public bool Equals(Point<T> other)
			{
				return X.Equals(other.X) && Y.Equals(other.Y);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is Point<T> && Equals((Point<T>)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (X.GetHashCode() * 397) ^ Y.GetHashCode();
				}
			}
		}

		private class CPoint : IEquatable<CPoint>
		{
			public float X;
			public float Y;

			public CPoint()
			{
			}

			public CPoint(float x, float y)
			{
				X = x;
				Y = y;
			}

			public bool Equals(CPoint other)
			{
				return X.Equals(other.X) && Y.Equals(other.Y);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is CPoint && Equals((CPoint)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (X.GetHashCode() * 397) ^ Y.GetHashCode();
				}
			}
		}
	}
}
#endif