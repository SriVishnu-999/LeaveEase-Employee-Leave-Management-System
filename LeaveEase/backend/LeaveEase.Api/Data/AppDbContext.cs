using LeaveEase.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveEase.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveRequestHistory> LeaveRequestHistories => Set<LeaveRequestHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>()
            .HasOne(u => u.Manager)
            .WithMany(u => u.DirectReports)
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeaveType>()
            .HasIndex(x => x.Code)
            .IsUnique();

        builder.Entity<LeaveBalance>()
            .HasIndex(x => new { x.UserId, x.LeaveTypeId, x.Year })
            .IsUnique();

        builder.Entity<LeaveBalance>()
            .Property(x => x.Allocated)
            .HasPrecision(8, 2);
        builder.Entity<LeaveBalance>()
            .Property(x => x.Used)
            .HasPrecision(8, 2);
        builder.Entity<LeaveBalance>()
            .Property(x => x.Pending)
            .HasPrecision(8, 2);

        builder.Entity<LeaveBalance>()
            .HasOne(x => x.User)
            .WithMany(x => x.LeaveBalances)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LeaveBalance>()
            .HasOne(x => x.LeaveType)
            .WithMany(x => x.LeaveBalances)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeaveRequest>()
            .Property(x => x.TotalDays)
            .HasPrecision(8, 2);

        builder.Entity<LeaveRequest>()
            .HasOne(x => x.Employee)
            .WithMany(x => x.LeaveRequests)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeaveRequest>()
            .HasOne(x => x.LeaveType)
            .WithMany(x => x.LeaveRequests)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeaveRequest>()
            .HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeaveRequestHistory>()
            .HasOne(x => x.LeaveRequest)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LeaveRequestHistory>()
            .HasOne(x => x.ChangedBy)
            .WithMany()
            .HasForeignKey(x => x.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
