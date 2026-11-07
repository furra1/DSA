package com.dsamanger.client.config;

import java.net.URI;
import java.net.URISyntaxException;
import java.util.Locale;

/** Validates and normalizes the base URL used for API requests. */
public final class ApiBaseUrl {
    private ApiBaseUrl() {}

    public static URI parse(String raw) {
        if (raw == null || raw.isBlank()) {
            throw new IllegalArgumentException("API base URL must not be blank.");
        }

        final URI uri;
        try {
            uri = new URI(raw.trim());
        } catch (URISyntaxException ex) {
            throw new IllegalArgumentException("API base URL is malformed.", ex);
        }

        String scheme = uri.getScheme();
        String host = uri.getHost();
        if (scheme == null || host == null ||
                !(scheme.equalsIgnoreCase("https") || scheme.equalsIgnoreCase("http"))) {
            throw new IllegalArgumentException("Use a complete http:// or https:// URL.");
        }
        if (uri.getUserInfo() != null || uri.getQuery() != null || uri.getFragment() != null) {
            throw new IllegalArgumentException("URL must not contain credentials, query, or fragment.");
        }
        String path = uri.getPath();
        if (path != null && !path.isEmpty() && !path.equals("/")) {
            throw new IllegalArgumentException("Use the server base URL without an API path.");
        }

        boolean loopback = host.equalsIgnoreCase("localhost")
                || host.equals("127.0.0.1") || host.equals("::1")
                || host.equals("[::1]");
        if (!scheme.equalsIgnoreCase("https") && !loopback) {
            throw new IllegalArgumentException("HTTPS is required for non-local servers.");
        }

        try {
            return new URI(scheme.toLowerCase(Locale.ROOT), null, host, uri.getPort(), "", null, null);
        } catch (URISyntaxException ex) {
            throw new IllegalArgumentException("API base URL could not be normalized.", ex);
        }
    }
}
