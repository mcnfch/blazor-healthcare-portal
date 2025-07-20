using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using ImageMagick;
using System.Text;
using System.Text.Json;

namespace DocumentService.Services;

public interface IDocumentProcessor
{
    Task<Dictionary<string, string>> ProcessDocumentAsync(string filePath, string mimeType, byte[] fileData);
}

public class DocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(ILogger<DocumentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> ProcessDocumentAsync(string filePath, string mimeType, byte[] fileData)
    {
        var metadata = new Dictionary<string, string>
        {
            ["file_size"] = fileData.Length.ToString(),
            ["processed_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["mime_type"] = mimeType
        };

        try
        {
            switch (mimeType?.ToLowerInvariant())
            {
                case "application/pdf":
                    await ProcessPdfDocument(fileData, metadata);
                    break;
                case "image/jpeg":
                case "image/jpg":
                case "image/png":
                    await ProcessImageDocument(fileData, metadata);
                    break;
                case "application/x-afp":
                case "application/afp":
                    ProcessAFPDocument(fileData, metadata);
                    break;
                default:
                    metadata["extraction_method"] = "basic";
                    metadata["status"] = "unsupported_format";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document: {FilePath}", filePath);
            metadata["extraction_error"] = ex.Message;
            metadata["status"] = "processing_failed";
        }

        return metadata;
    }

    private async Task ProcessPdfDocument(byte[] fileData, Dictionary<string, string> metadata)
    {
        try
        {
            using var stream = new MemoryStream(fileData);
            using var reader = new PdfReader(stream);
            
            var text = new StringBuilder();
            
            // Extract text from all pages
            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(reader, page);
                text.AppendLine(pageText);
            }

            metadata["document_type"] = "pdf";
            metadata["page_count"] = reader.NumberOfPages.ToString();
            metadata["extraction_method"] = "iTextSharp";

            var fullText = text.ToString();
            
            // Analyze content for healthcare-specific information
            AnalyzeHealthcareContent(fullText, metadata);

            // Store first 1000 characters of extracted text
            metadata["extracted_text"] = fullText.Length > 1000 
                ? fullText.Substring(0, 1000) + "..."
                : fullText;

            metadata["status"] = "successfully_processed";
            
            _logger.LogDebug("Successfully processed PDF document with {PageCount} pages", reader.NumberOfPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF document");
            metadata["status"] = "pdf_processing_failed";
            metadata["error"] = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task ProcessImageDocument(byte[] fileData, Dictionary<string, string> metadata)
    {
        try
        {
            using var image = new MagickImage(fileData);
            
            metadata["document_type"] = "image";
            metadata["width"] = image.Width.ToString();
            metadata["height"] = image.Height.ToString();
            metadata["color_space"] = image.ColorSpace.ToString();
            metadata["format"] = image.Format.ToString();
            metadata["extraction_method"] = "ImageMagick";
            
            // Basic image analysis
            if (image.Width > 2000 || image.Height > 2000)
            {
                metadata["quality"] = "high_resolution";
            }
            else if (image.Width < 500 || image.Height < 500)
            {
                metadata["quality"] = "low_resolution";
            }
            else
            {
                metadata["quality"] = "standard_resolution";
            }

            // Check if image might contain text (basic heuristic)
            var histogram = image.Histogram();
            var uniqueColors = histogram.Count();
            
            if (uniqueColors > 100)
            {
                metadata["likely_contains_text"] = "true";
                metadata["ocr_recommended"] = "true";
            }
            else
            {
                metadata["likely_contains_text"] = "false";
            }

            metadata["status"] = "successfully_processed";
            
            _logger.LogDebug("Successfully processed image document: {Width}x{Height} {Format}", 
                image.Width, image.Height, image.Format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image document");
            metadata["status"] = "image_processing_failed";
            metadata["error"] = ex.Message;
        }

        await Task.CompletedTask;
    }

    private void ProcessAFPDocument(byte[] fileData, Dictionary<string, string> metadata)
    {
        // AFP (Advanced Function Printing) is a complex format
        // This is a basic implementation - in production, you'd use specialized libraries
        
        metadata["document_type"] = "afp";
        metadata["extraction_method"] = "afp_basic";
        
        try
        {
            // Basic AFP header analysis
            if (fileData.Length > 16)
            {
                // Check for AFP record identifier
                var header = Encoding.ASCII.GetString(fileData, 0, Math.Min(16, fileData.Length));
                
                if (header.Contains("5A") || header.Contains("D3"))
                {
                    metadata["afp_format"] = "structured_field";
                    metadata["status"] = "recognized_afp_format";
                }
                else
                {
                    metadata["afp_format"] = "unknown";
                    metadata["status"] = "unrecognized_afp_format";
                }
            }
            
            metadata["requires_specialized_processing"] = "true";
            metadata["recommendation"] = "Use IBM AFP SDK or similar for full processing";
            
            _logger.LogDebug("Processed AFP document with basic analysis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AFP document");
            metadata["status"] = "afp_processing_failed";
            metadata["error"] = ex.Message;
        }
    }

    private void AnalyzeHealthcareContent(string text, Dictionary<string, string> metadata)
    {
        var upperText = text.ToUpperInvariant();
        
        // Look for common healthcare document indicators
        var indicators = new Dictionary<string, string[]>
        {
            ["claim_form"] = new[] { "CLAIM", "INSURANCE", "PATIENT", "PROVIDER", "DIAGNOSIS" },
            ["medical_record"] = new[] { "MEDICAL RECORD", "PATIENT HISTORY", "EXAMINATION", "ASSESSMENT" },
            ["lab_report"] = new[] { "LABORATORY", "LAB RESULTS", "SPECIMEN", "REFERENCE RANGE" },
            ["prescription"] = new[] { "PRESCRIPTION", "MEDICATION", "RX", "PHARMACY", "DOSAGE" },
            ["invoice"] = new[] { "INVOICE", "BILLING", "CHARGES", "PAYMENT", "AMOUNT DUE" },
            ["explanation_of_benefits"] = new[] { "EXPLANATION OF BENEFITS", "EOB", "BENEFITS PAID", "DEDUCTIBLE" }
        };

        var detectedTypes = new List<string>();
        var keywordCounts = new Dictionary<string, int>();

        foreach (var indicator in indicators)
        {
            var matchCount = 0;
            foreach (var keyword in indicator.Value)
            {
                var count = CountOccurrences(upperText, keyword);
                if (count > 0)
                {
                    matchCount += count;
                    keywordCounts[keyword.ToLowerInvariant()] = count;
                }
            }

            if (matchCount >= 2) // Require at least 2 keyword matches
            {
                detectedTypes.Add(indicator.Key);
            }
        }

        if (detectedTypes.Any())
        {
            metadata["detected_document_types"] = string.Join(", ", detectedTypes);
            metadata["healthcare_content"] = "true";
        }
        else
        {
            metadata["healthcare_content"] = "false";
        }

        // Look for specific medical codes
        if (ContainsMedicalCodes(upperText))
        {
            metadata["contains_medical_codes"] = "true";
        }

        // Store keyword analysis
        if (keywordCounts.Any())
        {
            metadata["keyword_analysis"] = JsonSerializer.Serialize(keywordCounts);
        }
    }

    private static int CountOccurrences(string text, string keyword)
    {
        int count = 0;
        int index = 0;
        
        while ((index = text.IndexOf(keyword, index)) != -1)
        {
            count++;
            index += keyword.Length;
        }
        
        return count;
    }

    private static bool ContainsMedicalCodes(string text)
    {
        // Simple regex patterns for common medical codes
        var patterns = new[]
        {
            @"[A-Z]\d{2}\.\d{1,3}", // ICD-10 pattern (e.g., Z00.00)
            @"\d{5}", // CPT code pattern (5 digits)
            @"[A-Z]\d{4}[A-Z]?", // HCPCS pattern
        };

        return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(text, pattern));
    }
}