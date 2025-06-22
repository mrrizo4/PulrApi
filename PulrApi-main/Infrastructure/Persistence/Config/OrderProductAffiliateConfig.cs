using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config;

public class OrderProductAffiliateConfig : IEntityTypeConfiguration<OrderProductAffiliate>
{
    public void Configure(EntityTypeBuilder<OrderProductAffiliate> builder)
    {
        builder.HasKey(b => b.OrderId);
        builder.HasKey(b => b.ProductId);
        builder.HasKey(b => b.AffiliateId);

        builder.HasOne(o => o.Order)
            .WithMany(o => o.OrderProductAffiliates)
            .HasForeignKey(b => b.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(o => o.Affiliate)
            .WithOne(a => a.OrderProductAffiliate)
            .HasForeignKey<OrderProductAffiliate>(b => b.AffiliateId)
            .OnDelete(DeleteBehavior.NoAction);
        ;

        builder.HasOne(o => o.Product)
            .WithOne(p => p.OrderProductAffiliate)
            .HasForeignKey<OrderProductAffiliate>(b => b.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
