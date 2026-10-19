using System.Security.Cryptography;
using PersonalNotes.Auth.Infrastructure.Services;

namespace PersonalNotes.Auth.Tests.Infrastructure
{
    public class PemKeysTests
    {
        private static readonly RSA Key = RSA.Create(2048);
        private static readonly string PrivatePem = Key.ExportPkcs8PrivateKeyPem();

        public static TheoryData<string> PrivateKeyAsPasted => new()
        {
            PrivatePem,
            PrivatePem.Replace("\n", "\\n"),                 // .env: "\n" literales
            PrivatePem.Replace("\n", " "),                   // dashboard de una línea: saltos → espacios
            PrivatePem.Replace("\n", "\r\n") + "\r\n",       // pegado desde Windows
            "\"" + PrivatePem.Replace("\n", "\\n") + "\"",   // con comillas
            "  " + PrivatePem + "  ",
            string.Concat(PrivatePem.Split('\n').Skip(1).SkipLast(1)), // solo el Base64, sin BEGIN/END
            Key.ExportRSAPrivateKeyPem()                     // formato PKCS#1 (BEGIN RSA PRIVATE KEY)
        };

        [Theory]
        [MemberData(nameof(PrivateKeyAsPasted))]
        public void ImportsThePrivateKey_HoweverTheDashboardMangledIt(string configured)
        {
            Assert.True(PemKeys.TryImportRsa(configured, requirePrivateKey: true, out var rsa));
            Assert.Equal(Key.ExportParameters(false).Modulus, rsa.ExportParameters(false).Modulus);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("jwt-private.pem")]
        [InlineData("-----BEGIN PRIVATE KEY----- no es base64 -----END PRIVATE KEY-----")]
        public void RejectsMissingOrInvalidKeys(string configured)
        {
            Assert.False(PemKeys.TryImportRsa(configured, requirePrivateKey: true, out _));
        }

        [Fact]
        public void APublicKey_IsRejectedWhereAPrivateOneIsRequired_AndAcceptedOtherwise()
        {
            var publicPem = Key.ExportSubjectPublicKeyInfoPem();

            Assert.False(PemKeys.TryImportRsa(publicPem, requirePrivateKey: true, out _));
            Assert.True(PemKeys.TryImportRsa(publicPem.Replace("\n", " "), requirePrivateKey: false, out _));
        }

        [Fact]
        public void ImportRsa_ThrowsAMessageWithoutTheKey()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => PemKeys.ImportRsa("secreto-mal-pegado", true));

            Assert.DoesNotContain("secreto-mal-pegado", ex.Message);
        }
    }
}
