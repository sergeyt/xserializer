using System;
using Newtonsoft.Json;

namespace TsvBits.Serialization.Json
{
	internal static class JsonExtensions
	{
		public static void MustRead(this JsonReader reader)
		{
			if (!reader.Read())
				throw new InvalidOperationException();
		}

		public static bool MoveTo(this JsonReader reader, params JsonToken[] tokens)
		{
			do
			{
				if (Array.IndexOf(tokens, reader.TokenType) >= 0)
					return true;
			} while (reader.Read());
			return false;
		}
	}
}