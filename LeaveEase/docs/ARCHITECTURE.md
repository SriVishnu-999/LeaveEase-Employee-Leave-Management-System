# LeaveEase Architecture

## Request flow

React SPA → JWT-authenticated ASP.NET Core API → EF Core → SQL Server

MailKit is invoked by the API after leave submission and after an approval/rejection. SMTP failures are logged and do not roll back a successful leave transaction.

## Roles

- Employee: dashboard, balances, apply leave, view/cancel own pending requests.
- Manager: all employee capabilities plus direct-report approval/rejection queue.
- Admin: organization-wide visibility plus leave type and yearly balance administration.

## Balance model

For each employee, leave type, and year:

`Available = Allocated - Used - Pending`

- Applying for leave increases `Pending`.
- Approving a request decreases `Pending` and increases `Used`.
- Rejecting or cancelling decreases `Pending` only.

Updates run inside a database transaction and the balance record includes a SQL Server row-version field for optimistic concurrency.

## Leave-day calculation

The demo business rule counts Monday–Friday and excludes weekends. Organization-specific public holidays can be added as a `Holiday` table and excluded in `LeavePolicyService`.
