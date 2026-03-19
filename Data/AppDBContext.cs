using BSEBAnnualResultsMVC.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){ }

    public DbSet<ExamFinalPublishedResult> FinalPublishedResults { get; set; }

    // Tables

}