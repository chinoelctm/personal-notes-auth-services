namespace PersonalNotes.Auth.Infrastructure.Options
{
    /// <summary>
    /// Usuario que se crea al arrancar si todavía no existe. Solo se configura el hash BCrypt de la contraseña
    /// (user-secrets o variable SeedUser__PasswordHash), nunca la contraseña en texto plano.
    /// </summary>
    public class SeedUserOptions
    {
        public const string SectionName = "SeedUser";

        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
