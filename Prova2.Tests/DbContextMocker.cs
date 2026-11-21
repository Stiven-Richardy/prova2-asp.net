using Microsoft.EntityFrameworkCore;
using Prova2.API.Models;

namespace Prova2.Tests
{
    public static class DbContextMocker
    {
        public static Prova2DbContext GetProva2DbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<Prova2DbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var dbContext = new Prova2DbContext(options);
            dbContext.Database.EnsureCreated();
            dbContext.SeedTestUsersAndProducts();

            return dbContext;
        }
    }
}