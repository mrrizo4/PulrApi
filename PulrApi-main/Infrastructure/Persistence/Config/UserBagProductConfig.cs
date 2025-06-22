using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class UserBagProductConfig : IEntityTypeConfiguration<UserBagProduct>
    {
        public void Configure(EntityTypeBuilder<UserBagProduct> builder)
        {
            builder.HasKey(pf => new { pf.UserId, pf.BagProductId });

            builder.HasOne(vsp => vsp.User)
                 .WithMany(spo => spo.BagItems)
                 .HasForeignKey(pf => pf.UserId)
                 .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
