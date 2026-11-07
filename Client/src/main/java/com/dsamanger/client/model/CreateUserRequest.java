package com.dsamanger.client.model;

/** Request shape mirrors the existing C# CreateUserRequest DTO. */
public record CreateUserRequest(
        String firstName,
        String lastName,
        String email,
        String employeeNumber,
        String password,
        UserRole role) {
    public CreateUserRequest {
        requireText(firstName, "firstName");
        requireText(lastName, "lastName");
        requireText(email, "email");
        requireText(employeeNumber, "employeeNumber");
        if (password == null || password.length() < 10 || password.length() > 128) {
            throw new IllegalArgumentException("Password length must be between 10 and 128 characters.");
        }
        if (role == null) role = UserRole.Employee;
    }

    private static void requireText(String value, String field) {
        if (value == null || value.isBlank()) throw new IllegalArgumentException(field + " must not be blank.");
    }
}
