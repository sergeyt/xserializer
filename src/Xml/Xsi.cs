using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization.Xml
{
	internal static class Xsi
	{
		public const string Uri = "http://www.w3.org/2001/XMLSchema-instance";
		public static readonly XNamespace Namespace = XNamespace.Get(Uri);

		public static readonly IDictionary<string, Type> Name2Type = new Dictionary<string, Type>
			{
				{"boolean", typeof(bool)},
				{"unsignedByte", typeof(byte)},
				{"byte", typeof(sbyte)},
				{"short", typeof(short)},
				{"xsd:unsignedShort", typeof(ushort)},
				{"int", typeof(int)},
				{"unsignedInt", typeof(uint)},
				{"long", typeof(long)},
				{"unsignedLong", typeof(ulong)},
				{"float", typeof(float)},
				{"double", typeof(double)},
				{"decimal", typeof(decimal)},
				{"dateTime", typeof(DateTime)},
				{"string", typeof(string)}
			};

		public static string TypeOf(object value)
		{
			if (value == null) return null;

			switch (Type.GetTypeCode(value.GetType()))
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return null;
				case TypeCode.Object:
					throw new NotSupportedException(String.Format("Unsupported type: {0}", value.GetType()));
				case TypeCode.Boolean:
					return "xsd:boolean";
				case TypeCode.Char:
					return "xsd:unsignedByte";
				case TypeCode.SByte:
					return "xsd:byte";
				case TypeCode.Byte:
					return "xsd:unsignedByte";
				case TypeCode.Int16:
					return "xsd:short";
				case TypeCode.UInt16:
					return "xsd:unsignedShort";
				case TypeCode.Int32:
					return "xsd:int";
				case TypeCode.UInt32:
					return "xsd:unsignedInt";
				case TypeCode.Int64:
					return "xsd:long";
				case TypeCode.UInt64:
					return "xsd:unsignedLong";
				case TypeCode.Single:
					return "xsd:float";
				case TypeCode.Double:
					return "xsd:double";
				case TypeCode.Decimal:
					return "xsd:decimal";
				case TypeCode.DateTime:
					return "xsd:dateTime";
				case TypeCode.String:
					return "xsd:string";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}