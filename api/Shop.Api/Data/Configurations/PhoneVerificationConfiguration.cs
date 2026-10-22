using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Api.Domain;

namespace Shop.Api.Data.Configurations;

internal sealed class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("PhoneVerification");

        // "+880" and ten digits; 16 leaves room for E.164's 15-digit maximum.
        builder.Property(x => x.Phone).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.AttemptCount).IsConcurrencyToken();

        // Every query is "this phone's rows, newest first" or "rows older than a day".
        builder.HasIndex(x => new { x.Phone, x.CreatedAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
