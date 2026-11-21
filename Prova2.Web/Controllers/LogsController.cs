using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prova2.Web.Services;

namespace Prova2.Web.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly Prova2ApiClient _apiClient;

        public LogsController(Prova2ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index(
            string? entidade = null,
            string? acao = null,
            string? origem = null,
            string? busca = null)
        {
            var (sucesso, logs, mensagem) = await _apiClient.ListarLogsAsync(entidade, acao, origem, busca);
            if (!sucesso)
            {
                TempData["Erro"] = mensagem;
            }

            ViewData["EntidadeAtual"] = entidade ?? "";
            ViewData["AcaoAtual"] = acao ?? "";
            ViewData["OrigemAtual"] = origem ?? "";
            ViewData["BuscaAtual"] = busca ?? "";

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarCsv(
            string? entidade = null,
            string? acao = null,
            string? origem = null,
            string? busca = null)
        {
            var (_, logs, _) = await _apiClient.ListarLogsAsync(entidade, acao, origem, busca);

            var sb = new StringBuilder();
            sb.AppendLine("ID;DataHora;Acao;Entidade;EntidadeId;Origem;Responsavel;Detalhes");

            foreach (var l in logs)
            {
                var detalhes = $"\"{l.Detalhes.Replace("\"", "\"\"")}\"";
                var resp = $"\"{l.UsuarioResponsavel.Replace("\"", "\"\"")}\"";
                sb.AppendLine($"{l.Id};{l.DataHora:dd/MM/yyyy HH:mm:ss};{l.Acao};{l.Entidade};{l.EntidadeId};{l.Origem};{resp};{detalhes}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"logs_auditoria_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}