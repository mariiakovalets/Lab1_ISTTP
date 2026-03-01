using System;
using System.Collections.Generic;
using Dormitory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dormitory.Infrastructure.Data;

public partial class DormitoryContext : DbContext
{
    public DormitoryContext()
    {
    }

    public DormitoryContext(DbContextOptions<DormitoryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Administrator> Administrators { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<Applicationstatus> Applicationstatuses { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<Queue> Queues { get; set; }

    public virtual DbSet<Residencehistory> Residencehistories { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>(entity =>
        {
            entity.HasKey(e => e.Adminid).HasName("administrators_pkey");

            entity.ToTable("administrators");

            entity.HasIndex(e => e.Username, "administrators_username_key").IsUnique();

            entity.Property(e => e.Adminid).HasColumnName("adminid");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("fullname");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Applicationid).HasName("applications_pkey");

            entity.ToTable("applications");

            entity.HasIndex(e => e.Studentid, "one_active_application")
                .IsUnique()
                .HasFilter("(statusid = ANY (ARRAY[1, 2]))");

            entity.HasIndex(e => new { e.Studentid, e.Applicationtype, e.Academicperiod }, "uk_active_application")
                .IsUnique()
                .HasFilter("(statusid = ANY (ARRAY[1, 2]))");

            entity.Property(e => e.Applicationid).HasColumnName("applicationid");
            entity.Property(e => e.Academicperiod)
                .HasMaxLength(20)
                .HasColumnName("academicperiod");
            entity.Property(e => e.Adminid).HasColumnName("adminid");
            entity.Property(e => e.Applicationtype)
                .HasMaxLength(50)
                .HasColumnName("applicationtype");
            entity.Property(e => e.Decisiondate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("decisiondate");
            entity.Property(e => e.Extensionenddate).HasColumnName("extensionenddate");
            entity.Property(e => e.Extensionstartdate).HasColumnName("extensionstartdate");
            entity.Property(e => e.Rejectionreason).HasColumnName("rejectionreason");
            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Studentid).HasColumnName("studentid");
            entity.Property(e => e.Submissiondate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("submissiondate");

            entity.HasOne(d => d.Admin).WithMany(p => p.Applications)
                .HasForeignKey(d => d.Adminid)
                .HasConstraintName("applications_adminid_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Applications)
                .HasForeignKey(d => d.Statusid)
                .HasConstraintName("applications_statusid_fkey");

            entity.HasOne(d => d.Student).WithOne(p => p.Application)
                .HasForeignKey<Application>(d => d.Studentid)
                .HasConstraintName("applications_studentid_fkey");
        });

        modelBuilder.Entity<Applicationstatus>(entity =>
        {
            entity.HasKey(e => e.Statusid).HasName("applicationstatuses_pkey");

            entity.ToTable("applicationstatuses");

            entity.HasIndex(e => e.Statusname, "applicationstatuses_statusname_key").IsUnique();

            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Statusname)
                .HasMaxLength(50)
                .HasColumnName("statusname");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Documentid).HasName("documents_pkey");

            entity.ToTable("documents");

            entity.Property(e => e.Documentid).HasColumnName("documentid");
            entity.Property(e => e.Expirydate).HasColumnName("expirydate");
            entity.Property(e => e.Filecontent).HasColumnName("filecontent");
            entity.Property(e => e.Issuedate).HasColumnName("issuedate");
            entity.Property(e => e.Studentid).HasColumnName("studentid");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uploaddate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("uploaddate");

            entity.HasOne(d => d.Student).WithMany(p => p.Documents)
                .HasForeignKey(d => d.Studentid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("documents_studentid_fkey");

            entity.HasOne(d => d.Type).WithMany(p => p.Documents)
                .HasForeignKey(d => d.Typeid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("documents_typeid_fkey");
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasKey(e => e.Typeid).HasName("document_types_pkey");

            entity.ToTable("document_types");

            entity.HasIndex(e => e.Typename, "document_types_typename_key").IsUnique();

            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.IsLifetime)
                .HasDefaultValue(true)
                .HasColumnName("is_lifetime");
            entity.Property(e => e.RequiresIssueDate).HasColumnName("requires_issue_date");
            entity.Property(e => e.Typename)
                .HasMaxLength(100)
                .HasColumnName("typename");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.Facultyid).HasName("faculties_pkey");

            entity.ToTable("faculties");

            entity.Property(e => e.Facultyid).HasColumnName("facultyid");
            entity.Property(e => e.Facultyname)
                .HasMaxLength(200)
                .HasColumnName("facultyname");
        });

        modelBuilder.Entity<Queue>(entity =>
        {
            entity.HasKey(e => e.Queueid).HasName("queue_pkey");

            entity.ToTable("queue");

            entity.HasIndex(e => e.Applicationid, "queue_applicationid_key").IsUnique();

            entity.Property(e => e.Queueid).HasColumnName("queueid");
            entity.Property(e => e.Applicationid).HasColumnName("applicationid");
            entity.Property(e => e.Position).HasColumnName("position");

            entity.HasOne(d => d.Application).WithOne(p => p.Queue)
                .HasForeignKey<Queue>(d => d.Applicationid)
                .HasConstraintName("queue_applicationid_fkey");
        });

        modelBuilder.Entity<Residencehistory>(entity =>
        {
            entity.HasKey(e => e.Historyid).HasName("residencehistory_pkey");

            entity.ToTable("residencehistory");

            entity.Property(e => e.Historyid).HasColumnName("historyid");
            entity.Property(e => e.Checkindate).HasColumnName("checkindate");
            entity.Property(e => e.Checkoutdate).HasColumnName("checkoutdate");
            entity.Property(e => e.Roomid).HasColumnName("roomid");
            entity.Property(e => e.Studentid).HasColumnName("studentid");

            entity.HasOne(d => d.Room).WithMany(p => p.Residencehistories)
                .HasForeignKey(d => d.Roomid)
                .HasConstraintName("residencehistory_roomid_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.Residencehistories)
                .HasForeignKey(d => d.Studentid)
                .HasConstraintName("residencehistory_studentid_fkey");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Roomid).HasName("rooms_pkey");

            entity.ToTable("rooms");

            entity.Property(e => e.Roomid).HasColumnName("roomid");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Floor).HasColumnName("floor");
            entity.Property(e => e.Roomnumber)
                .HasMaxLength(20)
                .HasColumnName("roomnumber");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Studentid).HasName("students_pkey");

            entity.ToTable("students");

            entity.HasIndex(e => e.Email, "students_email_key").IsUnique();

            entity.Property(e => e.Studentid).HasColumnName("studentid");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Birthdate).HasColumnName("birthdate");
            entity.Property(e => e.Course).HasColumnName("course");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Facultyid).HasColumnName("facultyid");
            entity.Property(e => e.Fullname)
                .HasMaxLength(200)
                .HasColumnName("fullname");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Students)
                .HasForeignKey(d => d.Facultyid)
                .HasConstraintName("students_facultyid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
