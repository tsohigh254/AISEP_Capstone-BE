using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/files")]
[Tags("Files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public FilesController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Upload a file as a chat attachment.
    /// Accessible to all authenticated users (Startup, Investor, Advisor, Staff).
    /// </summary>
    [HttpPost("upload-attachment")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<string>.Fail("File cannot be empty").ToActionResult();
        }

        try
        {
            // Upload to a specific folder for chat attachments
            var url = await _cloudinaryService.UploadDocument(file, "chat-attachments");
            return ApiResponse<string>.Ok(url).ToActionResult();
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail(ex.Message).ToActionResult();
        }
    }
}
