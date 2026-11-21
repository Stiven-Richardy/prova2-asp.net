using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prova2.API.Models;

namespace Prova2.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController>? _logger;
        private readonly Prova2DbContext _dbContext;

        public LogsController(ILogger<LogsController>? logger, Prova2DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetLogsAsync(
            int pageSize = 100,
            int pageNumber = 1,
            string? entidade = null,
            string? acao = null,
            string? origem = null,
            string? busca = null)
        {
            _logger?.LogDebug("'{0}' foi invocado", nameof(GetLogsAsync));
            var response = new PagedResponse<LogAuditoriaResponse>();

            try
            {
                var query = _dbContext.GetLogs(entidade, acao, origem, busca);

                response.PageSize = pageSize;
                response.PageNumber = pageNumber;
                response.ItemsCount = await query.CountAsync();

                var lista = await query.Paging(pageSize, pageNumber).ToListAsync();
                response.Model = lista.Select(l => l.ToResponse()).ToList();
                response.Message = $"Total de eventos registrados: {response.ItemsCount}.";
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.ErrorMessage = "Erro interno ao consultar logs de auditoria.";
                _logger?.LogCritical("Erro em '{0}': {1}", nameof(GetLogsAsync), ex);
            }

            return response.ToHttpResponse();
        }
    }
}