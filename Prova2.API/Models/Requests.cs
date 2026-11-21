using System.ComponentModel.DataAnnotations;

namespace Prova2.API.Models
{
    public class PostUsuarioRequest
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(64, ErrorMessage = "A senha deve ter no máximo 64 caracteres.")]
        [SenhaForte]
        public string Senha { get; set; } = string.Empty;

        public bool Status { get; set; } = true;
    }

    public class PutUsuarioRequest
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(64, ErrorMessage = "A senha deve ter no máximo 64 caracteres.")]
        [SenhaForte]
        public string? Senha { get; set; }

        [Required]
        public bool Status { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Informe o nome do usuário.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        public string Senha { get; set; } = string.Empty;
    }

    public class PostProdutoRequest
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome do produto deve ter entre 2 e 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Range(0.01, 10000000, ErrorMessage = "O preço deve ser maior que zero.")]
        public float Preco { get; set; }

        public bool Status { get; set; } = true;

        [Required(ErrorMessage = "O usuário responsável pelo cadastro é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Usuário de cadastro inválido.")]
        public int IdUsuarioCadastro { get; set; }
    }

    public class PutProdutoRequest
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome do produto deve ter entre 2 e 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Range(0.01, 10000000, ErrorMessage = "O preço deve ser maior que zero.")]
        public float Preco { get; set; }

        [Required]
        public bool Status { get; set; }

        [Required(ErrorMessage = "O usuário responsável pela alteração é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Usuário de atualização inválido.")]
        public int IdUsuarioUpdate { get; set; }
    }

    public static class RequestExtensions
    {
        public static Usuario ToEntity(this PostUsuarioRequest request)
            => new Usuario
            {
                Nome = request.Nome.Trim(),
                Senha = PasswordHelper.GerarHash(request.Senha),
                Status = request.Status
            };

        public static Produto ToEntity(this PostProdutoRequest request)
            => new Produto
            {
                Nome = request.Nome.Trim(),
                Preco = request.Preco,
                Status = request.Status,
                IdUsuarioCadastro = request.IdUsuarioCadastro,
                IdUsuarioUpdate = null
            };
    }
}