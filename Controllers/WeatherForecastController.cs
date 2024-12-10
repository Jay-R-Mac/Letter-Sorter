using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf
namespace fileSorter.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : ControllerBase
{
    [HttpPost("upload")]
public IActionResult UploadAndValidatePdf([FromForm] IFormFile file)
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
        // Validate the PDF file
        using (var memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Attempt to open the PDF to validate it
            using (var pdfReader = new PdfReader(memoryStream))
            {
                using (var pdfDocument = new PdfDocument(pdfReader))
                {
                    // If no exception is thrown, the PDF is valid
                }
            }
        }

        return Ok(new { Message = "PDF uploaded and validated successfully!", FileName = file.FileName });
    }
    catch (Exception ex)
    {
        return BadRequest(new { Message = "Invalid PDF file.", Error = ex.Message });
    }
}
