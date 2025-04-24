using Microsoft.EntityFrameworkCore;

namespace BilliardsManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

       
    }
}
