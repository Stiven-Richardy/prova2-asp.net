using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Prova2.API.Models
{
    public class UsuarioResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Status { get; set; }
    }

    public class ProdutoResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public float Preco { get; set; }
        public bool Status { get; set; }
        public int IdUsuarioCadastro { get; set; }
        public string NomeUsuarioCadastro { get; set; } = string.Empty;
        public int? IdUsuarioUpdate { get; set; }
        public string? NomeUsuarioUpdate { get; set; }
    }

    public class LogAuditoriaResponse
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Acao { get; set; } = string.Empty;
        public string Entidade { get; set; } = string.Empty;
        public int? EntidadeId { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string UsuarioResponsavel { get; set; } = string.Empty;
        public string Detalhes { get; set; } = string.Empty;
    }

    public static class EntityResponseExtensions
    {
        public static UsuarioResponse ToResponse(this Usuario usuario)
            => new UsuarioResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Status = usuario.Status
            };

        public static ProdutoResponse ToResponse(this Produto produto)
            => new ProdutoResponse
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Preco = produto.Preco,
                Status = produto.Status,
                IdUsuarioCadastro = produto.IdUsuarioCadastro,
                NomeUsuarioCadastro = produto.UsuarioCadastro?.Nome ?? $"Usuário #{produto.IdUsuarioCadastro}",
                IdUsuarioUpdate = produto.IdUsuarioUpdate,
                NomeUsuarioUpdate = produto.IdUsuarioUpdate.HasValue
                    ? (produto.UsuarioUpdate?.Nome ?? $"Usuário #{produto.IdUsuarioUpdate.Value}")
                    : null
            };

        public static LogAuditoriaResponse ToResponse(this LogAuditoria log)
            => new LogAuditoriaResponse
            {
                Id = log.Id,
                DataHora = log.DataHora,
                Acao = log.Acao,
                Entidade = log.Entidade,
                EntidadeId = log.EntidadeId,
                Origem = log.Origem,
                UsuarioResponsavel = log.UsuarioResponsavel,
                Detalhes = log.Detalhes
            };
    }

    public interface IResponse
    {
        string? Message { get; set; }
        bool DidError { get; set; }
        string? ErrorMessage { get; set; }
    }

    public interface ISingleResponse<TModel> : IResponse
    {
        TModel? Model { get; set; }
    }

    public interface IListResponse<TModel> : IResponse
    {
        IEnumerable<TModel>? Model { get; set; }
    }

    public interface IPagedResponse<TModel> : IListResponse<TModel>
    {
        int ItemsCount { get; set; }
        double PageCount { get; }
    }

    public class Response : IResponse
    {
        public string? Message { get; set; }
        public bool DidError { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SingleResponse<TModel> : ISingleResponse<TModel>
    {
        public string? Message { get; set; }
        public bool DidError { get; set; }
        public string? ErrorMessage { get; set; }
        public TModel? Model { get; set; }
    }

    public class ListResponse<TModel> : IListResponse<TModel>
    {
        public string? Message { get; set; }
        public bool DidError { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<TModel>? Model { get; set; }
    }

    public class PagedResponse<TModel> : IPagedResponse<TModel>
    {
        public string? Message { get; set; }
        public bool DidError { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<TModel>? Model { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int ItemsCount { get; set; }
        public double PageCount
            => PageSize <= 0 ? 1 : Math.Max(1, Math.Ceiling((double)ItemsCount / PageSize));
    }

    public static class ResponseExtensions
    {
        public static IActionResult ToHttpResponse(
            this IResponse response,
            HttpStatusCode successCode = HttpStatusCode.OK)
        {
            var status = response.DidError
                ? HttpStatusCode.InternalServerError
                : successCode;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }

        public static IActionResult ToHttpResponse<TModel>(
            this ISingleResponse<TModel> response,
            HttpStatusCode successCode = HttpStatusCode.OK)
        {
            var status = successCode;

            if (response.DidError)
                status = HttpStatusCode.InternalServerError;
            else if (response.Model == null)
                status = HttpStatusCode.NotFound;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }

        public static IActionResult ToHttpResponse<TModel>(this IListResponse<TModel> response)
        {
            var status = HttpStatusCode.OK;

            if (response.DidError)
                status = HttpStatusCode.InternalServerError;
            else if (response.Model == null)
                status = HttpStatusCode.NoContent;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }
    }
}