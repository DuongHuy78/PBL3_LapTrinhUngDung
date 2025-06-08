using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Volunteer_website.Models;

public partial class VolunteerManagementContext : DbContext
{
    public VolunteerManagementContext()
    {
    }

    public VolunteerManagementContext(DbContextOptions<VolunteerManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Donation> Donations { get; set; }

    public virtual DbSet<Evaluation> Evaluations { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventType> EventTypes { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Volunteer> Volunteers { get; set; }

   

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admin__43AA4141C26052D9");

            entity.ToTable("Admin");

            entity.Property(e => e.AdminId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("admin_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.ImgPath)
                .HasMaxLength(200)
                .HasColumnName("img_path");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.DonationId).HasName("PK__Donation__296B91DC678E6A79");

            entity.ToTable("Donation");

            entity.Property(e => e.DonationId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("donation_id");
            entity.Property(e => e.Amount)
                .HasColumnType("money")
                .HasColumnName("amount");
            entity.Property(e => e.DonationDate)
                .HasColumnType("datetime")
                .HasColumnName("donation_date");
            entity.Property(e => e.EventId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("event_id");
            entity.Property(e => e.Message)
                .HasMaxLength(500)
                .HasColumnName("message");
            entity.Property(e => e.VolunteerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("volunteer_id");

            entity.HasOne(d => d.Event).WithMany(p => p.Donations)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__Donation__event___5AEE82B9");

            entity.HasOne(d => d.Volunteer).WithMany(p => p.Donations)
                .HasForeignKey(d => d.VolunteerId)
                .HasConstraintName("FK__Donation__volunt__59FA5E80");
        });

        modelBuilder.Entity<Evaluation>(entity =>
        {
            entity.HasKey(e => e.EvaluationId).HasName("PK__Evaluati__36AE68F3A5580E2A");

            entity.Property(e => e.EvaluationId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.EvaluatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RegId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("reg_id");

            entity.HasOne(d => d.Reg).WithMany(p => p.Evaluations)
                .HasForeignKey(d => d.RegId)
                .HasConstraintName("FK_Evaluations_Registrations");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__2370F72732470AFA");

            entity.Property(e => e.EventId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("event_id");
            entity.Property(e => e.DayBegin).HasColumnName("day_begin");
            entity.Property(e => e.DayEnd).HasColumnName("day_end");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(100)
                .HasColumnName("image_path");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ListImg)
                .HasMaxLength(100)
                .HasColumnName("list_img");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.OrgId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("org_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TargetFunds).HasColumnName("target_funds");
            entity.Property(e => e.TargetMember).HasColumnName("target_member");
            entity.Property(e => e.TypeEventId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type_event_id");

            entity.HasOne(d => d.Org).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrgId)
                .HasConstraintName("FK__Events__org_id__4E88ABD4");

            entity.HasOne(d => d.TypeEvent).WithMany(p => p.Events)
                .HasForeignKey(d => d.TypeEventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Events_EventType");
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(e => e.TypeEventId).HasName("PK__EventTyp__9AB5A4B027EAEFA3");

            entity.ToTable("EventType");

            entity.HasIndex(e => e.Name, "UQ_EventType_Name").IsUnique();

            entity.Property(e => e.TypeEventId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type_event_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrgId).HasName("PK__Organiza__F6AD8012AD205AE0");

            entity.ToTable("Organization");

            entity.Property(e => e.OrgId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("org_id");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(50)
                .HasColumnName("image_path");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phone_number");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegId).HasName("PK__Registra__74038772661EDC80");

            entity.Property(e => e.RegId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("reg_id");
            entity.Property(e => e.EventId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("event_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.VolunteerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("volunteer_id");

            entity.HasOne(d => d.Event).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__Registrat__event__571DF1D5");

            entity.HasOne(d => d.Volunteer).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.VolunteerId)
                .HasConstraintName("FK__Registrat__volun__5629CD9C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F052DC85A");

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.RandomKey).HasMaxLength(255);
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_name");
        });

        modelBuilder.Entity<Volunteer>(entity =>
        {
            entity.HasKey(e => e.VolunteerId).HasName("PK__Voluntee__0FE766B1E87E1CB4");

            entity.Property(e => e.VolunteerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("volunteer_id");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(200)
                .HasColumnName("image_path");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phone_number");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
