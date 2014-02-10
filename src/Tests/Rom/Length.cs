using System.Globalization;

namespace TsvBits.Serialization.Tests.Rom
{
	internal struct Length
	{
		public readonly float Value;
		public readonly string Unit;

		public Length(float value, string unit)
		{
			Value = value;
			Unit = unit;
		}

		public bool IsValid
		{
			get { return !string.IsNullOrEmpty(Unit); }
		}

		public static implicit operator Length(string s)
		{
			return Parse(s);
		}

		public static Length Parse(string s)
		{
			if (string.IsNullOrEmpty(s) || s.Length <= 2)
				return default(Length);

			float value;
			if (!float.TryParse(s.Substring(0, s.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				return default(Length);

			var unit = s.Substring(s.Length - 2);
			return new Length(value, unit);
		}

		public override string ToString()
		{
			return IsValid ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", Value, Unit) : "invalid";
		}
	}
}