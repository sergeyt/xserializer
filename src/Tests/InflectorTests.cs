using TsvBits.Serialization.Utils;
#if NUNIT
using NUnit.Framework;

namespace TsvBits.Serialization.Tests
{
	[TestFixture]
	public class InflectorTests
	{
		[TestCase(null, null)]
		[TestCase("", "")]
		[TestCase("item", "items")]
		[TestCase("kiss", "kisses")]
		[TestCase("phase", "phases")]
		[TestCase("dish", "dishes")]
		[TestCase("lap", "laps")]
		[TestCase("cat", "cats")]
		[TestCase("boy", "boys")]
		[TestCase("hero", "heroes")]
		[TestCase("patato", "patatoes")]
		[TestCase("canto", "cantos")]
		[TestCase("hetero", "heteros")]
		[TestCase("photo", "photos")]
		[TestCase("zero", "zeros")]
		[TestCase("piano", "pianos")]
		[TestCase("portico", "porticos")]
		[TestCase("pro", "pros")]
		[TestCase("quarto", "quartos")]
		[TestCase("kimono", "kimonos")]
		[TestCase("cherry", "cherries")]
		[TestCase("lady", "ladies")]
		[TestCase("day", "days")]
		[TestCase("monkey", "monkeys")]
		[TestCase("bath", "baths")]
		[TestCase("mouth", "mouths")]
		[TestCase("calf", "calves")]
		[TestCase("leaf", "leaves")]
		[TestCase("knife", "knives")]
		[TestCase("life", "lives")]
		[TestCase("house", "houses")]
		[TestCase("moth", "moths")]
		[TestCase("proof", "proofs")]
		[TestCase("dwarf", "dwarves")]
		[TestCase("hoof", "hoofs")]
		[TestCase("elf", "elves")]
		[TestCase("roof", "roofs")]
		[TestCase("staff", "staffs")]
		[TestCase("turf", "turves")]
		// irregular
		[TestCase("bison", "bison")]
		[TestCase("buffalo", "buffalo")]
		[TestCase("deer", "deer")]
		[TestCase("fish", "fish")]
		[TestCase("moose", "moose")]
		[TestCase("pike", "pike")]
		[TestCase("sheep", "sheep")]
		[TestCase("salmon", "salmon")]
		[TestCase("trout", "trout")]
		[TestCase("swine", "swine")]
		[TestCase("plankton", "plankton")]
		[TestCase("squid", "squid")]
		// -(e)n
		[TestCase("ox", "oxen")]
		[TestCase("child", "children")]
		[TestCase("brother", "brothers")]
		// apophonic plurals
		[TestCase("foot", "feet")]
		[TestCase("goose", "geese")]
		[TestCase("louse", "lice")]
		[TestCase("man", "men")]
		[TestCase("mouse", "mice")]
		[TestCase("tooth", "teeth")]
		[TestCase("woman", "women")]
		// misc irregulars
		[TestCase("person", "people")]
		[TestCase("die", "dice")]
		[TestCase("penny", "pence")]
		public void Test(string singular, string plural)
		{
			Assert.AreEqual(plural, singular.ToPlural());
			Assert.AreEqual(singular, plural.ToSingular());
			Assert.AreEqual(plural, plural.ToPlural());
			Assert.AreEqual(singular, singular.ToSingular());
		}
	}
}
#endif