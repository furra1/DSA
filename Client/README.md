# DSA Manager — Java client core

This folder contains the **client-side foundation only**. There are no windows or UI framework dependencies; the visual design can be added later without changing the HTTP/API layer.

## Stack

- Java 21
- Gradle (Groovy DSL)
- Java `HttpClient` for asynchronous HTTP
- Jackson for JSON
- JUnit 5 for tests
- Gradle Version Catalog at `gradle/libs.versions.toml`

## Build and test

Install JDK 21 and Gradle 8.10+ (or use a correctly generated Gradle wrapper), then from this directory run:

```bash
gradle test
gradle build
```

## Implemented client-side capabilities

- Validates and normalizes API base URLs. HTTPS is required for non-loopback hosts; HTTP is allowed only for local development.
- Uses short connection/request timeouts, refuses automatic redirects, preserves standard TLS certificate validation, and caps response bodies at 1 MB.
- Calls the known health endpoint `GET /api/health`.
- Calls the endpoints defined by the supplied `UsersController`: `GET /api/users`, `POST /api/users`, and `PATCH /api/users/{id}/role`.
- Models the known request/response DTOs and string role values `Employee`, `Manager`, `Admin`.
- Keeps a bearer access token in memory only; no token is written to disk or logs.

## API limitations from the supplied repository

The client deliberately does **not** invent a login or registration endpoint. The supplied `Controllers/AuthController.cs` file contains a `UserService` implementation rather than an MVC controller with HTTP routes, so there is no confirmed login route or token response contract to call. Admin-only user endpoints will return 401/403 until the server exposes and configures the expected authentication contract. No C# files were changed.

## Suggested next client-only steps

1. Add integration tests against a local test HTTP server.
2. Add repositories/use cases over `ApiClient`.
3. Once the server team documents the login route and JWT response shape, implement a client-side authentication service without changing the server.
4. Add the UI only after the design is approved.
