using Microsoft.EntityFrameworkCore;
using RemoteManagerBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Data
{
    public class DataContext:DbContext
    {
        public DataContext (DbContextOptions<DataContext> options):base(options)
        {


        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>()
                .Property(a => a.jobCount).IsConcurrencyToken();
        }

        public DbSet<Manager> Managers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Client> Clients { get; set; }

    }
}
