package com.dsamanger.client.config;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class ApiBaseUrlTest {
    @Test void acceptsHttpsRemoteHost() {
        assertEquals("https://api.example.com", ApiBaseUrl.parse("https://api.example.com/").toString());
    }
    @Test void acceptsHttpLoopbackForLocalDevelopment() {
        assertEquals("http://localhost:5000", ApiBaseUrl.parse("http://localhost:5000").toString());
    }
    @Test void rejectsHttpRemoteHost() {
        assertThrows(IllegalArgumentException.class, () -> ApiBaseUrl.parse("http://api.example.com"));
    }
    @Test void rejectsCredentialsAndQuery() {
        assertThrows(IllegalArgumentException.class, () -> ApiBaseUrl.parse("https://user:pass@example.com"));
        assertThrows(IllegalArgumentException.class, () -> ApiBaseUrl.parse("https://example.com?token=x"));
    }
    @Test void rejectsPath() {
        assertThrows(IllegalArgumentException.class, () -> ApiBaseUrl.parse("https://example.com/api"));
    }
}
