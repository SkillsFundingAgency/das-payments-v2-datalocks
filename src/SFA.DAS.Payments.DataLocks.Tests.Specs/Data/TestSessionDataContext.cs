using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.DataLocks.Tests.Specs.Data.Configurations;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.Data
{
    public class TestSessionDataContext : DbContext
    {
        private readonly string connectionString;

        public virtual DbSet<ApprenticeshipEmploymentVerificationModel> ApprenticeshipEmploymentVerification { get; set; }

        public TestSessionDataContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString, options => options.CommandTimeout(600));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Payments2");
            modelBuilder.ApplyConfiguration(new ApprenticeshipEmploymentVerificationConfiguration());
        }

        public async Task ClearApprenticeshipEmploymentVerificationAsync(long apprenticeshipId)
        {
            var records = await ApprenticeshipEmploymentVerification
                .Where(x => x.ApprenticeshipId == apprenticeshipId)
                .ToListAsync();

            ApprenticeshipEmploymentVerification.RemoveRange(records);
            await SaveChangesAsync();
        }
    }
}