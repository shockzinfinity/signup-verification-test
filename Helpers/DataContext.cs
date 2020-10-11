using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using signup_verification.Entities;

namespace signup_verification.Helpers
{
    public class DataContext : DbContext
    {
        private readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(Configuration.GetConnectionString("signupDatabase"));
        }

        public DbSet<Account> Accounts { get; set; }
    }
}
