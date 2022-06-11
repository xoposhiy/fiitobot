using fiitobot;
using NUnit.Framework;

namespace tests;

public class PluralizeTests
{
    [TestCase(0, "0 пней")]
    [TestCase(-1, "-1 пней")]
    [TestCase(1, "1 пень")]
    [TestCase(2, "2 пня")]
    [TestCase(3, "3 пня")]
    [TestCase(4, "4 пня")]
    [TestCase(5, "5 пней")]
    [TestCase(9, "9 пней")]
    [TestCase(10, "10 пней")]
    [TestCase(11, "11 пней")]
    [TestCase(111, "111 пней")]
    [TestCase(119, "119 пней")]
    [TestCase(112, "112 пней")]
    [TestCase(20, "20 пней")]
    [TestCase(21, "21 пень")]
    [TestCase(121, "121 пень")]
    [TestCase(122, "122 пня")]
    [TestCase(125, "125 пней")]
    public void Pluralize(int count, string expected)
    {
        Assert.That(count.Pluralize("пень|пня|пней"), Is.EqualTo(expected));
    }
}