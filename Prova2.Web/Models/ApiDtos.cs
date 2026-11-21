using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Prova2.Web.Models
{
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

    public class UsuarioSessionDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Status { get; set; }
    }

    public class ProdutoItemDto
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

    public class LogItemDto
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

    public class ProdutosIndexViewModel
    {
        public List<ProdutoItemDto> Produtos { get; set; } = new();
        public int TotalProdutos { get; set; }
        public int TotalAtivos { get; set; }
        public int TotalInativos { get; set; }
        public float ValorTotalAtivos { get; set; }
        public string? BuscaAtual { get; set; }
        public string StatusAtual { get; set; } = "todos";
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o nome de usuário.")]
        [Display(Name = "Usuário")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;
    }

    public class ProdutoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 150 caracteres.")]
        [Display(Name = "Nome do Produto")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O preço unitário é obrigatório.")]
        [Display(Name = "Preço Unitário (R$)")]
        public string PrecoTexto { get; set; } = string.Empty;

        [Display(Name = "Produto Ativo")]
        public bool Status { get; set; } = true;

        public bool TryParsePreco(out float valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(PrecoTexto))
                return false;

            var texto = PrecoTexto.Trim().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (texto.Contains(',') && texto.Contains('.'))
            {
                texto = texto.Replace(".", "").Replace(",", ".");
            }
            else if (texto.Contains(','))
            {
                texto = texto.Replace(",", ".");
            }

            return float.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out valor)
                   && !float.IsNaN(valor)
                   && !float.IsInfinity(valor)
                   && valor > 0f;
        }
    }
}