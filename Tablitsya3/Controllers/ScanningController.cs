using Microsoft.AspNetCore.Mvc;
using Tablitsya3.Models.Scanning;
using Tablitsya3.Services;

namespace Tablitsya3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanningController : ControllerBase
    {
        private readonly ScanningService _scanningService;
        private readonly LoggingService _logger;

        public ScanningController(ScanningService scanningService, LoggingService logger)
        {
            _scanningService = scanningService;
            _logger = logger;
        }

        /// <summary>
        /// Імпорт проекту з .project файлу
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ImportResult>> ImportProject([FromForm] IFormFile file, [FromQuery] bool clearExisting = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ImportResult { Success = false, Message = "Файл не вибрано" });
                }

                // Читаємо файл
                using var reader = new StreamReader(file.OpenReadStream());
                var xmlContent = await reader.ReadToEndAsync();

                var result = await _scanningService.ImportProjectAsync(xmlContent, file.FileName, clearExisting);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка імпорту: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new ImportResult { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Імпорт проекту з XML рядка
        /// </summary>
        [HttpPost("import-xml")]
        public async Task<ActionResult<ImportResult>> ImportProjectXml([FromBody] ImportXmlRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.XmlContent))
                {
                    return BadRequest(new ImportResult { Success = false, Message = "XML контент порожній" });
                }

                var result = await _scanningService.ImportProjectAsync(
                    request.XmlContent, 
                    request.FileName ?? "uploaded.project", 
                    request.ClearExisting);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка імпорту XML: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new ImportResult { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Сканування QR-коду
        /// </summary>
        [HttpPost("scan")]
        public async Task<ActionResult<ScanResult>> Scan([FromBody] ScanRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.QRCode))
                {
                    return BadRequest(new ScanResult { Success = false, Message = "QR-код не вказано" });
                }

                var result = await _scanningService.ScanQRCodeAsync(
                    request.QRCode, 
                    request.Stage, 
                    request.UserId, 
                    request.DeviceId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка сканування: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new ScanResult { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання деталі за QR-кодом
        /// </summary>
        [HttpGet("part/{qrCode}")]
        public async Task<ActionResult<Part>> GetPart(string qrCode)
        {
            try
            {
                var part = await _scanningService.GetPartByQRCodeAsync(Uri.UnescapeDataString(qrCode));
                
                if (part == null)
                {
                    return NotFound(new { message = "Деталь не знайдена" });
                }

                return Ok(part);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання деталі: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання списку деталей
        /// </summary>
        [HttpGet("parts")]
        public async Task<ActionResult<PartsListResponse>> GetParts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? projectUuid = null,
            [FromQuery] string? fileName = null,
            [FromQuery] string? orderName = null,
            [FromQuery] bool? completed = null)
        {
            try
            {
                var (parts, total) = await _scanningService.GetPartsAsync(
                    page, pageSize, projectUuid, fileName, orderName, completed);

                return Ok(new PartsListResponse
                {
                    Parts = parts,
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання списку деталей: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання списку проектів
        /// </summary>
        [HttpGet("projects")]
        public async Task<ActionResult> GetProjects()
        {
            try
            {
                var projects = await _scanningService.GetProjectsAsync();
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання проектів: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання статистики проекту
        /// </summary>
        [HttpGet("projects/{projectUuid}/statistics")]
        public async Task<ActionResult<ProjectStatistics>> GetProjectStatistics(string projectUuid)
        {
            try
            {
                var stats = await _scanningService.GetProjectStatisticsAsync(Uri.UnescapeDataString(projectUuid));
                
                if (stats == null)
                {
                    return NotFound(new { message = "Проект не знайдено" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання статистики: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Видалення проекту
        /// </summary>
        [HttpDelete("projects/{projectUuid}")]
        public async Task<ActionResult> DeleteProject(string projectUuid)
        {
            try
            {
                var deletedCount = await _scanningService.DeleteProjectDataAsync(Uri.UnescapeDataString(projectUuid));
                return Ok(new { message = $"Видалено {deletedCount} деталей", deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка видалення проекту: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання унікальних файлів
        /// </summary>
        [HttpGet("files")]
        public async Task<ActionResult<List<string>>> GetFiles()
        {
            try
            {
                var files = await _scanningService.GetUniqueFileNamesAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання файлів: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання унікальних замовлень
        /// </summary>
        [HttpGet("orders")]
        public async Task<ActionResult<List<string>>> GetOrders([FromQuery] string? projectUuid = null)
        {
            try
            {
                var orders = await _scanningService.GetUniqueOrderNamesAsync(projectUuid);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання замовлень: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отримання кількості деталей
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount([FromQuery] string? projectUuid = null)
        {
            try
            {
                var count = await _scanningService.GetPartsCountAsync(projectUuid);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання кількості: {ex.Message}", ex, "ScanningAPI");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Діагностика - пошук деталі за QR-кодом з детальною інформацією
        /// </summary>
        [HttpGet("debug/{qrCode}")]
        public async Task<ActionResult> DebugPart(string qrCode)
        {
            try
            {
                var decoded = Uri.UnescapeDataString(qrCode);
                var parts = decoded.Split('/');
                
                if (parts.Length < 3)
                {
                    return Ok(new { 
                        error = "Invalid QR format", 
                        qrCode = decoded,
                        expectedFormat = "ProjectExternalUuid/PartId/PartCounter"
                    });
                }

                var projectUuid = parts[0];
                int.TryParse(parts[1], out int partId);
                int.TryParse(parts[2], out int partCounter);

                // Шукаємо всі проекти
                var allProjects = await _scanningService.GetProjectsAsync();
                
                // Шукаємо деталь
                var foundPart = await _scanningService.GetPartByQRCodeAsync(decoded);
                
                // Шукаємо схожі деталі (тільки по PartId)
                var similarParts = await _scanningService.GetPartsAsync(1, 10, projectUuid);

                return Ok(new {
                    qrCode = decoded,
                    parsed = new { projectUuid, partId, partCounter },
                    foundPart = foundPart != null ? new { 
                        foundPart.Id, 
                        foundPart.ProjectExternalUuid, 
                        foundPart.PartId, 
                        foundPart.PartCounter,
                        foundPart.Name,
                        foundPart.QRCode
                    } : null,
                    projectsInDb = allProjects.Select(p => new { 
                        p.Id, 
                        p.ProjectUuid, 
                        p.FileName,
                        p.PartsCount
                    }),
                    partsWithSameProjectUuid = similarParts.Parts.Take(5).Select(p => new {
                        p.Id,
                        p.ProjectExternalUuid,
                        p.PartId,
                        p.PartCounter,
                        p.Name,
                        p.QRCode
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }

    #region Request/Response models

    public class ImportXmlRequest
    {
        public string XmlContent { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public bool ClearExisting { get; set; }
    }

    public class ScanRequest
    {
        public string QRCode { get; set; } = string.Empty;
        public ProductionStage? Stage { get; set; }
        public string? UserId { get; set; }
        public string? DeviceId { get; set; }
    }

    public class PartsListResponse
    {
        public List<Part> Parts { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion
}
