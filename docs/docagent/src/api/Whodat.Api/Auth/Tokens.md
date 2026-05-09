# Tokens

> **File:** `src/api/Whodat.Api/Auth/Tokens.cs`  
> **Kind:** class

Provides utility methods for generating cryptographically secure tokens and hashing tokens to their SHA-256 digest. The Generate method creates a unique, unpredictable token with a 'wd_' prefix using a secure RNG, while Hash produces a stable, hex-encoded SHA-256 hash of a given token.

## Remarks

- Generate allocates 32 bytes on the stack, fills them with RandomNumberGenerator.Fill, and returns the string formed by 'wd_' followed by the lowercase hex representation of those bytes.
- Hash computes the SHA-256 hash of the UTF-8 bytes of the input token and returns the lowercase hexadecimal string.
- The class is stateless and thread-safe; both methods have no shared mutable state.
- Hash will throw if the input token is null; callers must supply a non-null string.
- This utility relies on cryptographic primitives provided by the runtime (RandomNumberGenerator, SHA256) and uses Convert.ToHexStringLower for hex encoding. It assumes the availability of Convert.ToHexStringLower and SHA256.HashData in the target framework.

## Example

```csharp
string token = Tokens.Generate();
string hash = Tokens.Hash(token);
Console.WriteLine(token);
Console.WriteLine(hash);
```

## Notes

- The generated token has the format: wd_[64 lowercase hex chars], e.g., wd_4f2a... (total length 67).
- The Hash output is a 64-character lowercase hex string representing the SHA-256 digest of the UTF-8 bytes of the input token.
- The methods are self-contained and do not rely on any external state or configuration.
- If you need to compare tokens securely, compare their hashes rather than storing tokens in plaintext; Hash(token) provides a stable digest for such comparisons.
