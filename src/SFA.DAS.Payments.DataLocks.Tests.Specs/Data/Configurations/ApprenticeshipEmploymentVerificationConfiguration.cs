using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.Payments.Model.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.Data.Configurations
{
    public class ApprenticeshipEmploymentVerificationConfiguration : IEntityTypeConfiguration<ApprenticeshipEmploymentVerificationModel>
    {
        public void Configure(EntityTypeBuilder<ApprenticeshipEmploymentVerificationModel> builder)
        {
            builder.ToTable("ApprenticeshipEmploymentVerification", "Payments2");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ApprenticeshipId).HasColumnName("ApprenticeshipId").IsRequired();
            builder.Property(x => x.PreEmploymentStatus).HasColumnName("PreEmploymentStatus").IsRequired();
            builder.Property(x => x.PreEmploymentCorrelationId).HasColumnName("PreEmploymentCorrelationId");
            builder.Property(x => x.FirstHiringGrantStatus).HasColumnName("FirstHiringGrantStatus").IsRequired();
            builder.Property(x => x.FirstHiringGrantCorrelationId).HasColumnName("FirstHiringGrantCorrelationId");
            builder.Property(x => x.SecondHiringGrantStatus).HasColumnName("SecondHiringGrantStatus").IsRequired();
            builder.Property(x => x.SecondHiringGrantCorrelationId).HasColumnName("SecondHiringGrantCorrelationId");
            builder.Property(x => x.CreationDate).HasColumnName("CreationDate").IsRequired();
            builder.Property(x => x.UpdateDate).HasColumnName("UpdateDate").IsRequired();

            builder.HasIndex(x => x.ApprenticeshipId).IsUnique();

            builder.HasOne<ApprenticeshipModel>()
                .WithOne()
                .HasForeignKey<ApprenticeshipEmploymentVerificationModel>(x => x.ApprenticeshipId);
        }
    }
}
