# Dtos.cs

> **Source:** `src/api/Whodat.Api/Models/Dtos.cs`

## Contents

- [EntryDto](#entrydto)
- [RegisterRequest](#registerrequest)
- [TokenResponse](#tokenresponse)
- [UpdateRequest](#updaterequest)

---

<a id="entrydto"></a>

## EntryDto

> **File:** `src/api/Whodat.Api/Models/Dtos.cs`  
> **Kind:** record

EntryDto is a data transfer object that aggregates user entry information for API clients. It captures a user handle, optional text, avatar ASCII art, metadata, aliases, visibility flags, and timestamps, and can be produced from a WhodatUser via the From factory to normalize data for clients.

## Remarks

- The handle is derived from u.UserName with a fallback to an empty string if null, ensuring the DTO always carries a non-null handle value.
- Text and AvatarAscii preserve nullability as defined in the record; Text and AvatarAscii may be null.
- Metadata is populated by deserializing MetadataJson into a `Dictionary<string, string>`:
  - If MetadataJson is null or empty, an empty dictionary is used.
  - Otherwise, MetadataJson is deserialized using System.Text.Json; if deserialization yields null, an empty dictionary is used.
  - Note: Invalid JSON will throw an exception at runtime when From is invoked.
- Aliases are collected from u.Aliases, extracting the Alias string from each element, then sorted in ascending order to yield a deterministic list.
- IsHidden and RandomVisible map to their JSON names via JsonPropertyName attributes: is_hidden and random_visible respectively.
- RegisteredAt and UpdatedAt map to registered_at and updated_at in JSON.
- This conversion is stateless and side-effect-free beyond data transformation; the From method simply constructs a new EntryDto.
- The code assumes u.Aliases is non-null; a null collection would raise an exception during mapping.
- The JsonPropertyName attributes affect only JSON serialization; internal representations remain as defined by the C# types.

## Example

```csharp
// Example: convert a WhodatUser to EntryDto
WhodatUser user = new WhodatUser
{
  UserName = "jdoe",
  Text = "Hello world",
  AvatarAscii = ":)",
  MetadataJson = "{\"role\":\"member\"}",
  Aliases = new List<WhodatAlias> { new WhodatAlias { Alias = "jdoe" }, new WhodatAlias { Alias = "john" } },
  IsHidden = false,
  RandomVisible = true,
  RegisteredAt = 1700000000,
  UpdatedAt = 1700000100
};

EntryDto dto = EntryDto.From(user);
```

## Notes
- Aliases are sorted to ensure stable API output regardless of input order.
- If MetadataJson contains invalid JSON, From will throw; ensure metadata JSON is validated upstream.
- The handle defaults to an empty string if UserName is null, which may be important for downstream consumers expecting a non-null identifier.
- Non-nullability of Aliases is assumed; ensure the source WhodatUser provides a non-null Aliases collection to avoid runtime exceptions.

---

<a id="registerrequest"></a>

## RegisterRequest

> **File:** `src/api/Whodat.Api/Models/Dtos.cs`  
> **Kind:** record

```csharp
public record RegisterRequest(
    string Handle,
    string? Password,
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string>? Metadata)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Handle` | `string` | — |
| `Password` | `string?` | — |
| `Text` | `string?` | — |
| `AvatarAscii` | `string?` | — |
| `Metadata` | ``Dictionary<string, string>`?` | — |


RegisterRequest is a data contract used to submit user registration data to the API. It requires a non-null Handle and may include an optional Password, Text, AvatarAscii, and Metadata. The AvatarAscii value is serialized to JSON as avatar_ascii, and Metadata is a dictionary of additional string attributes.

## Remarks
- AvatarAscii is serialized as avatar_ascii in JSON.
- All fields except Handle are optional; Metadata is a `Dictionary<string, string>`?.

---

<a id="tokenresponse"></a>

## TokenResponse

> **File:** `src/api/Whodat.Api/Models/Dtos.cs`  
> **Kind:** record

```csharp
/// Returned on every register / login. Includes the handle so login flows
/// (where the CLI doesn't know the handle ahead of time) can save it locally
/// without a follow-up call.
public record TokenResponse(string Token, string Handle)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `time` | `where the CLI doesn't know the handle ahead of` | — |


TokenResponse is the payload returned after a user registers or logs in. It includes the authentication token alongside the user handle so CLI flows that don’t know the handle ahead of time can persist credentials locally and resume authenticated operations without an extra request.

## Remarks

TokenResponse is an immutable record with two string properties: Token and Handle. The positional record form provides concise construction and stable value-based equality, which is handy for caching or comparing responses in tests. The Token is used to authorize subsequent API calls, while the Handle uniquely identifies the user or session on the server side. If you persist this payload locally, treat both fields as sensitive data and protect storage accordingly. The server controls the token’s lifetime; when it expires, re-authentication is required.

## Example

```csharp
// Example: persist the token and handle for later authenticated requests
var resp = await authClient.LoginAsync("user", "pass");
var json = JsonSerializer.Serialize(resp);
File.WriteAllText("token.json", json);

// Later, load the persisted token
var loaded = JsonSerializer.Deserialize<TokenResponse>(File.ReadAllText("token.json"));
```

## Notes

- TokenResponse is immutable; both Token and Handle are fixed at construction.
- The Token should be treated as sensitive data; avoid logging or exposing it in UI traces.
- Do not assume the token never expires; verify validity and re-authenticate as needed.
- The Handle enables offline or sequential CLI flows by preserving user context across sessions.

---

<a id="updaterequest"></a>

## UpdateRequest

> **File:** `src/api/Whodat.Api/Models/Dtos.cs`  
> **Kind:** record

Represents a payload for updating an entity, allowing partial updates by using nullable members; provided values replace existing fields while nulls indicate no change.

## Remarks
All members are optional. Text, AvatarAscii, Metadata, IsHidden, RandomVisible and Aliases may be omitted (null) to indicate no change. The AvatarAscii, IsHidden, and RandomVisible properties map to specific JSON field names via JsonPropertyName attributes, ensuring stable serialization even when property names differ in C#. Aliases supports a replace-all semantics: pass null to leave unchanged; pass an empty list to clear all aliases; otherwise provide the desired list to replace existing aliases. Metadata is a dictionary of arbitrary key-value pairs that can be updated alongside other fields. The type is a C# record with an explicit primary constructor; instances are immutable after creation.

## Example
```csharp
var update = new UpdateRequest(
    Text: "Updated description",
    AvatarAscii: null,
    Metadata: null,
    IsHidden: null,
    RandomVisible: true,
    Aliases: null
);
```

## Notes
- Null values indicate no change for the corresponding field.
- Aliases semantics: null to leave unchanged; empty list to clear all aliases.
- To clear the Text field, provide an empty string.


---