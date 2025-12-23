namespace BTChat;

public record User(string Nickname);

/// <summary>
/// Manages the user's session, including login, logout, and session state.
/// </summary>
public class SessionManager
{
    private readonly StorageService _storageService;
    public User? CurrentUser { get; private set; }

    public SessionManager(StorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Per PRD: Anonymous/basic account registration using nickname.
    /// </summary>
    public void Login(string nickname)
    {
        CurrentUser = new User(nickname);
    }

    /// <summary>
    /// Per PRD: User logs out or app closes — session and stored data deleted instantly.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _storageService.PurgeAllDataAsync();
        CurrentUser = null;
    }

    public User? GetCurrentUser() => CurrentUser;
}