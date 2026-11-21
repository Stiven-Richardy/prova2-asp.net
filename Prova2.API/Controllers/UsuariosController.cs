using System;
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
    public class UsuariosController : ControllerBase
    {
        private readonly ILogger<UsuariosController>? _logger;
        private readonly Prova2DbContext _dbContext;

        public UsuariosController(ILogger<UsuariosController>? logger, Prova2DbContext dbContext)
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

        private string ObterAtor(string padrao = "Administrador")
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
        public async Task<IActionResult> GetUsuariosAsync(
            int pageSize = 50,
            int pageNumber = 1,
            string? nome = null,
            bool? status = null)
        {
            _logger?.LogDebug("'{0}' foi invocado", nameof(GetUsuariosAsync));
            var response = new PagedResponse<UsuarioResponse>();

            try
            {
                var query = _dbContext.GetUsuarios(nome, status);

                response.PageSize = pageSize;
                response.PageNumber = pageNumber;
                response.ItemsCount = await query.CountAsync();

                var lista = await query.Paging(pageSize, pageNumber).ToListAsync();
                response.Model = lista.Select(u => u.ToResponse()).ToList();
                response.Message = $"Página {pageNumber} de {response.PageCount}, Total de usuários: {response.ItemsCount}.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao consultar usuários.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(GetUsuariosAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUsuarioAsync(int id)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(GetUsuarioAsync), id);
            var response = new SingleResponse<UsuarioResponse>();

            try
            {
                var entity = await _dbContext.GetUsuarioByIdAsync(id);
                if (entity != null)
                {
                    response.Model = entity.ToResponse();
                }
                else
                {
                    response.Message = $"Usuário #{id} não encontrado.";
                }
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao consultar usuário.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(GetUsuarioAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostUsuarioAsync([FromBody] PostUsuarioRequest request)
        {
            _logger?.LogDebug("'{0}' foi invocado", nameof(PostUsuarioAsync));
            var response = new SingleResponse<UsuarioResponse>();

            try
            {
                if (!PasswordHelper.ValidarSenhaForte(request.Senha, out var errosSenha))
                {
                    response.DidError = true;
                    response.ErrorMessage = $"A senha não atende aos critérios de segurança: {string.Join(", ", errosSenha)}.";
                    return BadRequest(response);
                }

                var existing = await _dbContext.GetUsuarioByNomeAsync(request.Nome);
                if (existing != null)
                {
                    response.DidError = true;
                    response.ErrorMessage = $"Já existe um usuário cadastrado com o nome '{request.Nome.Trim()}'.";
                    return BadRequest(response);
                }

                var entity = request.ToEntity();
                _dbContext.Usuarios.Add(entity);
                await _dbContext.SaveChangesAsync();

                await _dbContext.RegistrarLogAsync(
                    "CRIAR",
                    "Usuario",
                    entity.Id,
                    ObterOrigem(),
                    ObterAtor(),
                    $"Usuário '{entity.Nome}' criado com status {(entity.Status ? "Ativo" : "Inativo")}.");

                response.Model = entity.ToResponse();
                response.Message = "Usuário criado com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao criar usuário.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(PostUsuarioAsync), ex);
            }

            return response.ToHttpResponse(HttpStatusCode.Created);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutUsuarioAsync(int id, [FromBody] PutUsuarioRequest request)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(PutUsuarioAsync), id);
            var response = new SingleResponse<UsuarioResponse>();

            try
            {
                var entity = await _dbContext.GetUsuarioByIdAsync(id);
                if (entity == null)
                {
                    response.Message = $"Usuário #{id} não encontrado.";
                    return NotFound(response);
                }

                if (!string.IsNullOrWhiteSpace(request.Senha) && !PasswordHelper.ValidarSenhaForte(request.Senha, out var errosSenha))
                {
                    response.DidError = true;
                    response.ErrorMessage = $"A nova senha não atende aos critérios de segurança: {string.Join(", ", errosSenha)}.";
                    return BadRequest(response);
                }

                var duplicado = await _dbContext.GetUsuarioByNomeAsync(request.Nome);
                if (duplicado != null && duplicado.Id != id)
                {
                    response.DidError = true;
                    response.ErrorMessage = $"O nome '{request.Nome.Trim()}' já está em uso por outro usuário.";
                    return BadRequest(response);
                }

                if (entity.Status && !request.Status)
                {
                    var totalAtivos = await _dbContext.Usuarios.CountAsync(u => u.Status);
                    if (totalAtivos <= 1)
                    {
                        response.DidError = true;
                        response.ErrorMessage = "Operação bloqueada: o sistema deve manter pelo menos um usuário ativo.";
                        return BadRequest(response);
                    }
                }

                var statusAnterior = entity.Status;
                var alterouSenha = !string.IsNullOrWhiteSpace(request.Senha);

                entity.Nome = request.Nome.Trim();
                entity.Status = request.Status;

                if (alterouSenha)
                {
                    entity.Senha = PasswordHelper.GerarHash(request.Senha!);
                }

                _dbContext.Usuarios.Update(entity);
                await _dbContext.SaveChangesAsync();

                var acaoLog = statusAnterior != entity.Status ? "ALTERAR_STATUS" : "EDITAR";
                var detalhes = statusAnterior != entity.Status
                    ? $"Status do usuário '{entity.Nome}' alterado de {(statusAnterior ? "Ativo" : "Inativo")} para {(entity.Status ? "Ativo" : "Inativo")}."
                    : $"Dados do usuário '{entity.Nome}' atualizados{(alterouSenha ? " (senha redefinida)" : "")}.";

                await _dbContext.RegistrarLogAsync(acaoLog, "Usuario", entity.Id, ObterOrigem(), ObterAtor(), detalhes);

                response.Model = entity.ToResponse();
                response.Message = "Usuário atualizado com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao atualizar usuário.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(PutUsuarioAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteUsuarioAsync(int id)
        {
            _logger?.LogDebug("'{0}' foi invocado para ID {1}", nameof(DeleteUsuarioAsync), id);
            var response = new Response();

            try
            {
                var entity = await _dbContext.GetUsuarioByIdAsync(id);
                if (entity == null)
                {
                    response.Message = $"Usuário #{id} não encontrado.";
                    return NotFound(response);
                }

                if (entity.Status)
                {
                    var totalAtivos = await _dbContext.Usuarios.CountAsync(u => u.Status);
                    if (totalAtivos <= 1)
                    {
                        response.DidError = true;
                        response.ErrorMessage = "Operação bloqueada: não é possível excluir o único usuário ativo do sistema.";
                        return BadRequest(response);
                    }
                }

                var possuiProdutosVinculados = await _dbContext.Produtos
                    .AnyAsync(p => p.IdUsuarioCadastro == id || p.IdUsuarioUpdate == id);

                if (possuiProdutosVinculados)
                {
                    response.DidError = true;
                    response.ErrorMessage = "Este usuário possui histórico em Produtos e não pode ser excluído. Altere o status para Inativo para revogar o acesso.";
                    return BadRequest(response);
                }

                var nomeRemovido = entity.Nome;
                _dbContext.Usuarios.Remove(entity);
                await _dbContext.SaveChangesAsync();

                await _dbContext.RegistrarLogAsync(
                    "EXCLUIR",
                    "Usuario",
                    id,
                    ObterOrigem(),
                    ObterAtor(),
                    $"Usuário '{nomeRemovido}' (ID #{id}) removido do sistema.");

                response.Message = "Usuário removido com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao excluir usuário.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(DeleteUsuarioAsync), ex);
            }

            return response.ToHttpResponse();
        }

        [HttpPost("login")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            _logger?.LogDebug("'{0}' foi invocado para '{1}'", nameof(LoginAsync), request.Nome);
            var response = new SingleResponse<UsuarioResponse>();

            try
            {
                var entity = await _dbContext.GetUsuarioByNomeAsync(request.Nome);
                if (entity == null || !PasswordHelper.VerificarSenha(request.Senha, entity.Senha))
                {
                    await _dbContext.RegistrarLogAsync(
                        "LOGIN_FALHA",
                        "Sessao",
                        entity?.Id,
                        ObterOrigem(),
                        request.Nome.Trim(),
                        $"Tentativa de acesso negada por credenciais inválidas para '{request.Nome.Trim()}'.");

                    response.DidError = true;
                    response.ErrorMessage = "Usuário ou senha inválidos.";
                    return Unauthorized(response);
                }

                if (!entity.Status)
                {
                    await _dbContext.RegistrarLogAsync(
                        "LOGIN_BLOQUEADO",
                        "Sessao",
                        entity.Id,
                        ObterOrigem(),
                        entity.Nome,
                        $"Acesso bloqueado: usuário '{entity.Nome}' está inativo.");

                    response.DidError = true;
                    response.ErrorMessage = "Usuário inativo. Solicite a reativação ao administrador.";
                    return Unauthorized(response);
                }

                await _dbContext.RegistrarLogAsync(
                    "LOGIN_SUCESSO",
                    "Sessao",
                    entity.Id,
                    ObterOrigem(),
                    entity.Nome,
                    $"Sessão iniciada com sucesso por '{entity.Nome}'.");

                response.Model = entity.ToResponse();
                response.Message = "Autenticação realizada com sucesso.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao validar credenciais.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(LoginAsync), ex);
            }

            return response.ToHttpResponse();
        }
    }
}