using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TsvBits.Serialization.Utils
{
	/// <summary>
	/// Performs inflextions.
	/// </summary>
	internal static class Inflector
	{
		private static readonly List<Rule> PluralRules = new List<Rule>();
		private static readonly List<Rule> SingularRules = new List<Rule>();
		private static readonly HashSet<string> Plurals = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
		private static readonly HashSet<string> Singulars = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		private static readonly HashSet<string> Uncountables = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
			{
				"equipment",
				"information",
				"rice",
				"money",
				"species",
				"series",
				"fish",
				"sheep",
				"bison",
				"buffalo",
				"deer",
				"moose",
				"pike",
				"salmon",
				"trout",
				"swine",
				"plankton",
				"squid"
			};

		static Inflector()
		{
			AddPluralRule("$", "s");
			AddPluralRule("s$", "s");
			AddPluralRule("(ax|test)is$", "$1es");
			AddPluralRule("(octop|vir)us$", "$1i");
			AddPluralRule("(alias|status)$", "$1es");
			AddPluralRule("(bu)s$", "$1ses");
			AddPluralRule("(\\w+)o$", "$1oes");
			AddPluralRule("(cant|heter|kimon|phot|pian|portic|pr|quart|zer)o$", "$1os");
			AddPluralRule("([ti])um$", "$1a");
			AddPluralRule("sis$", "ses");
			AddPluralRule("(?:([^f])fe|([lr])f)$", "$1$2ves");
			AddPluralRule("(lea)f$", "$1ves");
			AddPluralRule("(hive)$", "$1s");
			AddPluralRule("([^aeiouy]|qu)y$", "$1ies");
			AddPluralRule("(x|ch|ss|sh)$", "$1es");
			AddPluralRule("(matr|vert|ind)ix|ex$", "$1ices");
			AddPluralRule("(quiz)$", "$1zes");

			AddSingularRule("s$", String.Empty);
			AddSingularRule("ss$", "ss");
			AddSingularRule("(n)ews$", "$1ews");
			AddSingularRule("([ti])a$", "$1um");
			AddSingularRule("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
			AddSingularRule("(^analy)ses$", "$1sis");
			AddSingularRule("([^f])ves$", "$1fe");
			AddSingularRule("(lea)ves$", "$1f");
			AddSingularRule("(hive)s$", "$1");
			AddSingularRule("(tive)s$", "$1");
			AddSingularRule("([lr])ves$", "$1f");
			AddSingularRule("([^aeiouy]|qu)ies$", "$1y");
			AddSingularRule("(s)eries$", "$1eries");
			AddSingularRule("(m)ovies$", "$1ovie");
			AddSingularRule("(x|ch|ss|sh)es$", "$1");
			AddSingularRule("(bus)es$", "$1");
			AddSingularRule("(o)es$", "$1");
			AddSingularRule("(shoe)s$", "$1");
			AddSingularRule("(cris|ax|test)es$", "$1is");
			AddSingularRule("(octop|vir)i$", "$1us");
			AddSingularRule("(alias|status)$", "$1");
			AddSingularRule("(alias|status)es$", "$1");
			AddSingularRule("(vert|ind)ices$", "$1ex");
			AddSingularRule("(matr)ices$", "$1ix");
			AddSingularRule("(quiz)zes$", "$1");

			AddIrregularRule("person", "people");
			AddIrregularRule("man", "men");
			AddIrregularRule("woman", "women");
			AddIrregularRule("child", "children");
			AddIrregularRule("sex", "sexes");
			AddIrregularRule("tax", "taxes");
			AddIrregularRule("move", "moves");
			AddIrregularRule("foot", "feet");
			AddIrregularRule("goose", "geese");
			AddIrregularRule("penny", "pence");
			AddIrregularRule("tooth", "teeth");
			AddIrregularRule("die", "dice");
			AddIrregularRule("ox", "oxen");
			AddIrregularRule("louse", "lice");
			AddIrregularRule("mouse", "mice");
		}

		private static void AddIrregularRule(string singular, string plural)
		{
			Singulars.Add(singular);
			Plurals.Add(plural);

			AddPluralRule(string.Concat("(", singular[0], ")", singular.Substring(1), "$"),
			              string.Concat("$1", plural.Substring(1)));
			AddSingularRule(string.Concat("(", plural[0], ")", plural.Substring(1), "$"),
			                string.Concat("$1", singular.Substring(1)));
		}

		private static void AddPluralRule(string rule, string replacement)
		{
			PluralRules.Add(new Rule(rule, replacement));
		}

		private static void AddSingularRule(string rule, string replacement)
		{
			SingularRules.Add(new Rule(rule, replacement));
		}

		/// <summary>
		/// Makes the plural.
		/// </summary>
		/// <param name="word">The word to pluralize.</param>
		public static string ToPlural(this string word)
		{
			if (string.IsNullOrEmpty(word)) return word;
			if (Plurals.Contains(word)) return word;
			return ApplyRules(PluralRules, word);
		}

		/// <summary>
		/// Makes the singular.
		/// </summary>
		/// <param name="word">The word to singularize.</param>
		public static string ToSingular(this string word)
		{
			if (string.IsNullOrEmpty(word)) return word;
			if (Singulars.Contains(word)) return word;
			return ApplyRules(SingularRules, word);
		}

		private static string ApplyRules(IList<Rule> rules, string word)
		{
			string result = word;
			if (!Uncountables.Contains(word))
			{
				for (int i = rules.Count - 1; i >= 0; i--)
				{
					string inflected = rules[i].Apply(word);
					if (inflected != null)
					{
						result = inflected;
						break;
					}
				}
			}
			return result;
		}

		private sealed class Rule
		{
			private readonly Regex _regex;
			private readonly string _replacement;

			public Rule(string regexPattern, string replacement)
			{
				_regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
				_replacement = replacement;
			}

			public string Apply(string word)
			{
				if (!_regex.IsMatch(word))
					return null;

				string replace = _regex.Replace(word, _replacement);
				if (word == word.ToUpper())
					replace = replace.ToUpper();

				return replace;
			}
		}
	}
}