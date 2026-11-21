using System.Linq;
using Prova2.API.Models;

namespace Prova2.Tests
{
    public static class DbContextExtensions
    {
        public static void SeedTestUsersAndProducts(this Prova2DbContext dbContext)
        {
            if (!dbContext.Usuarios.Any(u => u.Id == 3))
            {
                dbContext.Usuarios.Add(new Usuario
                {
                    Id = 3,
                    Nome = "inativo.teste",
                    Senha = PasswordHelper.GerarHash("Inativo@123"),
                    Status = false
                });

                dbContext.Usuarios.Add(new Usuario
                {
                    Id = 4,
                    Nome = "usuario.deletavel",
                    Senha = PasswordHelper.GerarHash("Deletar@123"),
                    Status = true
                });

                dbContext.SaveChanges();
            }
        }
    }
}