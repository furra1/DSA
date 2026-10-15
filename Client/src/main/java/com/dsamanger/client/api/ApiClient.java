package com.dsamanger.client.api;

import com.dsamanger.client.config.ApiBaseUrl;
import com.dsamanger.client.model.ChangeUserRoleRequest;
import com.dsamanger.client.model.CreateUserRequest;
import com.dsamanger.client.model.HealthResponse;
import com.dsamanger.client.model.UserResponse;
import com.dsamanger.client.network.ApiException;
import com.dsamanger.client.network.HttpTransport;
import com.dsamanger.client.network.JsonCodec;

import java.net.URI;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.CompletableFuture;

/** API wrapper for endpoints that are present in the supplied C# controllers. */
public final class ApiClient {
    private final HttpTransport transport;
    private final JsonCodec json;
    private volatile URI baseUri;
    private volatile String accessToken;

    public ApiClient(String baseUrl) { this(ApiBaseUrl.parse(baseUrl), new HttpTransport(), new JsonCodec()); }

    public ApiClient(URI baseUri, HttpTransport transport, JsonCodec json) {
        this.baseUri = ApiBaseUrl.parse(baseUri == null ? null : baseUri.toString());
        this.transport = java.util.Objects.requireNonNull(transport, "transport");
        this.json = java.util.Objects.requireNonNull(json, "json");
    }

    public void setBaseUrl(String baseUrl) { this.baseUri = ApiBaseUrl.parse(baseUrl); }
    public URI getBaseUri() { return baseUri; }

    /** Sets an access token in memory only; token persistence is deliberately not implemented yet. */
    public void setAccessToken(String token) {
        if (token != null && token.isBlank()) token = null;
        this.accessToken = token;
    }

    public void clearSession() { this.accessToken = null; }

    public CompletableFuture<HealthResponse> health() {
        return transport.get(endpoint("/api/health"), null)
                .thenApply(body -> json.read(body, HealthResponse.class));
    }

    public CompletableFuture<List<UserResponse>> getUsers() {
        return transport.get(endpoint("/api/users"), requireToken())
                .thenApply(body -> Arrays.asList(json.read(body, UserResponse[].class)));
    }

    public CompletableFuture<UserResponse> createUser(CreateUserRequest request) {
        java.util.Objects.requireNonNull(request, "request");
        return transport.postJson(endpoint("/api/users"), json.write(request), requireToken())
                .thenApply(body -> json.read(body, UserResponse.class));
    }

    public CompletableFuture<Void> changeUserRole(int userId, ChangeUserRoleRequest request) {
        if (userId <= 0) throw new IllegalArgumentException("userId must be positive.");
        java.util.Objects.requireNonNull(request, "request");
        URI uri = endpoint("/api/users/" + userId + "/role");
        return transport.patchJson(uri, json.write(request), requireToken()).thenApply(ignored -> null);
    }

    private String requireToken() {
        String token = accessToken;
        if (token == null || token.isBlank()) {
            throw new ApiException("This endpoint requires an access token. Login endpoint is not defined in the supplied API controllers.", 401);
        }
        return token;
    }

    private URI endpoint(String path) { return baseUri.resolve(path); }
}
