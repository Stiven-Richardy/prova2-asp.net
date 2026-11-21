using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Prova2.Web.Models;

namespace Prova2.Web.Services
{
    public class Prova2ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Prova2ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool Sucesso, UsuarioSessionDto? Usuario, string Mensagem)> AutenticarAsync(string nome, string senha)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/v1/Usuarios/login", new { Nome = nome, Senha = senha });
                var body = await response.Content.ReadFromJsonAsync<ApiSingleResponse<UsuarioSessionDto>>(_jsonOptions);

                if (response.IsSuccessStatusCode && body?.Model != null && !body.DidError)
                {
                    return (true, body.Model, body.Message ?? "Login efetuado.");
                }

                return (false, null, body?.ErrorMessage ?? "Credenciais inválidas ou acesso não autorizado.");
            }
            catch (HttpRequestException)
            {
                return (false, null, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<bool> UsuarioContinuaAtivoAsync(int idUsuario)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/Usuarios/{idUsuario}");
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return false;

                if (!response.IsSuccessStatusCode)
                    return true;

                var body = await response.Content.ReadFromJsonAsync<ApiSingleResponse<UsuarioSessionDto>>(_jsonOptions);
                return body?.Model?.Status == true;
            }
            catch
            {
                return true;
            }
        }

        public async Task<(bool Sucesso, List<ProdutoItemDto> Produtos, string Mensagem)> ListarProdutosAsync(string? filtroNome = null, bool? filtroStatus = null)
        {
            try
            {
                var url = "/api/v1/Produtos?pageSize=200&pageNumber=1";
                if (!string.IsNullOrWhiteSpace(filtroNome))
                    url += $"&nome={Uri.EscapeDataString(filtroNome.Trim())}";
                if (filtroStatus.HasValue)
                    url += $"&status={filtroStatus.Value.ToString().ToLower()}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ProdutoItemDto>>(_jsonOptions);

                if (response.IsSuccessStatusCode && body != null && !body.DidError)
                {
                    return (true, body.Model ?? new List<ProdutoItemDto>(), body.Message ?? "Produtos carregados.");
                }

                return (false, new List<ProdutoItemDto>(), body?.ErrorMessage ?? "Não foi possível carregar os produtos.");
            }
            catch (HttpRequestException)
            {
                return (false, new List<ProdutoItemDto>(), "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, ProdutoItemDto? Produto, string Mensagem)> ObterProdutoPorIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/Produtos/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, $"Produto #{id} não encontrado.");
                }

                var body = await response.Content.ReadFromJsonAsync<ApiSingleResponse<ProdutoItemDto>>(_jsonOptions);
                return (body?.Model != null, body?.Model, body?.Message ?? string.Empty);
            }
            catch (HttpRequestException)
            {
                return (false, null, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> CriarProdutoAsync(string nome, float preco, bool status, int idUsuarioCadastro)
        {
            try
            {
                var payload = new
                {
                    Nome = nome,
                    Preco = preco,
                    Status = status,
                    IdUsuarioCadastro = idUsuarioCadastro
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/Produtos", payload);
                return await LerEnvelopeAsync(response, "Produto cadastrado com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> AtualizarProdutoAsync(int id, string nome, float preco, bool status, int idUsuarioUpdate)
        {
            try
            {
                var payload = new
                {
                    Nome = nome,
                    Preco = preco,
                    Status = status,
                    IdUsuarioUpdate = idUsuarioUpdate
                };

                var response = await _httpClient.PutAsJsonAsync($"/api/v1/Produtos/{id}", payload);
                return await LerEnvelopeAsync(response, "Produto atualizado com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> ExcluirProdutoAsync(int id, string? usuarioResponsavel = null)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/Produtos/{id}");
                if (!string.IsNullOrWhiteSpace(usuarioResponsavel))
                {
                    req.Headers.Add("X-User-Actor", usuarioResponsavel);
                }

                var response = await _httpClient.SendAsync(req);
                return await LerEnvelopeAsync(response, "Produto excluído com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, List<LogItemDto> Logs, string Mensagem)> ListarLogsAsync(
            string? entidade = null,
            string? acao = null,
            string? origem = null,
            string? busca = null)
        {
            try
            {
                var url = "/api/v1/Logs?pageSize=200&pageNumber=1";
                if (!string.IsNullOrWhiteSpace(entidade))
                    url += $"&entidade={Uri.EscapeDataString(entidade)}";
                if (!string.IsNullOrWhiteSpace(acao))
                    url += $"&acao={Uri.EscapeDataString(acao)}";
                if (!string.IsNullOrWhiteSpace(origem))
                    url += $"&origem={Uri.EscapeDataString(origem)}";
                if (!string.IsNullOrWhiteSpace(busca))
                    url += $"&busca={Uri.EscapeDataString(busca.Trim())}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadFromJsonAsync<ApiPagedResponse<LogItemDto>>(_jsonOptions);

                if (response.IsSuccessStatusCode && body != null && !body.DidError)
                {
                    return (true, body.Model ?? new List<LogItemDto>(), body.Message ?? "Logs carregados.");
                }

                return (false, new List<LogItemDto>(), body?.ErrorMessage ?? "Não foi possível carregar o histórico de logs.");
            }
            catch (HttpRequestException)
            {
                return (false, new List<LogItemDto>(), "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        private async Task<(bool Sucesso, string Mensagem)> LerEnvelopeAsync(HttpResponseMessage response, string msgPadrao)
        {
            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                var envelope = JsonSerializer.Deserialize<ApiResponse>(raw, _jsonOptions);
                if (response.IsSuccessStatusCode && (envelope == null || !envelope.DidError))
                {
                    return (true, envelope?.Message ?? msgPadrao);
                }

                if (!string.IsNullOrWhiteSpace(envelope?.ErrorMessage))
                {
                    return (false, envelope.ErrorMessage);
                }
            }
            catch { }

            return (false, "Não foi possível processar a solicitação. Verifique os dados informados.");
        }
    }
}