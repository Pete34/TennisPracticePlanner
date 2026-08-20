using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

public interface IAuthService : IAsyncDisposable
{
    AppUser? CurrentUser { get; }

    bool IsSignedIn { get; }

    bool? IsAllowed { get; }

    string? LastErrorMessage { get; }

    /// <summary>Completes once the initial Firebase session restore + allow-list check has finished.</summary>
    Task InitialCheckCompleteAsync { get; }

    event Action? AuthStateChanged;

    Task InitializeAsync();

    Task SignInWithGoogleAsync();

    Task SignOutAsync();
}
