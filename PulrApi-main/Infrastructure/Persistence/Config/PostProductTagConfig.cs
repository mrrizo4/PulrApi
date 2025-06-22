using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostProductTagConfig : IEntityTypeConfiguration<PostProductTag>
    {
        public void Configure(EntityTypeBuilder<PostProductTag> builder)
        {

            builder.HasKey(pf => new { pf.PostId, pf.ProductId });

            builder.HasOne(vsp => vsp.Post)
                .WithMany(spo => spo.PostProductTags)
                .HasForeignKey(pf => pf.PostId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}

