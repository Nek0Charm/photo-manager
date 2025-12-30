using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class McpController : ControllerBase
    {
        private readonly IMcpSearchService _mcpSearchService;

        public McpController(IMcpSearchService mcpSearchService)
        {
            _mcpSearchService = mcpSearchService;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] McpSearchRequest request, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { message = "请输入查询内容" });
            }

            var response = await _mcpSearchService.SearchAsync(request, userId.Value, cancellationToken);
            return Ok(response);
        }
    }
}
