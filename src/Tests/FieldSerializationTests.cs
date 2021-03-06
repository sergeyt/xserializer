﻿#if NUNIT
using System;
using System.Reflection;
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture(Format.Xml)]
#if FULL
	[TestFixture(Format.Json)]
	[TestFixture(Format.JsonML)]
#endif
	public class FieldSerializationTests
	{
		private readonly Format _format;

		public FieldSerializationTests(Format format)
		{
			_format = format;
		}

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
			var method = type.GetMethod("TestPointT", BindingFlags.NonPublic | BindingFlags.Instance);
			method = method.MakeGenericMethod(x.GetType());
			method.Invoke(this, new [] {asAttrs, x, y});
		}

		private void TestPointT<T>(bool asAttrs, T x, T y)
		{
			var schema = new Scope();

			var def = schema.Element<Point<T>>().Init();
			(asAttrs ? def.Attributes() : def.Elements())
				.Add(p => p.X)
				.Add(p => p.Y)
				.End();

			var pt = new Point<T>(x, y);
			var serial = schema.ToString(pt, _format);
			var pt2 = schema.Parse<Point<T>>(serial, _format);

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
			var schema = new Scope();

			var def = schema.Element<CPoint>();
			(asAttrs ? def.Attributes() : def.Elements())
				.Add(p => p.X)
				.Add(p => p.Y);

			var pt = new CPoint(x, y);
			var serial = schema.ToString(pt, _format);
			var pt2 = schema.Parse<CPoint>(serial, _format);

			Assert.AreEqual(pt, pt2);
		}

		private struct Point<T> : IEquatable<Point<T>>
		{
			[Arg(0)]
			public readonly T X;

			[Arg(1)]
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
			[Arg(0)]
			public float X;

			[Arg(1)]
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