using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prova2.API.Controllers;
using Prova2.API.Models;
using Xunit;

namespace Prova2.Tests
{
    public class UsuariosControllerUnitTest
    {
        [Fact(DisplayName = "Usuarios GET: Deve retornar lista paginada sem erros")]
        public async Task TestGetUsuariosAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestGetUsuariosAsync));
            var controller = new UsuariosController(null, dbContext);

            var response = await controller.GetUsuariosAsync() as ObjectResult;
            var value = response?.Value as IPagedResponse<UsuarioResponse>;

            Assert.NotNull(value);
            Assert.False(value!.DidError);
            Assert.True(value.ItemsCount >= 4);
        }

        [Fact(DisplayName = "Usuarios POST: Deve cadastrar usuário com senha forte e registrar log")]
        public async Task TestPostUsuarioForteAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPostUsuarioForteAsync));
            var controller = new UsuariosController(null, dbContext);

            var request = new PostUsuarioRequest
            {
                Nome = "novo.usuario",
                Senha = "Senha@2026",
                Status = true
            };

            var response = await controller.PostUsuarioAsync(request) as ObjectResult;
            var value = response?.Value as ISingleResponse<UsuarioResponse>;

            Assert.NotNull(value);
            Assert.Equal(201, response!.StatusCode);
            Assert.False(value!.DidError);
            Assert.Equal("novo.usuario", value.Model?.Nome);
            Assert.True(await dbContext.Logs.AnyAsync(l => l.Acao == "CRIAR" && l.Entidade == "Usuario"));
        }

        [Fact(DisplayName = "Usuarios POST: Deve rejeitar senha fraca com BadRequest (400)")]
        public async Task TestPostUsuarioSenhaFracaAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPostUsuarioSenhaFracaAsync));
            var controller = new UsuariosController(null, dbContext);

            var request = new PostUsuarioRequest
            {
                Nome = "usuario.fraco",
                Senha = "123456",
                Status = true
            };

            var response = await controller.PostUsuarioAsync(request) as ObjectResult;
            Assert.Equal(400, response?.StatusCode);
        }

        [Fact(DisplayName = "Usuarios PUT: Deve inativar usuário existente e registrar log de status")]
        public async Task TestPutUsuarioAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPutUsuarioAsync));
            var controller = new UsuariosController(null, dbContext);

            var request = new PutUsuarioRequest
            {
                Nome = "operador",
                Senha = null,
                Status = false
            };

            var response = await controller.PutUsuarioAsync(2, request) as ObjectResult;
            var value = response?.Value as ISingleResponse<UsuarioResponse>;

            Assert.NotNull(value);
            Assert.False(value!.DidError);
            Assert.False(value.Model!.Status);
            Assert.True(await dbContext.Logs.AnyAsync(l => l.Acao == "ALTERAR_STATUS" && l.EntidadeId == 2));
        }

        [Fact(DisplayName = "Usuarios PUT: Deve impedir inativar o último usuário ativo (proteção contra lockout)")]
        public async Task TestPutLockoutUltimoUsuarioAtivoAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPutLockoutUltimoUsuarioAtivoAsync));
            var controller = new UsuariosController(null, dbContext);

            await controller.PutUsuarioAsync(2, new PutUsuarioRequest { Nome = "operador", Status = false });
            await controller.PutUsuarioAsync(4, new PutUsuarioRequest { Nome = "usuario.deletavel", Status = false });

            var tentativaBloqueada = await controller.PutUsuarioAsync(1, new PutUsuarioRequest { Nome = "admin", Status = false }) as ObjectResult;
            Assert.Equal(400, tentativaBloqueada?.StatusCode);
        }

        [Fact(DisplayName = "Usuarios DELETE: Deve excluir usuário sem vínculos de auditoria")]
        public async Task TestDeleteUsuarioAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestDeleteUsuarioAsync));
            var controller = new UsuariosController(null, dbContext);

            var response = await controller.DeleteUsuarioAsync(4) as ObjectResult;
            var value = response?.Value as IResponse;

            Assert.NotNull(value);
            Assert.Equal(200, response!.StatusCode);
            Assert.False(value!.DidError);
        }

        [Fact(DisplayName = "Usuarios LOGIN: Deve autorizar usuário ativo, bloquear inativo e gravar logs")]
        public async Task TestLoginAtivoEInativoAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestLoginAtivoEInativoAsync));
            var controller = new UsuariosController(null, dbContext);

            var loginAtivo = await controller.LoginAsync(new LoginRequest { Nome = "admin", Senha = "Admin@123" }) as ObjectResult;
            var loginInativo = await controller.LoginAsync(new LoginRequest { Nome = "inativo.teste", Senha = "Inativo@123" }) as ObjectResult;

            Assert.Equal(200, loginAtivo?.StatusCode);
            Assert.Equal(401, loginInativo?.StatusCode);
            Assert.True(await dbContext.Logs.AnyAsync(l => l.Acao == "LOGIN_SUCESSO"));
            Assert.True(await dbContext.Logs.AnyAsync(l => l.Acao == "LOGIN_BLOQUEADO"));
        }

        [Fact(DisplayName = "Logs GET: Deve listar histórico de auditoria com filtros")]
        public async Task TestGetLogsAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestGetLogsAsync));
            var usuariosController = new UsuariosController(null, dbContext);
            var logsController = new LogsController(null, dbContext);

            await usuariosController.LoginAsync(new LoginRequest { Nome = "admin", Senha = "Admin@123" });

            var response = await logsController.GetLogsAsync(entidade: "Sessao", acao: "LOGIN_SUCESSO") as ObjectResult;
            var value = response?.Value as IPagedResponse<LogAuditoriaResponse>;

            Assert.NotNull(value);
            Assert.False(value!.DidError);
            Assert.True(value.ItemsCount >= 1);
        }
    }
}