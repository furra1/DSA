package com.dsamanger.client.network;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionException;

/** Small asynchronous HTTP transport. It never disables TLS certificate validation. */
public final class HttpTransport {
    private static final int MAX_RESPONSE_BYTES = 1_000_000;
    private static final Duration REQUEST_TIMEOUT = Duration.ofSeconds(15);
    private final HttpClient client;

    public HttpTransport() {
        this(HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(5))
                .followRedirects(HttpClient.Redirect.NEVER)
                .build());
    }

    public HttpTransport(HttpClient client) {
        this.client = java.util.Objects.requireNonNull(client, "client");
    }

    public CompletableFuture<String> get(URI uri, String bearerToken) {
        return send(uri, "GET", null, bearerToken);
    }

    public CompletableFuture<String> postJson(URI uri, String json, String bearerToken) {
        return send(uri, "POST", json, bearerToken);
    }

    public CompletableFuture<String> patchJson(URI uri, String json, String bearerToken) {
        return send(uri, "PATCH", json, bearerToken);
    }

    private CompletableFuture<String> send(URI uri, String method, String json, String bearerToken) {
        if (uri == null || !("https".equalsIgnoreCase(uri.getScheme())
                || "http".equalsIgnoreCase(uri.getScheme()))) {
            return CompletableFuture.failedFuture(new IllegalArgumentException("Only HTTP(S) URLs are supported."));
        }
        HttpRequest.Builder builder = HttpRequest.newBuilder(uri)
                .timeout(REQUEST_TIMEOUT)
                .header("Accept", "application/json");
        if (bearerToken != null && !bearerToken.isBlank()) {
            if (bearerToken.indexOf('\r') >= 0 || bearerToken.indexOf('\n') >= 0) {
                return CompletableFuture.failedFuture(new IllegalArgumentException("Invalid bearer token."));
            }
            builder.header("Authorization", "Bearer " + bearerToken.trim());
        }

        if ("GET".equals(method)) {
            builder.GET();
        } else {
            builder.header("Content-Type", "application/json; charset=utf-8")
                    .method(method, HttpRequest.BodyPublishers.ofString(json == null ? "" : json));
        }

        return client.sendAsync(builder.build(), HttpResponse.BodyHandlers.ofByteArray())
                .thenApply(response -> {
                    byte[] bytes = response.body();
                    if (bytes.length > MAX_RESPONSE_BYTES) {
                        throw new ApiException("API response exceeded the 1 MB limit.");
                    }
                    int status = response.statusCode();
                    String body = new String(bytes, StandardCharsets.UTF_8);
                    if (status < 200 || status >= 300) {
                        String detail = switch (status) {
                            case 400 -> "Request was rejected as invalid.";
                            case 401 -> "Authentication is required or has expired.";
                            case 403 -> "You do not have permission to perform this action.";
                            case 404 -> "The requested API resource was not found.";
                            case 409 -> "The request conflicts with the current server state.";
                            case 429 -> "Too many requests. Try again later.";
                            default -> status >= 500 ? "The server failed to process the request." : "API request failed.";
                        };
                        throw new ApiException(detail, status);
                    }
                    return body;
                })
                .exceptionallyCompose(error -> {
                    Throwable cause = error instanceof CompletionException && error.getCause() != null
                            ? error.getCause() : error;
                    if (cause instanceof ApiException) return CompletableFuture.failedFuture(cause);
                    if (cause instanceof IOException) {
                        return CompletableFuture.failedFuture(new ApiException("Could not reach the API server.", cause));
                    }
                    return CompletableFuture.failedFuture(cause);
                });
    }
}
