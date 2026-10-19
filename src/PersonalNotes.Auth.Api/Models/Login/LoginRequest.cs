namespace PersonalNotes.Auth.Api.Models.Login
{
    public record LoginRequest(
        string Username,
        string Password
    );
}
