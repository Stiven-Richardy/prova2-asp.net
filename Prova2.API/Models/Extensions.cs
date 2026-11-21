using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Prova2.API.Models
{
    public static class Prova2DbContextExtensions
    {
        public static IQueryable<Usuario> GetUsuarios(
            this Prova2DbContext dbContext,
            string? nome = null,
            bool? status = null)
        {
            var query = dbContext.Usuarios.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var termo = nome.Trim().ToLower();
                query = query.Where(u => u.Nome.ToLower().Contains(termo));
            }

            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            return query.OrderBy(u => u.Id);
        }

        public static async Task<Usuario?> GetUsuarioByIdAsync(this Prova2DbContext dbContext, int id)
            => await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        public static async Task<Usuario?> GetUsuarioByNomeAsync(this Prova2DbContext dbContext, string nome)
        {
            var termo = nome.Trim().ToLower();
            return await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Nome.ToLower() == termo);
        }

        public static IQueryable<Produto> GetProdutos(
            this Prova2DbContext dbContext,
            string? nome = null,
            bool? status = null,
            int? idUsuarioCadastro = null)
        {
            var query = dbContext.Produtos
                .AsNoTracking()
                .Include(p => p.UsuarioCadastro)
                .Include(p => p.UsuarioUpdate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var termo = nome.Trim().ToLower();
                query = query.Where(p => p.Nome.ToLower().Contains(termo));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (idUsuarioCadastro.HasValue)
            {
                query = query.Where(p => p.IdUsuarioCadastro == idUsuarioCadastro.Value);
            }

            return query.OrderBy(p => p.Id);
        }

        public static async Task<Produto?> GetProdutoByIdAsync(this Prova2DbContext dbContext, int id)
            => await dbContext.Produtos
                .Include(p => p.UsuarioCadastro)
                .Include(p => p.UsuarioUpdate)
                .FirstOrDefaultAsync(p => p.Id == id);

        public static IQueryable<LogAuditoria> GetLogs(
            this Prova2DbContext dbContext,
            string? entidade = null,
            string? acao = null,
            string? origem = null,
            string? busca = null)
        {
            var query = dbContext.Logs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(entidade))
            {
                var e = entidade.Trim().ToLower();
                query = query.Where(l => l.Entidade.ToLower() == e);
            }

            if (!string.IsNullOrWhiteSpace(acao))
            {
                var a = acao.Trim().ToLower();
                query = query.Where(l => l.Acao.ToLower() == a);
            }

            if (!string.IsNullOrWhiteSpace(origem))
            {
                var o = origem.Trim().ToLower();
                query = query.Where(l => l.Origem.ToLower() == o);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(l =>
                    l.Detalhes.ToLower().Contains(termo) ||
                    l.UsuarioResponsavel.ToLower().Contains(termo));
            }

            return query.OrderByDescending(l => l.DataHora).ThenByDescending(l => l.Id);
        }

        public static async Task RegistrarLogAsync(
            this Prova2DbContext dbContext,
            string acao,
            string entidade,
            int? entidadeId,
            string origem,
            string usuarioResponsavel,
            string detalhes)
        {
            dbContext.Logs.Add(new LogAuditoria
            {
                DataHora = DateTime.Now,
                Acao = acao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Origem = string.IsNullOrWhiteSpace(origem) ? "API" : origem,
                UsuarioResponsavel = string.IsNullOrWhiteSpace(usuarioResponsavel) ? "Sistema" : usuarioResponsavel,
                Detalhes = detalhes
            });

            await dbContext.SaveChangesAsync();
        }
    }

    public static class IQueryableExtensions
    {
        public static IQueryable<TModel> Paging<TModel>(
            this IQueryable<TModel> query,
            int pageSize = 0,
            int pageNumber = 0) where TModel : class
            => pageSize > 0 && pageNumber > 0
                ? query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
                : query;
    }
}