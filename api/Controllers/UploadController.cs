using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace openapiconverter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            {
                return BadRequest("No files were provided.");
            }

            var convertedFiles = new List<object>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Determine the file extension
                    var fileExtension = Path.GetExtension(file.FileName).ToLower();

                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Read the file content as OpenAPI document
                    var reader = new OpenApiStreamReader();
                    var document = reader.Read(memoryStream, out var diagnostic);

                    // Convert the document based on the extension
                    string convertedContent;
                    if (fileExtension == ".yaml" || fileExtension == ".yml")
                    {
                        // Convert YAML to JSON
                        convertedContent = ConvertToJson(document);
                    }
                    else if (fileExtension == ".json")
                    {
                        // Convert JSON to YAML
                        convertedContent = ConvertToYaml(document);
                    }
                    else
                    {
                        return BadRequest($"Unsupported file extension: {fileExtension}. Only .yaml, .yml, and .json are supported.");
                    }

                    // Add the converted file data to the response
                    convertedFiles.Add(new
                    {
                        FileName = Path.GetFileNameWithoutExtension(file.FileName) + (fileExtension == ".json" ? ".yaml" : ".json"),
                        Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(convertedContent)),
                        Size = convertedContent.Length,
                        OriginalExtension = fileExtension,
                        ConvertedExtension = fileExtension == ".json" ? ".yaml" : ".json"
                    });
                }
            }

            return Ok(new
            {
                FileCount = files.Count,
                Files = convertedFiles,
                Message = "Files uploaded and converted successfully!"
            });
        }

        private string ConvertToJson(OpenApiDocument document)
        {
            using var stringWriter = new StringWriter();
            var jsonWriter = new OpenApiJsonWriter(stringWriter);
            document.SerializeAsV3(jsonWriter);
            return stringWriter.ToString();
        }

        private string ConvertToYaml(OpenApiDocument document)
        {
            using var stringWriter = new StringWriter();
            var yamlWriter = new OpenApiYamlWriter(stringWriter);
            document.SerializeAsV3(yamlWriter);
            return stringWriter.ToString();
        }
    }