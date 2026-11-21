using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prova2.Web.Models;
using Prova2.Web.Services;

namespace Prova2.Web.Controllers
{
    [Authorize]
    public class ProdutosController : Controller
    {
        private readonly Prova2ApiClient _apiClient;

        public ProdutosController(Prova2ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private int ObterIdUsuarioLogado()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // GET: / ou /Produtos
        public async Task<IActionResult> Index(string? busca = null, string? filtroStatus = null)
        {
            bool? statusBool = filtroStatus switch
            {
                "ativos" => true,
                "inativos" => false,
                _ => null
            };

            var (sucesso, produtosFiltrados, mensagem) = await _apiClient.ListarProdutosAsync(busca, statusBool);
            if (!sucesso)
            {
                TempData["Erro"] = mensagem;
            }

            var todos = (string.IsNullOrWhiteSpace(busca) && !statusBool.HasValue)
                ? produtosFiltrados
                : (await _apiClient.ListarProdutosAsync()).Produtos;

            var vm = new ProdutosIndexViewModel
            {
                Produtos = produtosFiltrados,
                TotalProdutos = todos.Count,
                TotalAtivos = todos.Count(p => p.Status),
                TotalInativos = todos.Count(p => !p.Status),
                ValorTotalAtivos = todos.Where(p => p.Status).Sum(p => p.Preco),
                BuscaAtual = busca,
                StatusAtual = filtroStatus ?? "todos"
            };

            return View(vm);
        }

        // GET: /Produtos/ExportarCsv
        [HttpGet]
        public async Task<IActionResult> ExportarCsv(string? busca = null, string? filtroStatus = null)
        {
            bool? statusBool = filtroStatus switch
            {
                "ativos" => true,
                "inativos" => false,
                _ => null
            };

            var (_, produtos, _) = await _apiClient.ListarProdutosAsync(busca, statusBool);
            var cultura = new CultureInfo("pt-BR");

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nome;Preco;Status;CadastradoPor;UltimaAlteracao");

            foreach (var p in produtos)
            {
                var nomeEscapado = $"\"{p.Nome.Replace("\"", "\"\"")}\"";
                var statusTxt = p.Status ? "Ativo" : "Inativo";
                var cadastroTxt = $"\"{p.NomeUsuarioCadastro} (#{p.IdUsuarioCadastro})\"";
                var updateTxt = p.IdUsuarioUpdate.HasValue
                    ? $"\"{p.NomeUsuarioUpdate} (#{p.IdUsuarioUpdate})\""
                    : "\"Sem alterações\"";

                sb.AppendLine($"{p.Id};{nomeEscapado};{p.Preco.ToString("F2", cultura)};{statusTxt};{cadastroTxt};{updateTxt}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"produtos_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // GET: /Produtos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var (sucesso, produto, mensagem) = await _apiClient.ObterProdutoPorIdAsync(id);
            if (!sucesso || produto == null)
            {
                TempData["Erro"] = mensagem;
                return RedirectToAction(nameof(Index));
            }

            return View(produto);
        }

        // GET: /Produtos/Create
        public IActionResult Create()
        {
            return View(new ProdutoFormViewModel { Status = true });
        }

        // POST: /Produtos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProdutoFormViewModel model)
        {
            if (!model.TryParsePreco(out var precoFloat))
            {
                ModelState.AddModelError(nameof(model.PrecoTexto), "Informe um preço numérico válido maior que zero (ex: 149,90).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var idUsuarioLogado = ObterIdUsuarioLogado();
            var (sucesso, mensagem) = await _apiClient.CriarProdutoAsync(model.Nome, precoFloat, model.Status, idUsuarioLogado);

            if (!sucesso)
            {
                ModelState.AddModelError(string.Empty, mensagem);
                return View(model);
            }

            TempData["Sucesso"] = mensagem;
            return RedirectToAction(nameof(Index));
        }

        // GET: /Produtos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var (sucesso, produto, mensagem) = await _apiClient.ObterProdutoPorIdAsync(id);
            if (!sucesso || produto == null)
            {
                TempData["Erro"] = mensagem;
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new ProdutoFormViewModel
            {
                Id = produto.Id,
                Nome = produto.Nome,
                PrecoTexto = produto.Preco.ToString("F2", new CultureInfo("pt-BR")),
                Status = produto.Status
            };

            ViewData["AuditoriaCadastro"] = $"{produto.NomeUsuarioCadastro} (#{produto.IdUsuarioCadastro})";
            ViewData["AuditoriaUpdate"] = produto.IdUsuarioUpdate.HasValue
                ? $"{produto.NomeUsuarioUpdate} (#{produto.IdUsuarioUpdate})"
                : "Nenhuma alteração registrada";

            return View(viewModel);
        }

        // POST: /Produtos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProdutoFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!model.TryParsePreco(out var precoFloat))
            {
                ModelState.AddModelError(nameof(model.PrecoTexto), "Informe um preço numérico válido maior que zero (ex: 149,90).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var idUsuarioLogado = ObterIdUsuarioLogado();
            var (sucesso, mensagem) = await _apiClient.AtualizarProdutoAsync(id, model.Nome, precoFloat, model.Status, idUsuarioLogado);

            if (!sucesso)
            {
                ModelState.AddModelError(string.Empty, mensagem);
                return View(model);
            }

            TempData["Sucesso"] = mensagem;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Produtos/AlternarStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarStatus(int id)
        {
            var (encontrou, produto, msgBusca) = await _apiClient.ObterProdutoPorIdAsync(id);
            if (!encontrou || produto == null)
            {
                TempData["Erro"] = msgBusca;
                return RedirectToAction(nameof(Index));
            }

            var idUsuarioLogado = ObterIdUsuarioLogado();
            var novoStatus = !produto.Status;

            var (sucesso, mensagem) = await _apiClient.AtualizarProdutoAsync(
                id,
                produto.Nome,
                produto.Preco,
                novoStatus,
                idUsuarioLogado);

            if (sucesso)
                TempData["Sucesso"] = $"Status do produto #{id} alterado para {(novoStatus ? "Ativo" : "Inativo")}.";
            else
                TempData["Erro"] = mensagem;

            return RedirectToAction(nameof(Index));
        }

        // GET: /Produtos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var (sucesso, produto, mensagem) = await _apiClient.ObterProdutoPorIdAsync(id);
            if (!sucesso || produto == null)
            {
                TempData["Erro"] = mensagem;
                return RedirectToAction(nameof(Index));
            }

            return View(produto);
        }

        // POST: /Produtos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (sucesso, mensagem) = await _apiClient.ExcluirProdutoAsync(id, User.Identity?.Name);
            if (sucesso)
                TempData["Sucesso"] = mensagem;
            else
                TempData["Erro"] = mensagem;

            return RedirectToAction(nameof(Index));
        }
    }
}