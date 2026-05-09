# GithubAuthEndpoints

> **File:** `src/api/Whodat.Api/Endpoints/GithubAuthEndpoints.cs`  
> **Kind:** class

Implements GitHub authentication using the device-code flow. The CLI obtains a device code via /auth/github/start, presents the user_code to the user at github.com/login/device, and then completes the flow with /auth/github/complete to either link an existing WhodatUser or create a new one.

## Remarks
- The endpoints are active only when GithubOptions.IsConfigured is true; otherwise the endpoints respond with a disabled status. 
- Start validates an optional user handle to ensure it is valid and not already taken, preventing a bad user experience if the handle is already registered elsewhere. It uses a named HttpClient ("github") to request a device code from GitHub and returns a StartResponse containing the device_code, user_code, verification_uri, expires_in, and interval.
- If the GitHub device-code request fails, Start returns a 502 with a descriptive error.
- Complete continues the flow by validating prerequisites: device_code must be supplied, the user-provided text must not exceed MaxText, and avatar ASCII data must not exceed MaxAscii. It then exchanges the device_code for an access_token with GitHub and proceeds to link the GitHub identity to a WhodatUser or to create a new user with the supplied handle when appropriate.
- The flow adheres to a register-or-login pattern: if the GitHub identity is already linked to a WhodatUser, the existing session is rotated; otherwise a new WhodatUser is created with the provided handle.
- The implementation relies on dependency-injected services such as IHttpClientFactory, `IOptions<GithubOptions>`, and `UserManager<WhodatUser>`, and uses serialization helpers (JsonSerializer, JsonPropertyName) to parse GitHub responses.

## Example
```csharp
// Client-side pseudo-usage of the device-code flow
// 1) Start the flow to obtain a device code and user code
var startResp = await httpClient.PostAsync("/auth/github/start", null)
                          .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<StartResponse>())
                          .Unwrap();

// Present startResp.UserCode to the user and prompt them to authorize at startResp.VerificationUri

// 2) Complete the flow once the user has authorized on GitHub
var completeReq = new CompleteRequest
{
    DeviceCode = startResp.DeviceCode,
    Text = "User provided handle or notes",
    Handle = "desired_handle",
    AvatarAscii = "ASCII_ART"
};
var completeResp = await httpClient.PostAsJsonAsync("/auth/github/complete", completeReq)
                                 .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<CompleteResponse>())
                                 .Unwrap();
```

## Notes
- The device-code flow is inherently time-bound; the StartResponse contains a device code and expiry information used by clients to poll for completion.
- Handle validation in Start helps prevent collisions early by checking the user store before redirecting to GitHub.
- Complete enforces length constraints to avoid storing excessively large text or avatar data.
- The behavior depends on the GitHub OAuth device flow endpoints; failures from GitHub yield 502 responses, while malformed or missing inputs yield 400-level errors with descriptive messages.