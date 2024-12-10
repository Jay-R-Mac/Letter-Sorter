using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq; // Make sure this namespace is included

namespace fileSorter.Controllers
{
    [ApiController]
    [Route("filecontroller")]
    public class FileController : ControllerBase
    {
        // Endpoint to upload and extract text from the PDF
        [HttpPost("extract-text")]
        public IActionResult ExtractTextFromPdf([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file was uploaded or the file is empty." });
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Message = "Only PDF files are allowed." });
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (var pdfReader = new PdfReader(memoryStream))
                    {
                        using (var pdfDocument = new PdfDocument(pdfReader))
                        {
                            var text = new StringBuilder();
                            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                            {
                                var page = pdfDocument.GetPage(i);
                                var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                                text.Append(pageText);
                            }

                            // Call the method to organize the PDF based on extracted date
                            string folderPath = OrganizePdfByDate(text.ToString(), file);
                            return Ok(new { Message = "PDF uploaded and organized successfully.", FolderPath = folderPath });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Failed to extract text.", Error = ex.Message });
            }
        }

        // Method to organize PDF into date-based folders
        private string OrganizePdfByDate(string extractedText, IFormFile file)
        {
            // Adjusted regex to match the format "1st Dec. 2024"
            string pattern = @"\d{1,2}(st|nd|rd|th)\s+[A-Za-z]{3}\.\s+\d{4}";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(extractedText);

            if (match.Success)
            {
                // Extracted date (e.g., "1st Dec. 2024")
                string dateString = match.Value;

                // Simplify the date (remove suffix and period) to use in folder name
                string simplifiedDate = Regex.Replace(dateString, @"(st|nd|rd|th|\.)", "");
                string folderName = simplifiedDate.Replace(" ", "-"); // Example: "1-Dec-2024"
                string folderPath = Path.Combine("UploadedFiles", folderName);

                // Create the directory if it doesn't exist
                Directory.CreateDirectory(folderPath);

                // Save the PDF file in the appropriate folder
                string filePath = Path.Combine(folderPath, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return filePath; // Return the path where the file is stored
            }

            return null; // Return null if no date is found
        }

        // GET: /api/monthly-data
        [HttpGet("api/monthly-data")]
        public IActionResult GetMonthlyFileData()
        {
            try
            {
                // Get all subdirectories in the 'UploadedFiles' directory
                string uploadedFilesPath = "UploadedFiles";
                var directories = Directory.GetDirectories(uploadedFilesPath);

                // Create an object to store the month-wise count of files
                var monthlyData = directories
                    .Select(directory =>
                    {
                        // Extract the month (YYYY-MM) from the folder name
                        var folderName = Path.GetFileName(directory);
                        return new { Month = folderName, FileCount = Directory.GetFiles(directory).Length };
                    })
                    .ToList();

                return Ok(monthlyData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Failed to retrieve monthly data.", Error = ex.Message });
            }
        }
    }
}
