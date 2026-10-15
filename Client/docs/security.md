# Security decisions

- Standard JDK TLS verification is retained; do not install a trust-all SSL context.
- HTTPS is required for non-loopback hosts. Plain HTTP is permitted only for local development.
- Redirects are disabled to avoid unexpectedly forwarding authorization headers.
- Access tokens are held in memory only and are never logged.
- HTTP request timeout is 15 seconds; connect timeout is 5 seconds.
- Response bodies are limited to 1 MB after receipt.
- Error messages do not include arbitrary response bodies, which may contain sensitive data.
- Client-side checks improve safety and usability but do not replace server-side authorization.
