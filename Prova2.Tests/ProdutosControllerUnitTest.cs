using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prova2.API.Controllers;
using Prova2.API.Models;
using Xunit;

namespace Prova2.Tests
{
    public class ProdutosControllerUnitTest
    {
        [Fact(DisplayName = "Produtos GET: Deve retornar lista paginada de produtos com auditoria")]
        public async Task TestGetProdutosAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestGetProdutosAsync));
            var controller = new ProdutosController(null, dbContext);

            var response = await controller.GetProdutosAsync() as ObjectResult;
            var value = response?.Value as IPagedResponse<ProdutoResponse>;

            Assert.NotNull(value);
            Assert.False(value!.DidError);
            Assert.True(value.ItemsCount >= 2);
        }

        [Fact(DisplayName = "Produtos POST: Deve cadastrar produto gravando IdUsuarioCadastro")]
        public async Task TestPostProdutoAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPostProdutoAsync));
            var controller = new ProdutosController(null, dbContext);

            var request = new PostProdutoRequest
            {
                Nome = "Mouse Wireless Logitech",
                Preco = 189.90f,
                Status = true,
                IdUsuarioCadastro = 1
            };

            var response = await controller.PostProdutoAsync(request) as ObjectResult;
            var value = response?.Value as ISingleResponse<ProdutoResponse>;

            Assert.NotNull(value);
            Assert.Equal(201, response!.StatusCode);
            Assert.False(value!.DidError);
            Assert.Equal(1, value.Model?.IdUsuarioCadastro);
            Assert.Null(value.Model?.IdUsuarioUpdate);
        }

        [Fact(DisplayName = "Produtos PUT: Deve inativar produto e registrar IdUsuarioUpdate")]
        public async Task TestPutProdutoStatusEAuditoriaAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPutProdutoStatusEAuditoriaAsync));
            var controller = new ProdutosController(null, dbContext);

            var request = new PutProdutoRequest
            {
                Nome = "Notebook Lenovo IdeaPad i5",
                Preco = 3499.90f,
                Status = false,
                IdUsuarioUpdate = 2
            };

            var response = await controller.PutProdutoAsync(1, request) as ObjectResult;
            var value = response?.Value as ISingleResponse<ProdutoResponse>;

            Assert.NotNull(value);
            Assert.Equal(200, response!.StatusCode);
            Assert.False(value!.DidError);
            Assert.False(value.Model!.Status);
            Assert.Equal(2, value.Model.IdUsuarioUpdate);
            Assert.Equal("operador", value.Model.NomeUsuarioUpdate);
        }

        [Fact(DisplayName = "Produtos POST: Deve bloquear cadastro por usuário inativo")]
        public async Task TestPostProdutoUsuarioInativoAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestPostProdutoUsuarioInativoAsync));
            var controller = new ProdutosController(null, dbContext);

            var request = new PostProdutoRequest
            {
                Nome = "Produto Bloqueado",
                Preco = 50.0f,
                Status = true,
                IdUsuarioCadastro = 3
            };

            var response = await controller.PostProdutoAsync(request) as ObjectResult;
            Assert.Equal(400, response?.StatusCode);
        }

        [Fact(DisplayName = "Produtos DELETE: Deve excluir produto existente")]
        public async Task TestDeleteProdutoAsync()
        {
            using var dbContext = DbContextMocker.GetProva2DbContext(nameof(TestDeleteProdutoAsync));
            var controller = new ProdutosController(null, dbContext);

            var response = await controller.DeleteProdutoAsync(2) as ObjectResult;
            var value = response?.Value as IResponse;

            Assert.NotNull(value);
            Assert.Equal(200, response!.StatusCode);
            Assert.False(value!.DidError);
        }
    }
}