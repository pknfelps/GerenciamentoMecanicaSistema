using GerenciamentoMecanica.Auth.Contracts;

namespace Auth.Contracts.Tests;

public class CnpjRulesTests
{
    [TestCase("10359666000194")]
    [TestCase("10.359.666/0001-94")]
    public void NormalizesValidCnpjAndIsIdempotent(string input)
    {
        const string expected = "10.359.666/0001-94";
        Assert.That(CnpjRules.Normalize(input), Is.EqualTo(expected));
        Assert.That(CnpjRules.Normalize(expected), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("10 359666000194")]
    [TestCase("1A359666000194")]
    [TestCase("1035966600019")]
    [TestCase("103596660001944")]
    [TestCase("10359666000184")]
    [TestCase("10359666000195")]
    [TestCase("529.982.247-25")]
    public void RejectsInvalidCnpjOrOtherDocumentType(string? input) =>
        Assert.Throws<FormatException>(() => CnpjRules.Normalize(input));

    [TestCase("00000000000000", "00.000.000/0000-00")]
    [TestCase("10/359/666/0001/94", "10.359.666/0001-94")]
    public void PreservesLegacyDigitRules(string input, string expected) =>
        Assert.That(CnpjRules.Normalize(input), Is.EqualTo(expected));
}
