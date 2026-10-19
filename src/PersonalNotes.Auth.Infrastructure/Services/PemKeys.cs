using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace PersonalNotes.Auth.Infrastructure.Services
{
    /// <summary>
    /// Lee claves RSA en PEM tal como llegan de la configuración. Los dashboards (Render) y los .env suelen
    /// cambiar los saltos de línea por espacios o por "\n" literales, o agregar comillas; aquí se reconstruye
    /// el PEM antes de importarlo. También acepta solo el Base64 del cuerpo, sin las líneas BEGIN/END.
    /// </summary>
    public static partial class PemKeys
    {
        [GeneratedRegex(@"-----BEGIN ([A-Z ]+)-----(.*?)-----END \1-----", RegexOptions.Singleline)]
        private static partial Regex PemBlock();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();

        public static RSA ImportRsa(string? configured, bool requirePrivateKey)
        {
            if (!TryImportRsa(configured, requirePrivateKey, out var rsa))
            {
                throw new InvalidOperationException(requirePrivateKey
                    ? "La clave privada JWT no es una clave RSA privada válida en PEM (-----BEGIN PRIVATE KEY----- ... -----END PRIVATE KEY-----)."
                    : "La clave pública JWT no es una clave RSA válida en PEM (-----BEGIN PUBLIC KEY----- ... -----END PUBLIC KEY-----).");
            }
            return rsa;
        }

        public static bool TryImportRsa(string? configured, bool requirePrivateKey, out RSA rsa)
        {
            var key = RSA.Create();
            rsa = key;
            if (string.IsNullOrWhiteSpace(configured))
                return false;

            var text = configured.Trim().Trim('"', '\'')
                .Replace("\\r", string.Empty)
                .Replace("\\n", "\n");

            var match = PemBlock().Match(text);
            var body = Whitespace().Replace(match.Success ? match.Groups[2].Value : text, string.Empty);

            byte[] der;
            try
            {
                der = Convert.FromBase64String(body);
            }
            catch (FormatException)
            {
                return false;
            }

            var label = match.Success ? match.Groups[1].Value : null;
            var importers = new List<(string Label, Action<byte[]> Import, bool IsPrivate)>
            {
                ("PRIVATE KEY", b => key.ImportPkcs8PrivateKey(b, out _), true),
                ("RSA PRIVATE KEY", b => key.ImportRSAPrivateKey(b, out _), true),
                ("PUBLIC KEY", b => key.ImportSubjectPublicKeyInfo(b, out _), false),
                ("RSA PUBLIC KEY", b => key.ImportRSAPublicKey(b, out _), false)
            };

            foreach (var (importerLabel, import, isPrivate) in importers)
            {
                if (label is not null && label != importerLabel) continue;
                if (requirePrivateKey && !isPrivate) continue;

                try
                {
                    import(der);
                    return true;
                }
                catch (CryptographicException)
                {
                    // Sin etiqueta se prueba el siguiente formato
                }
            }

            return false;
        }
    }
}
