# API contract observed in the supplied repository

This document records existing routes only; it does not define or assume new server routes.

| Method | Route | Expected result | Access |
|---|---|---|---|
| GET | `/api/health` | `{ status, message, time }` | No authorization attribute on health controller |
| GET | `/api/users` | Array of `UserResponse` | Admin role |
| POST | `/api/users` | `UserResponse` (201), conflict response (409) | Admin role |
| PATCH | `/api/users/{id}/role` | Empty response (204), error response | Admin role |

`UserResponse` fields: `id`, `firstName`, `lastName`, `email`, `employeeNumber`, `role`, `groupId`, `isActive`, `createdAt`.

Roles are serialized as strings by `JsonStringEnumConverter`: `Employee`, `Manager`, `Admin`.

## Not confirmed

No HTTP login route or JWT response DTO is visible in the supplied controller code. The file named `Controllers/AuthController.cs` contains a `UserService` class, not an MVC auth controller. The client therefore does not call login/register endpoints until their contract is supplied.
