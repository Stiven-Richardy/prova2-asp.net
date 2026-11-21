using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prova2.API.Models
{
    public static class PasswordHelper
    {
        public static string GerarHash(string senha)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(senha));
            return Convert.ToHexString(bytes);
        }

        public static bool VerificarSenha(string senhaDigitada, string hashArmazenado)
        {
            var hashDigitado = GerarHash(senhaDigitada);
            return string.Equals(hashDigitado, hashArmazenado, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ValidarSenhaForte(string? senha, out List<string> erros)
        {
            erros = new List<string>();

            if (string.IsNullOrEmpty(senha) || senha.Length < 8)
                erros.Add("mínimo de 8 caracteres");

            if (string.IsNullOrEmpty(senha) || !senha.Any(char.IsUpper))
                erros.Add("pelo menos 1 letra maiúscula");

            if (string.IsNullOrEmpty(senha) || !senha.Any(char.IsLower))
                erros.Add("pelo menos 1 letra minúscula");

            if (string.IsNullOrEmpty(senha) || !senha.Any(char.IsDigit))
                erros.Add("pelo menos 1 número");

            if (string.IsNullOrEmpty(senha) || !senha.Any(c => !char.IsLetterOrDigit(c)))
                erros.Add("pelo menos 1 caractere especial");

            return erros.Count == 0;
        }
    }

    public class SenhaForteAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string senha || string.IsNullOrWhiteSpace(senha))
                return ValidationResult.Success;

            if (!PasswordHelper.ValidarSenhaForte(senha, out var erros))
            {
                return new ValidationResult($"A senha não atende aos critérios de segurança: {string.Join(", ", erros)}.");
            }

            return ValidationResult.Success;
        }
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public bool Status { get; set; } = true;

        public ICollection<Produto> ProdutosCadastrados { get; set; } = new List<Produto>();
        public ICollection<Produto> ProdutosAtualizados { get; set; } = new List<Produto>();
    }

    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public float Preco { get; set; }
        public bool Status { get; set; } = true;
        public int IdUsuarioCadastro { get; set; }
        public int? IdUsuarioUpdate { get; set; }

        public Usuario? UsuarioCadastro { get; set; }
        public Usuario? UsuarioUpdate { get; set; }
    }

    public class LogAuditoria
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string Acao { get; set; } = string.Empty;
        public string Entidade { get; set; } = string.Empty;
        public int? EntidadeId { get; set; }
        public string Origem { get; set; } = "API";
        public string UsuarioResponsavel { get; set; } = "Sistema";
        public string Detalhes { get; set; } = string.Empty;
    }

    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nome)
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            builder.HasIndex(u => u.Nome)
                .IsUnique();

            builder.Property(u => u.Senha)
                .HasColumnType("nvarchar(128)")
                .IsRequired();

            builder.Property(u => u.Status)
                .IsRequired();
        }
    }

    public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
    {
        public void Configure(EntityTypeBuilder<Produto> builder)
        {
            builder.ToTable("Produtos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Nome)
                .HasColumnType("nvarchar(150)")
                .IsRequired();

            builder.Property(p => p.Preco)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.IdUsuarioCadastro)
                .IsRequired();

            builder.Property(p => p.IdUsuarioUpdate)
                .IsRequired(false);

            builder.HasOne(p => p.UsuarioCadastro)
                .WithMany(u => u.ProdutosCadastrados)
                .HasForeignKey(p => p.IdUsuarioCadastro)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.UsuarioUpdate)
                .WithMany(u => u.ProdutosAtualizados)
                .HasForeignKey(p => p.IdUsuarioUpdate)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
    {
        public void Configure(EntityTypeBuilder<LogAuditoria> builder)
        {
            builder.ToTable("Logs");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.DataHora).IsRequired();
            builder.Property(l => l.Acao).HasColumnType("nvarchar(40)").IsRequired();
            builder.Property(l => l.Entidade).HasColumnType("nvarchar(40)").IsRequired();
            builder.Property(l => l.EntidadeId).IsRequired(false);
            builder.Property(l => l.Origem).HasColumnType("nvarchar(30)").IsRequired();
            builder.Property(l => l.UsuarioResponsavel).HasColumnType("nvarchar(100)").IsRequired();
            builder.Property(l => l.Detalhes).HasColumnType("nvarchar(400)").IsRequired();

            builder.HasIndex(l => l.DataHora);
        }
    }

    public class Prova2DbContext : DbContext
    {
        public Prova2DbContext(DbContextOptions<Prova2DbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<LogAuditoria> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
            modelBuilder.ApplyConfiguration(new ProdutoConfiguration());
            modelBuilder.ApplyConfiguration(new LogAuditoriaConfiguration());

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nome = "admin",
                    Senha = PasswordHelper.GerarHash("Admin@123"),
                    Status = true
                },
                new Usuario
                {
                    Id = 2,
                    Nome = "operador",
                    Senha = PasswordHelper.GerarHash("Operador@123"),
                    Status = true
                }
            );

            modelBuilder.Entity<Produto>().HasData(
                new Produto
                {
                    Id = 1,
                    Nome = "Notebook Lenovo IdeaPad i5",
                    Preco = 3499.90f,
                    Status = true,
                    IdUsuarioCadastro = 1,
                    IdUsuarioUpdate = null
                },
                new Produto
                {
                    Id = 2,
                    Nome = "Monitor LG 24\" Full HD IPS",
                    Preco = 849.50f,
                    Status = true,
                    IdUsuarioCadastro = 1,
                    IdUsuarioUpdate = 2
                }
            );

            modelBuilder.Entity<LogAuditoria>().HasData(
                new LogAuditoria
                {
                    Id = 1,
                    DataHora = new DateTime(2026, 11, 21, 9, 0, 0),
                    Acao = "INICIALIZACAO",
                    Entidade = "Sistema",
                    EntidadeId = null,
                    Origem = "Sistema",
                    UsuarioResponsavel = "Sistema",
                    Detalhes = "Base de dados inicializada com registros padrão."
                }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}