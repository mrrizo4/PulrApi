using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductLikeConfig : IEntityTypeConfiguration<ProductLike>
    {
        public void Configure(EntityTypeBuilder<ProductLike> builder)
        {

            builder.HasKey(e => new { e.ProductId, e.LikedById });

            builder.HasOne(e => e.Product)
                .WithMany(e => e.ProductLikes)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
