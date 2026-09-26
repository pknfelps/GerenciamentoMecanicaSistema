using GerenciamentoMecanica.Auth.Contracts;

namespace Auth.Contracts.Tests;

public class DocumentRulesTests
{
    [TestCase("52998224725", DocumentType.Cpf, "529.982.247-25")]
    [TestCase("529.982.247-25", DocumentType.Cpf, "529.982.247-25")]
    [TestCase("10359666000194", DocumentType.Cnpj, "10.359.666/0001-94")]
    [TestCase("10.359.666/0001-94", DocumentType.Cnpj, "10.359.666/0001-94")]
    public void IdentifiesAndNormalizesCustomerDocument(string input, DocumentType type, string expected)
    {
        var document = DocumentRules.Parse(input);
        Assert.That(document.Type, Is.EqualTo(type));
        Assert.That(document.Value, Is.EqualTo(expected));
        Assert.That(DocumentRules.Normalize(expected), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1234")]
    [TestCase("52998224724")]
    [TestCase("10359666000184")]
    [TestCase("10359666000195")]
    [TestCase("x52998224725")]
    [TestCase("x10359666000194")]
    [TestCase("529 98224725")]
    [TestCase("10359666 000194")]
    public void RejectsInvalidDocumentBeforeItCanBeUsedForLookup(string? input) =>
        Assert.Throws<FormatException>(() => DocumentRules.Normalize(input));
}
