using System;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/ai-settings")]
    public class AiSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AiSettingsController> _logger;

        public AiSettingsController(AppDbContext context, ILogger<AiSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        [HttpGet]
        public async Task<ActionResult<AiSettingsResponse>> Get()
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var settings = await _context.UserAiSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            return Ok(ToResponse(settings));
        }

        [HttpPost]
        public async Task<ActionResult<AiSettingsResponse>> Save([FromBody] AiSettingsUpsertRequest request)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "未登录" });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var settings = await _context.UserAiSettings.FirstOrDefaultAsync(s => s.UserId == userId.Value);
            if (settings == null)
            {
                settings = new UserAiSetting
                {
                    UserId = userId.Value
                };
                _context.UserAiSettings.Add(settings);
            }

            var normalizedModel = string.IsNullOrWhiteSpace(request.Model)
                ? "gpt-4o-mini"
                : request.Model.Trim();

            settings.Provider = "OpenAI";
            settings.Model = normalizedModel;
            settings.Endpoint = string.IsNullOrWhiteSpace(request.Endpoint)
                ? null
                : request.Endpoint.Trim();

            var mustUpdateKey = request.UpdateApiKey || string.IsNullOrWhiteSpace(settings.ApiKey);
            if (mustUpdateKey)
            {
                var incomingKey = request.ApiKey?.Trim();
                if (string.IsNullOrWhiteSpace(incomingKey))
                {
                    return BadRequest(new { message = "请提供 API Key" });
                }

                settings.ApiKey = incomingKey;
            }

            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated AI settings", userId);

            return Ok(ToResponse(settings));
        }

        private static AiSettingsResponse ToResponse(UserAiSetting? settings)
        {
            if (settings == null)
            {
                return new AiSettingsResponse
                {
                    Provider = "OpenAI",
                    Model = "gpt-4o-mini",
                    Endpoint = null,
                    HasApiKey = false,
                    UpdatedAt = null
                };
            }

            return new AiSettingsResponse
            {
                Provider = settings.Provider,
                Model = settings.Model,
                Endpoint = settings.Endpoint,
                HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey),
                UpdatedAt = settings.UpdatedAt
            };
        }
    }
}
