using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SJPCORE.Models;
using SJPCORE.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SJPCORE.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class SettingsController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public SettingsController(DapperContext context, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _context = context;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        // GET: api/settings
        [HttpGet]
        public IActionResult GetAllSettings()
        {
            using (var con = _context.CreateConnection())
            {
                var configSettings = con.Query<ConfigModel>("SELECT * FROM sjp_setting").ToList();
                var orderedconfigSettings = configSettings.OrderBy(o => o.key).ToList();
                var response = new ApiResponse<List<ConfigModel>>
                {
                    Success = true,
                    StatusCode = 200,
                    Data = orderedconfigSettings,
                    Message = "Successfully retrieved all settings."
                };
                return Ok(response);
            }
        }

        // GET: api/settings/{id}
        [HttpGet("{id}")]
        public IActionResult GetSetting(string id)
        {
            using (var con = _context.CreateConnection())
            {
                var configSetting = con.QueryFirstOrDefault<ConfigModel>("SELECT * FROM sjp_setting WHERE id = @Id", new { Id = id });
                if (configSetting == null)
                {
                    var response = new ApiResponse<ConfigModel>
                    {
                        Success = false,
                        StatusCode = 404,
                        Error = "Setting not found."
                    };
                    return NotFound(response);
                }

                var responseData = new ApiResponse<ConfigModel>
                {
                    Success = true,
                    StatusCode = 200,
                    Data = configSetting,
                    Message = "Successfully retrieved the setting."
                };
                return Ok(responseData);
            }
        }

        // POST: api/settings
        [HttpPost]
        public IActionResult CreateSetting(ConfigModel configModel)
        {
            Console.WriteLine(configModel.key + " " + configModel.value + " " + configModel.grp);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                var response = new ApiResponse<string>
                {
                    Success = false,
                    StatusCode = 400,
                    Error = "Invalid data.",
                    Message = string.Join(", ", errors)
                };
                return BadRequest(response);
            }

            try
            {
                using (var con = _context.CreateConnection())
                {
                    var sql = "INSERT INTO sjp_setting (`key`, `value`, `grp`, `update_at`) " +
                              "VALUES (@key, @value, @grp, @update_at)";
                    con.Execute(sql, configModel);
                }

                var response = new ApiResponse<ConfigModel>
                {
                    Success = true,
                    StatusCode = 201,
                    Data = configModel,
                    Message = "Setting created successfully."
                };
                return CreatedAtAction(nameof(GetSetting), new { id = configModel.id }, response);
            }
            catch (Exception ex)
            {
                var errorResponse = new ApiResponse<string>
                {
                    Success = false,
                    StatusCode = 500,
                    Error = "An error occurred while creating the setting.",
                    Message = ex.Message
                };
                return StatusCode(500, errorResponse);
            }
        }

        // PUT: api/settings
        [HttpPut]
        public async Task<IActionResult> UpdateSettingsAsync(List<ConfigModel> settings)
        {
            if (!ModelState.IsValid || settings == null || settings.Count == 0)
            {
                var response = new ApiResponse<string>
                {
                    Success = false,
                    StatusCode = 400,
                    Error = "Invalid data.",
                    Message = "Invalid data or empty settings list."
                };
                return BadRequest(response);
            }

                using (var con = _context.CreateConnection())
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        foreach (var setting in settings)
                        {
                            var existingSetting = con.QueryFirstOrDefault<ConfigModel>("SELECT * FROM sjp_setting WHERE `key` = @Key", new { Key = setting.key });
                            if (existingSetting == null)
                            {
                                transaction.Rollback(); // Rollback the transaction if any setting is not found
                                var response1 = new ApiResponse<string>
                                {
                                    Success = false,
                                    StatusCode = 404,
                                    Error = $"Setting with key '{setting.key}' not found.",
                                };
                                return NotFound(response1);
                            }

                            setting.id = existingSetting.id; // Preserve the original ID
                            setting.update_at = DateTime.Now;

                            var sql = "UPDATE sjp_setting " +
                                      "SET value = @value, update_at = @update_at " +
                                      "WHERE `key` = @key";
                            con.Execute(sql, setting, transaction);
                        }

                        transaction.Commit(); // Commit the transaction if all settings are updated successfully
                    }
                }

                var response2 = new ApiResponse<string>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Settings updated successfully."
                };
                // Restarts PuppeteerBackgroundService and EMQXClientService to apply the new settings
                var emqxClientService = _serviceProvider.GetRequiredService<EMQXClientService>();
                emqxClientService.Reconnect();
                var puppeteerBackgroundService = _serviceProvider.GetRequiredService<PuppeteerBackgroundService>();
                puppeteerBackgroundService.ReloadPageAsync();
                return Ok(response2);
        }

        // PUT: api/settings/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateSetting(int id, ConfigModel configModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                var response = new ApiResponse<string>
                {
                    Success = false,
                    StatusCode = 400,
                    Error = "Invalid data.",
                    Message = string.Join(", ", errors)
                };
                return BadRequest(response);
            }

            using (var con = _context.CreateConnection())
            {
                var existingSetting = con.QueryFirstOrDefault<ConfigModel>("SELECT * FROM sjp_setting WHERE id = @Id", new { Id = id });
                if (existingSetting == null)
                {
                    var response = new ApiResponse<string>
                    {
                        Success = false,
                        StatusCode = 404,
                        Error = "Setting not found.",
                    };
                    return NotFound(response);
                }

                configModel.id = id;
                configModel.update_at = DateTime.Now;

                try
                {
                    var sql = "UPDATE sjp_setting " +
                              "SET `key` = @key, value = @value, grp = @grp, update_at = @update_at " +
                              "WHERE id = @id";
                    con.Execute(sql, configModel);

                    var response = new ApiResponse<ConfigModel>
                    {
                        Success = true,
                        StatusCode = 200,
                        Data = configModel,
                        Message = "Setting updated successfully."
                    };
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    var errorResponse = new ApiResponse<string>
                    {
                        Success = false,
                        StatusCode = 500,
                        Error = "An error occurred while updating the setting.",
                        Message = ex.Message
                    };
                    return StatusCode(500, errorResponse);
                }
            }
        }

        // DELETE: api/settings/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteSetting(string id)
        {
            using (var con = _context.CreateConnection())
            {
                var existingSetting = con.QueryFirstOrDefault<ConfigModel>("SELECT * FROM sjp_setting WHERE id = @Id", new { Id = id });
                if (existingSetting == null)
                {
                    var response = new ApiResponse<string>
                    {
                        Success = false,
                        StatusCode = 404,
                        Error = "Setting not found.",
                    };
                    return NotFound(response);
                }

                try
                {
                    var sql = "DELETE FROM sjp_setting WHERE id = @Id";
                    con.Execute(sql, new { Id = id });

                    var response = new ApiResponse<string>
                    {
                        Success = true,
                        StatusCode = 200,
                        Message = "Setting deleted successfully."
                    };
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    var errorResponse = new ApiResponse<string>
                    {
                        Success = false,
                        StatusCode = 500,
                        Error = "An error occurred while deleting the setting.",
                        Message = ex.Message
                    };
                    return StatusCode(500, errorResponse);
                }
            }
        }
    }
}
