package com.dsamanger.client.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record UserResponse(
        int id,
        String firstName,
        String lastName,
        String email,
        String employeeNumber,
        UserRole role,
        Integer groupId,
        boolean isActive,
        String createdAt) {}
