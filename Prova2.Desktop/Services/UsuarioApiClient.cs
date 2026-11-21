using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Prova2.Desktop.Models;

namespace Prova2.Desktop.Services
{
    public class UsuarioApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public string SwaggerUrl { get; }

        public UsuarioApiClient(string baseUrl = "http://localhost:5100")
        {
            var normalizedUrl = baseUrl.TrimEnd('/');
            SwaggerUrl = $"{normalizedUrl}/swagger";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(normalizedUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            _httpClient.DefaultRequestHeaders.Add("X-Client-Origin", "Desktop");
            _httpClient.DefaultRequestHeaders.Add("X-User-Actor", "Administrador (Desktop)");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<(bool Sucesso, List<UsuarioDto> Dados, string Mensagem)> ListarAsync(string? filtroNome = null, bool? filtroStatus = null)
        {
            try
            {
                var query = "/api/v1/Usuarios?pageSize=200&pageNumber=1";
                if (!string.IsNullOrWhiteSpace(filtroNome))
                    query += $"&nome={Uri.EscapeDataString(filtroNome.Trim())}";
                if (filtroStatus.HasValue)
                    query += $"&status={filtroStatus.Value.ToString().ToLower()}";

                var response = await _httpClient.GetAsync(query);
                var body = await response.Content.ReadFromJsonAsync<ApiPagedResponse<UsuarioDto>>(_jsonOptions);

                if (response.IsSuccessStatusCode && body != null && !body.DidError)
                {
                    return (true, body.Model ?? new List<UsuarioDto>(), "Lista atualizada.");
                }

                return (false, new List<UsuarioDto>(), body?.ErrorMessage ?? "Não foi possível carregar os usuários.");
            }
            catch (HttpRequestException)
            {
                return (false, new List<UsuarioDto>(), "Serviço temporariamente indisponível. Verifique se o servidor está ativo.");
            }
            catch (Exception)
            {
                return (false, new List<UsuarioDto>(), "Falha inesperada ao consultar os dados.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> CriarAsync(CriarUsuarioPayload payload)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/v1/Usuarios", payload);
                return await ProcessarRetornoSimplesAsync(response, "Usuário cadastrado com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> AtualizarAsync(int id, AtualizarUsuarioPayload payload)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/v1/Usuarios/{id}", payload);
                return await ProcessarRetornoSimplesAsync(response, "Usuário atualizado com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> ExcluirAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/v1/Usuarios/{id}");
                return await ProcessarRetornoSimplesAsync(response, "Usuário excluído com sucesso.");
            }
            catch (HttpRequestException)
            {
                return (false, "Serviço temporariamente indisponível. Tente novamente em instantes.");
            }
        }

        private async Task<(bool Sucesso, string Mensagem)> ProcessarRetornoSimplesAsync(HttpResponseMessage response, string mensagemPadrao)
        {
            var texto = await response.Content.ReadAsStringAsync();

            try
            {
                var envelope = JsonSerializer.Deserialize<ApiResponse>(texto, _jsonOptions);
                if (response.IsSuccessStatusCode && (envelope == null || !envelope.DidError))
                {
                    return (true, envelope?.Message ?? mensagemPadrao);
                }

                if (!string.IsNullOrWhiteSpace(envelope?.ErrorMessage))
                {
                    return (false, envelope.ErrorMessage);
                }

                using var doc = JsonDocument.Parse(texto);
                if (doc.RootElement.TryGetProperty("errors", out var errorsElement))
                {
                    var mensagens = new List<string>();
                    foreach (var prop in errorsElement.EnumerateObject())
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            var msg = item.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                mensagens.Add(msg);
                        }
                    }

                    if (mensagens.Count > 0)
                        return (false, string.Join("\n", mensagens));
                }
            }
            catch { }

            return (false, "Não foi possível concluir a operação. Verifique os dados informados.");
        }
    }
}