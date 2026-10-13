using AlQalam.Users.Core.Entities;
using AlQalam.Users.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlQalam.Users.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.FullName)
            .HasMaxLength(UserProfile.MaxFullNameLength)
            .IsRequired();

        builder.Property(p => p.MemorizationLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
