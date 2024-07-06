using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using api_convert_to_md.Utils;
using System.Text;

// BlobTrigger-Convert-To-MD
namespace api_rfp_resume.Function
{
    public class BlobTrigger_Convert_To_Ideal_MD
    {
        private readonly ILogger<BlobTrigger_Convert_To_Ideal_MD> _logger;
        private readonly Kernel _kernel;
        private readonly AIHelper _aiHelper;

        public BlobTrigger_Convert_To_Ideal_MD(ILogger<BlobTrigger_Convert_To_Ideal_MD> logger, Kernel kernel)
        {
            _logger = logger;
            _kernel = kernel;
            _aiHelper = new AIHelper(_kernel);
        }

        [Function(nameof(BlobTrigger_Convert_To_Ideal_MD))]
        public async Task Run([BlobTrigger("input-candidate-resumes-pdfs/{name}", Connection = "CONTAINER_STORAGE_CONNECTIONSTRING")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            stream.Position = 0;

            MemoryStream memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");
            await _aiHelper.GenerateMarkdownAsync(memoryStream, name, "output-candidate-resumes-md");
        }
    }
}
