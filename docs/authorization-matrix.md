# Authorization Matrix

This document defines the authentication and authorization boundaries for Phase 3.

## Access Matrix

|Operation|Anonymous|Employee|Manager|HR|Scope|
|-|-:|-:|-:|-:|-|
|Login|Allowed|Allowed|Allowed|Allowed|Everyone|
|Create employee|401|403|403|Allowed|HR only|
|Update employee|401|403|403|Allowed|HR only|
|Delete employee|401|403|403|Allowed|HR only|
|View employees|401|403|403|Allowed|HR only|
|Create leave request|401|Allowed|Allowed|Allowed|Current employee|
|View own leave requests|401|Allowed|Allowed|Allowed|Current employee|
|View own leave balance|401|Allowed|Allowed|Allowed|Current employee|
|View leave request by id|401|Own request|Own or active direct-report request|Allowed|Role-scoped|
|Update leave request|401|Own pending request|Own pending request|Own pending request|Current employee|
|Delete leave request|401|Own pending request|Own pending request|Own pending request|Current employee|
|View direct-report leave requests|401|403|Allowed|403|Active direct reports|
|View all leave requests|401|403|403|Allowed|HR only|
|Approve leave request|401|403|Allowed|403|Current active direct report|
|Reject leave request|401|403|Allowed|403|Current active direct report|
|View leave calendar|401|Own records|Active direct reports|All records|Role-scoped|
|View department leave statistics|401|403|403|Allowed|HR only|

## HTTP Status Rules

* Missing or invalid authentication returns `401 Unauthorized`.
* A valid authenticated user who fails a role or coarse-grained authorization policy receives `403 Forbidden`.
* A technically valid token belonging to an inactive user account returns `403 Forbidden`.
* A technically valid token belonging to an inactive employee returns `403 Forbidden`.
* A technically valid token whose role no longer matches the employee's current database role returns `403 Forbidden`.
* A resource that does not exist returns `404 Not Found`.
* A resource outside the caller's permitted ownership or direct-report scope is exposed as `404 Not Found`.
* A valid resource in an invalid business state returns `400 Bad Request`.
* A manager whose role is valid but who is not the employee's current direct manager receives `404 Not Found` for resource-specific operations.
* Collection endpoints return only the records within the caller's permitted scope; inaccessible records are not included in the result.
* Public forbidden responses must not expose internal authorization details.

## Forbidden Response Contract

Policy-based and application-level authorization failures that return `403 Forbidden` use the same public response contract:

```json
{
  "status": 403,
  "title": "Forbidden.",
  "detail": "You do not have permission to perform this operation."
}
```

## Identity Rules

* The required JWT claims are `sub`, `jti`, `email`, `employee_id`, and `role`.
* Every required claim must occur exactly once and contain a valid, non-empty value.
* User and reviewer identities are derived from validated JWT claims.
* Request bodies must not accept identity fields that can be derived from the authenticated user.
* The `sub` claim identifies the user account.
* The `employee_id` claim identifies the associated employee.
* The user account referenced by `sub` must exist.
* `UserAccount.EmployeeId` must match the validated `employee_id` claim.
* The current database role is authoritative for authorization.
* A role claim alone does not grant access.
* Sensitive operations verify the current database state of the user account, employee, and role.
* Current-user access state may be cached only for the lifetime of a single request.

## Leave Review Rules

* HR users can view all leave requests but cannot approve or reject them.
* Managers can approve or reject only requests belonging to their current active direct reports.
* The employee's current manager at review time is authoritative.
* Active direct reports are within a manager's viewing and review scope.
* Inactive direct reports are outside the manager's viewing and review scope.
* Historical leave records of inactive employees remain accessible to HR.
* Ownership and direct-report checks for mutation operations must be repeated inside the application use case.

## Employee Administration Rules

* Employee administration is restricted to a currently active HR user.
* The final active HR cannot be deactivated.
* The final active HR cannot be assigned a non-HR role.
* The final active HR cannot be deleted.
* An active HR is an active employee with the HR role and an active associated user account.
* A manager assignment requires an existing, active employee whose current role is `Manager`.
* An employee cannot be assigned as their own manager.
* Manager hierarchy cycles are not allowed.
* A manager with active direct reports cannot be deactivated.
* A manager with active direct reports cannot be assigned a non-Manager role.
* A manager with any direct reports cannot be hard deleted.

## API Security Rules

* Collection queries must apply authorization filters in the database query.
* Collections must not be fully loaded and then filtered in memory.
* Controller policies perform coarse-grained authorization.
* Application handlers and queries enforce ownership and direct-report boundaries.
* Every production API action must explicitly declare `[Authorize]` or `[AllowAnonymous]`.
* A global fallback policy provides a second layer of protection.
* The default and fallback policies must validate the current active user, not only the presence of an authenticated token.



