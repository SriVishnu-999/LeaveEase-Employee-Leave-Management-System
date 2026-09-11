# 5-minute interview demo flow

1. Sign in as `employee@leaveease.local` / `Pass@123`.
2. Show the overview dashboard and explain allocated, used, pending, and available leave days.
3. Open **Apply leave**, choose a type and date range, and point out live working-day/balance validation.
4. Submit a request and show it in **My requests** with its audit history.
5. Sign out and sign in as `manager@leaveease.local` / `Pass@123`.
6. Open **Team approvals**, review the employee reason, approve or reject with a comment, and explain the transaction-safe balance update.
7. Sign back in as the employee and show the status/comment update. Mention that MailKit sends the same result by email when SMTP is enabled.
8. Sign in as `admin@leaveease.local` / `Pass@123`, open **Administration**, and demonstrate leave-type and yearly allocation management.

## Architecture explanation for interview

The React SPA sends JWT-authenticated REST requests to ASP.NET Core. ASP.NET Core Identity stores users and roles, EF Core persists HR data in SQL Server, business rules reserve pending days before manager review, and manager decisions update balances inside a SQL transaction. MailKit handles SMTP notifications after the transaction succeeds.
