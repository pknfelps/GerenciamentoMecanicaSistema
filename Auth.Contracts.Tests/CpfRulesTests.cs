using GerenciamentoMecanica.Auth.Contracts;

namespace Auth.Contracts.Tests;

public class CpfRulesTests
{
    [TestCase("66211973063", "662.119.730-63")]
    [TestCase("662.119.730-63", "662.119.730-63")]
    [TestCase("52998224725", "529.982.247-25")]
    [TestCase("529.982.247-25", "529.982.247-25")]
    public void NormalizesValidCpfAndIsIdempotent(string input, string expected)
    {
        Assert.That(CpfRules.Normalize(input), Is.EqualTo(expected));
        Assert.That(CpfRules.Normalize(expected), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("123 456 789 12")]
    [TestCase("5299822472A")]
    [TestCase("5299822472")]
    [TestCase("529982247255")]
    [TestCase("52998224715")]
    [TestCase("52998224724")]
    [TestCase("10.359.666/0001-94")]
    public void RejectsInvalidCpf(string? input) =>
        Assert.Throws<FormatException>(() => CpfRules.Normalize(input));

    // Caracterização do legado, não uma promessa de validação estrita de entrada HTTP.
    [TestCase("00000000000", "000.000.000-00")]
    [TestCase("529/982/247/25", "529.982.247-25")]
    public void PreservesLegacyAcceptanceDuringExtraction(string input, string expected) =>
        Assert.That(CpfRules.Normalize(input), Is.EqualTo(expected));
}
