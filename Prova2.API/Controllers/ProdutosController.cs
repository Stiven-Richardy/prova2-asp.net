using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prova2.API.Models;

namespace Prova2.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly ILogger<ProdutosController>? _logger;
        private readonly Prova2DbContext _dbContext;

        public ProdutosController(ILogger<ProdutosController>? logger, Prova2DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        private string ObterOrigem()
        {
            if (HttpContext?.Request?.Headers.TryGetValue("X-Client-Origin", out var origem) == true
                && !string.IsNullOrWhiteSpace(origem))
            {
                return origem.ToString().Trim();
            }
            return "API";
        }

        private string ObterAtor(string padrao = "Sistema")
        {
            if (HttpContext?.Request?.Headers.TryGetValue("X-User-Actor", out var ator) == true
                && !string.IsNullOrWhiteSpace(ator))
            {
                return ator.ToString().Trim();
            }
            return padrao;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProdutosAsync(
            int pageSize = 50,
            int pageNumber = 1,
            string? nome = null,
            bool? status = null,
            int? idUsuarioCadastro = null)
        {
            _logger?.LogDebug("'{0}' foi invocado", nameof(GetProdutosAsync));
            var response = new PagedResponse<ProdutoResponse>();

            try
            {
                var query = _dbContext.GetProdutos(nome, status, idUsuarioCadastro);

                response.PageSize = pageSize;
                response.PageNumber = pageNumber;
                response.ItemsCount = await query.CountAsync();

                var lista = await query.Paging(pageSize, pageNumber).ToListAsync();
                response.Model = lista.Select(p => p.ToResponse()).ToList();
                response.Message = $"Página {pageNumber} de {response.PageCount}, Total de produtos: {response.ItemsCount}.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao consultar produtos.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(GetProdutosAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProdutoAsync(int id)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(GetProdutoAsync), id);
            var response = new SingleResponse<ProdutoResponse>();

            try
            {
                var entity = await _dbContext.GetProdutoByIdAsync(id);
                if (entity != null)
                {
                    response.Model = entity.ToResponse();
                }
                else
                {
                    response.Message = $"Produto #{id} não encontrado.";
                }
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao consultar produto.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(GetProdutoAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostProdutoAsync([FromBody] PostProdutoRequest request)
        {
            _logger?.LogDebug("'{0}' foi invocado", nameof(PostProdutoAsync));
            var response = new SingleResponse<ProdutoResponse>();

            try
            {
                var usuarioCadastro = await _dbContext.GetUsuarioByIdAsync(request.IdUsuarioCadastro);
                if (usuarioCadastro == null || !usuarioCadastro.Status)
                {
                    response.DidError = true;
                    response.ErrorMessage = "Usuário de cadastro inexistente ou inativo.";
                    return BadRequest(response);
                }

                var entity = request.ToEntity();
                _dbContext.Produtos.Add(entity);
                await _dbContext.SaveChangesAsync();

                var precoFmt = entity.Preco.ToString("C2", new CultureInfo("pt-BR"));
                await _dbContext.RegistrarLogAsync(
                    "CRIAR",
                    "Produto",
                    entity.Id,
                    ObterOrigem(),
                    usuarioCadastro.Nome,
                    $"Produto '{entity.Nome}' cadastrado por {precoFmt} ({(entity.Status ? "Ativo" : "Inativo")}).");

                var salvo = await _dbContext.GetProdutoByIdAsync(entity.Id);
                response.Model = (salvo ?? entity).ToResponse();
                response.Message = "Produto cadastrado com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao cadastrar produto.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(PostProdutoAsync), ex);
            }

            return response.ToHttpResponse(HttpStatusCode.Created);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutProdutoAsync(int id, [FromBody] PutProdutoRequest request)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(PutProdutoAsync), id);
            var response = new SingleResponse<ProdutoResponse>();

            try
            {
                var entity = await _dbContext.GetProdutoByIdAsync(id);
                if (entity == null)
                {
                    response.Message = $"Produto #{id} não encontrado.";
                    return NotFound(response);
                }

                var usuarioUpdate = await _dbContext.GetUsuarioByIdAsync(request.IdUsuarioUpdate);
                if (usuarioUpdate == null || !usuarioUpdate.Status)
                {
                    response.DidError = true;
                    response.ErrorMessage = "Usuário de atualização inexistente ou inativo.";
                    return BadRequest(response);
                }

                var statusAnterior = entity.Status;
                var nomeAnterior = entity.Nome;
                var precoAnterior = entity.Preco;

                entity.Nome = request.Nome.Trim();
                entity.Preco = request.Preco;
                entity.Status = request.Status;
                entity.IdUsuarioUpdate = request.IdUsuarioUpdate;

                _dbContext.Produtos.Update(entity);
                await _dbContext.SaveChangesAsync();

                var mudouApenasStatus = statusAnterior != entity.Status
                    && nomeAnterior == entity.Nome
                    && Math.Abs(precoAnterior - entity.Preco) < 0.001f;

                var acao = mudouApenasStatus ? "ALTERAR_STATUS" : "EDITAR";
                var detalhes = mudouApenasStatus
                    ? $"Status do produto '{entity.Nome}' alterado de {(statusAnterior ? "Ativo" : "Inativo")} para {(entity.Status ? "Ativo" : "Inativo")}."
                    : $"Produto '{entity.Nome}' atualizado (Preço: {entity.Preco.ToString("C2", new CultureInfo("pt-BR"))}, Status: {(entity.Status ? "Ativo" : "Inativo")}).";

                await _dbContext.RegistrarLogAsync(acao, "Produto", entity.Id, ObterOrigem(), usuarioUpdate.Nome, detalhes);

                var atualizado = await _dbContext.GetProdutoByIdAsync(entity.Id);
                response.Model = (atualizado ?? entity).ToResponse();
                response.Message = "Produto atualizado com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao atualizar produto.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(PutProdutoAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteProdutoAsync(int id)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(DeleteProdutoAsync), id);
            var response = new Response();

            try
            {
                var entity = await _dbContext.Produtos.FirstOrDefaultAsync(p => p.Id == id);
                if (entity == null)
                {
                    response.Message = $"Produto #{id} não encontrado.";
                    return NotFound(response);
                }

                var nomeRemovido = entity.Nome;
                _dbContext.Produtos.Remove(entity);
                await _dbContext.SaveChangesAsync();

                await _dbContext.RegistrarLogAsync(
                    "EXCLUIR",
                    "Produto",
                    id,
                    ObterOrigem(),
                    ObterAtor(),
                    $"Produto '{nomeRemovido}' (ID #{id}) excluído permanentemente.");

                response.Message = "Produto removido com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao excluir produto.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(DeleteProdutoAsync), ex);
            }

            return response.ToHttpResponse();
        }
    }
}