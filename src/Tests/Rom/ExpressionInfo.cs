namespace TsvBits.Serialization.Tests.Rom
{
	internal class ExpressionInfo
	{
		private readonly string _expression;

		private ExpressionInfo(string expression)
		{
			_expression = expression;
		}

		public static ExpressionInfo Parse(string s)
		{
			return new ExpressionInfo(s);
		}

		public override string ToString()
		{
			return _expression;
		}
	}
}