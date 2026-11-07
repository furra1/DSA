package com.dsamanger.client.network;

/** Represents transport, HTTP, or response-decoding failures. */
public class ApiException extends RuntimeException {
    private final int statusCode;

    public ApiException(String message) { this(message, -1, null); }
    public ApiException(String message, Throwable cause) { this(message, -1, cause); }
    public ApiException(String message, int statusCode) { this(message, statusCode, null); }

    public ApiException(String message, int statusCode, Throwable cause) {
        super(message, cause);
        this.statusCode = statusCode;
    }

    /** Returns the HTTP status, or -1 if the failure was not an HTTP error. */
    public int statusCode() { return statusCode; }
}
