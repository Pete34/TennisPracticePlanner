using Microsoft.JSInterop;
using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

public class AuthService : IAuthService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AuthService>? _dotNetRef;
    private bool _isInitialized;
    private readonly TaskCompletionSource _initialCheckTcs = new();

    public AppUser? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public bool? IsAllowed { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public Task InitialCheckCompleteAsync => _initialCheckTcs.Task;

    public event Action? AuthStateChanged;

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/firebase-interop.js");
        _dotNetRef = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("subscribeToAuthState", _dotNetRef);

        _isInitialized = true;
    }

    public async Task SignInWithGoogleAsync()
    {
        await InitializeAsync();
        LastErrorMessage = null;

        try
        {
            // OnAuthStateChanged fires and updates CurrentUser/IsAllowed once sign-in completes.
            await _module!.InvokeVoidAsync("signInWithGoogle");
        }
        catch (JSException ex)
        {
            LastErrorMessage = DescribeFirebaseError(ex.Message);
            AuthStateChanged?.Invoke();
        }
    }

    public async Task SignOutAsync()
    {
        await InitializeAsync();
        LastErrorMessage = null;

        try
        {
            await _module!.InvokeVoidAsync("signOutUser");
        }
        catch (JSException ex)
        {
            LastErrorMessage = DescribeFirebaseError(ex.Message);
            AuthStateChanged?.Invoke();
        }
    }

    private static string DescribeFirebaseError(string rawMessage)
    {
        if (rawMessage.Contains("configuration-not-found"))
        {
            return "Google sign-in is not enabled yet for this Firebase project.";
        }

        if (rawMessage.Contains("popup-closed-by-user") || rawMessage.Contains("cancelled-popup-request"))
        {
            return "Sign-in was cancelled.";
        }

        return "Sign-in failed. Please try again.";
    }

    [JSInvokable]
    public async Task OnAuthStateChanged(AppUser? user)
    {
        CurrentUser = user;
        IsAllowed = null;

        if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
        {
            IsAllowed = await _module!.InvokeAsync<bool>("isEmailAllowed", user.Email);
        }

        _initialCheckTcs.TrySetResult();
        AuthStateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
