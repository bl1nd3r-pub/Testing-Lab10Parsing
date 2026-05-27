using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserDb
{
    public class ForumDbContext : DbContext
    {
        public DbSet<ForumPost> Posts { get; set; }

        public ForumDbContext() : base(DbContextOptionsFactory.Create()) { }

        public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ForumPost>().Property(p => p.Name).HasMaxLength(256).IsRequired();
            modelBuilder.Entity<ForumPost>().Property(p => p.Message).HasMaxLength(8096).IsRequired();
        }
    }
}
