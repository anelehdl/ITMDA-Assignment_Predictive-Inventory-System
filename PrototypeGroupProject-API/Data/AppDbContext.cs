using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using PrototypeGroupProject_API.Models.Entities;

namespace PrototypeGroupProject_API.Data
{
    public class AppDbContext : DbContext
    {
        // Placeholder for the actual DbContext implementation need to look at how to do this with mongo db
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure entity mappings here
            modelBuilder.Entity<StaffEntity>().ToCollection("StaffUser");       //can change collection name later
        }
        public DbSet<StaffEntity> Staff { get; set; }//mongodb no tables but collections, we need to map this correctly
    }
}
