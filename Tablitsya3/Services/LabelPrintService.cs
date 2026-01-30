using QRCoder;
using System.Text;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для генерації та друку бірок деталей
    /// </summary>
    public class LabelPrintService
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _logger;

        public LabelPrintService(ApplicationDbContext context, LoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Модель даних для бірки
        /// </summary>
        public class LabelData
        {
            public string NC { get; set; } = "";           // NC1: номер
            public string Project { get; set; } = "";       // П: проект (числовий ID)
            public string Material { get; set; } = "";      // М: матеріал
            public string OrderName { get; set; } = "";     // И: замовлення
            public string PartName { get; set; } = "";      // Н: назва деталі
            public string PartCode { get; set; } = "";      // Код деталі
            public double Length { get; set; }              // Довжина
            public double Width { get; set; }               // Ширина
            public double Thickness { get; set; }           // Товщина
            public int PartNumber { get; set; }             // № деталі
            public int PartPosition { get; set; }           // Позиція (1:4)
            public int TotalParts { get; set; }             // Всього в позиції
            public int Quantity { get; set; }               // К: кількість
            public string QRCode { get; set; } = "";        // QR-код
            public DateTime PrintDate { get; set; }         // Дата друку
            public string DefectType { get; set; } = "";    // Тип браку (для перевиробництва)
            public bool IsDefectLabel { get; set; }         // Чи це бірка браку
            public string EdgeBandingThickness { get; set; } = "";  // Товщина кромки (біля QR-коду)
        }

        /// <summary>
        /// Створити дані бірки з деталі
        /// </summary>
        public async Task<LabelData> CreateLabelFromPartAsync(PartEntity part, DefectEntity? defect = null)
        {
            // Отримуємо числовий ID проекту з бази
            var projectId = await _context.ImportedProjects
                .Where(p => p.ProjectUuid == part.ProjectExternalUuid)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            return new LabelData
            {
                NC = $"NC1: {part.PartId:D6}^",
                Project = projectId > 0 ? projectId.ToString() : part.ProjectExternalUuid ?? "",
                Material = part.Material ?? "",
                OrderName = part.OrderName ?? "",
                PartName = part.Name ?? "",
                PartCode = part.Code ?? "",
                Length = part.Length,
                Width = part.Width,
                Thickness = part.Thickness,
                PartNumber = part.PartId,
                PartPosition = part.PartCounter,
                TotalParts = part.Quantity,
                Quantity = part.Quantity,
                QRCode = $"{part.ProjectExternalUuid}/{part.PartId}/{part.PartCounter}",
                PrintDate = DateTime.Now,
                DefectType = defect?.DefectType ?? "",
                IsDefectLabel = defect != null,
                EdgeBandingThickness = part.EdgeBandingThickness ?? ""
            };
        }

        /// <summary>
        /// Генерувати QR-код як Base64 PNG
        /// </summary>
        public string GenerateQRCodeBase64(string content, int pixelsPerModule = 10)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка генерації QR-коду: {ex.Message}", ex, "LabelPrintService");
                return "";
            }
        }

        /// <summary>
        /// Генерувати штрих-код Code128 як SVG
        /// </summary>
        public string GenerateBarcodeAsSvg(string content, int width = 200, int height = 40)
        {
            // Простий Code128 штрих-код у SVG форматі
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'>");
            
            // Генеруємо прості смуги для візуалізації
            var barWidth = width / (content.Length * 11.0);
            var x = 0.0;
            
            foreach (var c in content)
            {
                var pattern = GetCode128Pattern(c);
                foreach (var bar in pattern)
                {
                    if (bar == '1')
                    {
                        sb.Append($"<rect x='{x:F1}' y='0' width='{barWidth:F1}' height='{height}' fill='black'/>");
                    }
                    x += barWidth;
                }
            }
            
            sb.Append("</svg>");
            return sb.ToString();
        }

        private string GetCode128Pattern(char c)
        {
            // Спрощений патерн для Code128
            var code = (int)c % 10;
            return code switch
            {
                0 => "11011001100",
                1 => "11001101100",
                2 => "11001100110",
                3 => "10010011000",
                4 => "10010001100",
                5 => "10001001100",
                6 => "10011001000",
                7 => "10011000100",
                8 => "10001100100",
                9 => "11001001000",
                _ => "11011001100"
            };
        }

        /// <summary>
        /// Генерувати HTML для друку бірки
        /// </summary>
        public string GenerateLabelHtml(LabelData label)
        {
            var qrCodeBase64 = GenerateQRCodeBase64(label.QRCode, 8);
            var dimensions = $"{label.Length:F0} x {label.Width:F0} x {label.Thickness:F0}";
            var partInfo = $"№:{label.PartNumber} ({label.PartPosition}:{label.TotalParts}) К:{label.Quantity}";
            
            var defectBadge = label.IsDefectLabel 
                ? $"<div style='background:#dc3545;color:white;padding:2px 8px;font-weight:bold;margin-bottom:5px;'>🔄 ПЕРЕВИРОБНИЦТВО: {label.DefectType}</div>" 
                : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Бірка деталі</title>
    <style>
        @page {{
            size: 100mm 60mm;
            margin: 2mm;
        }}
        body {{
            font-family: Arial, sans-serif;
            font-size: 11px;
            margin: 0;
            padding: 3mm;
            width: 94mm;
            height: 54mm;
            box-sizing: border-box;
        }}
        .label-container {{
            display: flex;
            height: 100%;
        }}
        .left-section {{
            flex: 1;
            padding-right: 5px;
        }}
        .right-section {{
            width: 45mm;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
        }}
        .barcode {{
            width: 100%;
            height: 25px;
            margin-bottom: 3px;
        }}
        .barcode-text {{
            font-family: monospace;
            font-size: 10px;
            text-align: center;
            margin-bottom: 5px;
        }}
        .field {{
            margin: 2px 0;
            line-height: 1.3;
        }}
        .field-label {{
            font-weight: bold;
        }}
        .dimensions {{
            font-size: 14px;
            font-weight: bold;
        }}
        .part-info {{
            font-size: 13px;
            font-weight: bold;
        }}
        .qr-code {{
            width: 38mm;
            height: 38mm;
        }}
        .qr-number {{
            font-size: 18px;
            font-weight: bold;
            margin-top: 3px;
        }}
        .date {{
            font-size: 10px;
            color: #666;
            margin-top: 5px;
        }}
        .defect-badge {{
            background: #dc3545;
            color: white;
            padding: 2px 8px;
            font-weight: bold;
            margin-bottom: 5px;
            font-size: 10px;
        }}
        @media print {{
            body {{
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }}
        }}
    </style>
</head>
<body>
    <div class='label-container'>
        <div class='left-section'>
            {defectBadge}
            <div class='barcode'>
                <svg viewBox='0 0 200 25' style='width:100%;height:25px;'>
                    {GenerateBarcodeSvgContent(label.NC)}
                </svg>
            </div>
            <div class='barcode-text'>{label.NC}</div>

            <div class='field'><span class='field-label'>П:</span>{TruncateText(label.Project, 20)}</div>
            <div class='field'><span class='field-label'>М:</span>{TruncateText(label.Material, 25)}</div>
            <div class='field'><span class='field-label'>И:</span>{TruncateText(label.OrderName, 25)}</div>
            <div class='field'><span class='field-label'>Н:</span>{label.PartCode} - {TruncateText(label.PartName, 20)}</div>
            <div class='field dimensions'><span class='field-label'>РД:</span>{dimensions}</div>
            <div class='field part-info'>{partInfo}</div>
            <div class='field'><span class='field-label'>О:</span></div>
            <div class='date'>{label.PrintDate:yyyy.MM.dd HH:mm}</div>
        </div>
        <div class='right-section'>
            <div class='qr-number'>{label.EdgeBandingThickness}</div>
            <img class='qr-code' src='data:image/png;base64,{qrCodeBase64}' alt='QR Code'/>
            <div class='qr-number'>{label.EdgeBandingThickness}</div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateBarcodeSvgContent(string text)
        {
            var sb = new StringBuilder();
            var barWidth = 1.5;
            var x = 0.0;
            
            // Start pattern
            sb.Append($"<rect x='{x}' y='0' width='{barWidth * 2}' height='25' fill='black'/>");
            x += barWidth * 3;
            
            foreach (var c in text.Take(15))
            {
                var pattern = GetCode128Pattern(c);
                foreach (var bar in pattern)
                {
                    if (bar == '1')
                    {
                        sb.Append($"<rect x='{x:F1}' y='0' width='{barWidth:F1}' height='25' fill='black'/>");
                    }
                    x += barWidth;
                }
            }
            
            // End pattern
            sb.Append($"<rect x='{x}' y='0' width='{barWidth * 2}' height='25' fill='black'/>");
            
            return sb.ToString();
        }

        private string TruncateText(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }
    }
}
