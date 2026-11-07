# Client architecture (UI-independent)

- `config/ApiBaseUrl`: validates the configured server base URL.
- `network/HttpTransport`: HTTP mechanics, timeouts, status handling and response-size limit.
- `network/JsonCodec`: centralized JSON serialization/deserialization.
- `api/ApiClient`: methods corresponding to routes confirmed in the C# controllers.
- `model`: client-side DTOs and role enum matching the existing API JSON.

The future UI should call application/use-case classes and must not construct HTTP requests directly. The access token currently lives in memory only; durable credential storage and refresh handling require a confirmed auth contract.
