using System.Collections.Generic;

namespace Prova2.Desktop.Models
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Status { get; set; }

        public string StatusTexto => Status ? "Ativo" : "Inativo";
    }

    public class ApiResponse
    {
        public string? Message { get; set; }
        public bool DidError { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ApiSingleResponse<T> : ApiResponse
    {
        public T? Model { get; set; }
    }

    public class ApiPagedResponse<T> : ApiResponse
    {
        public List<T>? Model { get; set; }
        public int ItemsCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

    public class CriarUsuarioPayload
    {
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }

    public class AtualizarUsuarioPayload
    {
        public string Nome { get; set; } = string.Empty;
        public string? Senha { get; set; }
        public bool Status { get; set; }
    }
}