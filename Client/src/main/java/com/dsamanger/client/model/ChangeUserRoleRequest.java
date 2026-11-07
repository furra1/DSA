package com.dsamanger.client.model;

public record ChangeUserRoleRequest(UserRole role) {
    public ChangeUserRoleRequest {
        if (role == null) throw new IllegalArgumentException("role must not be null.");
    }
}
