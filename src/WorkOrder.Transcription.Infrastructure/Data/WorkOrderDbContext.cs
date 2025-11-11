using Microsoft.EntityFrameworkCore;
using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Infrastructure.Data;

public class WorkOrderDbContext : DbContext
{
    public WorkOrderDbContext(DbContextOptions<WorkOrderDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.WorkOrder> WorkOrders { get; set; }
    public DbSet<Transcript> Transcripts { get; set; }
    public DbSet<Asset> Assets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkOrder configuration
        modelBuilder.Entity<Domain.Entities.WorkOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetId).HasMaxLength(64);
            entity.Property(e => e.TranscriptLocale).HasMaxLength(10);
            entity.Property(e => e.LabourHours).HasColumnType("decimal(5,2)");
            entity.Property(e => e.TranscriptConfidence).HasColumnType("decimal(3,2)");

            entity.HasMany(e => e.Transcripts)
                .WithOne(t => t.WorkOrder)
                .HasForeignKey(t => t.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transcript configuration
        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Vendor).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VendorRequestId).HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(10).IsRequired();
            entity.Property(e => e.AudioUri).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ExtractedAsset).HasMaxLength(64);
            entity.Property(e => e.ExtractedHours).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Confidence).HasColumnType("decimal(3,2)");
            entity.Property(e => e.IsFinal).HasDefaultValue(true);
            entity.Property(e => e.UsedLlmFallback).HasDefaultValue(false);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.WorkOrderId).HasDatabaseName("IX_Transcripts_WorkOrderId");
            entity.HasIndex(e => e.CreatedUtc).HasDatabaseName("IX_Transcripts_CreatedUtc");
            entity.HasIndex(e => e.Language).HasDatabaseName("IX_Transcripts_Language");
        });

        // Asset configuration
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.AssetName).HasMaxLength(255);
            entity.Property(e => e.AssetType).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => e.AssetId).IsUnique().HasDatabaseName("IX_Assets_AssetId");
        });
    }
}
