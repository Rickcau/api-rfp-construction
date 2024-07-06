using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using api_rfp_resume.Utils;
using System.Text;



namespace api_rfp_resume.Function
{
    public class ProcsssBlobPdfToMdIdealResume
    {
        private readonly ILogger<ProcsssBlobPdfToMdIdealResume> _logger;
        private readonly Kernel _kernel;
        private readonly AIHelper _aiHelper;

        public ProcsssBlobPdfToMdIdealResume(ILogger<ProcsssBlobPdfToMdIdealResume> logger, Kernel kernel)
        {
            _logger = logger;
            _kernel = kernel;
            _aiHelper = new AIHelper(_kernel);
        }

        [Function(nameof(ProcsssBlobPdfToMdIdealResume))]
        public async Task Run([BlobTrigger("input-rfps-pdfs/{name}", Connection = "CONTAINER_STORAGE_CONNECTIONSTRING")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            stream.Position = 0;

            MemoryStream memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name}");
            await _aiHelper.GenerateIdealResumeAsync(memoryStream, name, "output-rfps-ideal-resumes-md");
        }
    }
}
