using AlQalam.Users.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlQalam.Users.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.FamilyId)
            .IsRequired();

        builder.Property(r => r.DeviceId)
            .HasMaxLength(128);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .IsRequired();
    }
}
