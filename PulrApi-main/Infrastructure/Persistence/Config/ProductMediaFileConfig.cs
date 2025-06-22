using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductMediaFileConfig : IEntityTypeConfiguration<ProductMediaFile>
    {
        public void Configure(EntityTypeBuilder<ProductMediaFile> builder)
        {

            builder.HasKey(pf => new { pf.ProductId, pf.MediaFileId });

            builder.HasOne(vsp => vsp.Product)
                .WithMany(spo => spo.ProductMediaFiles)
                .HasForeignKey(pf => pf.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
