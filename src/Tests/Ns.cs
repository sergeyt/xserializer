#if NUNIT
using System.Xml.Linq;

namespace TsvBits.Serialization.Tests
{
	internal static class Ns
	{
		public static readonly XNamespace Rdl2003 =
			"http://schemas.microsoft.com/sqlserver/reporting/2003/10/reportdefinition";

		public static readonly XNamespace Rdl2005 =
			"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

		public static readonly XNamespace Rdl2008 =
			"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

		public static readonly XNamespace Rdl2010 =
			"http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

		public static readonly XNamespace Dd =
			"http://schemas.datadynamics.com/reporting/2005/02/reportdefinition";

		public static readonly XNamespace Snippet =
			"http://schemas.datadynamics.com/reporting/2005/02/snippet";
	}
}
#endif
