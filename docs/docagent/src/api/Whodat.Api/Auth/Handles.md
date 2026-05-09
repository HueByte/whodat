# Handles

> **File:** `src/api/Whodat.Api/Auth/Handles.cs`  
> **Kind:** class

Normalizes a raw handle into a canonical, lowercase form and validates it against the allowed handle pattern; if valid, the normalized string is returned, otherwise null. This centralizes handle normalization to ensure consistent storage and comparisons.

## Remarks

- The Valid regex enforces a strict handle syntax: the string must start with a lowercase letter or digit, may contain lowercase letters, digits, or hyphens in the middle, and (if longer than one character) must end with a lowercase letter or digit. The overall length is constrained to 1–32 characters. Hyphens cannot be at the start or end.
- Normalize performs: trim whitespace, convert to lowercase via ToLowerInvariant, then validate with the generated regex. If the cleaned string matches, it is returned; otherwise null is returned.
- The regex is defined via [GeneratedRegex], which means the pattern is compiled ahead of time and the resulting Regex is cached for efficient reuse. The method itself does not allocate beyond the initial trim/lowercase and the IsMatch check.
- The input is a non-null string. If you pass null, a NullReferenceException will occur at the Trim call; callers should guard or provide a non-null value.
- The return type is string? to indicate that invalid inputs yield null, signaling to callers that normalization was not possible.

## Example

```csharp
string input = "  Example-Handle-01  ";
string? normalized = Handles.Normalize(input);
Console.WriteLine(normalized); // prints: example-handle-01
```

## Notes
- Nullability: Normalize returns null for invalid inputs; callers should handle the possibility of a null result.
- Thread-safety: The underlying generated Regex is static and designed to be thread-safe; concurrent calls reuse the compiled regex.
- Character set: Only ASCII lowercase letters a–z, digits 0–9, and hyphens are allowed by the pattern; any other characters will cause normalization to return null.
