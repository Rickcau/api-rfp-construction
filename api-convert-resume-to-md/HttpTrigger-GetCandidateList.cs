
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using api_convert_to_md.Utils;
using Azure.Storage.Blobs;

namespace api_rfp_resume.Function
{
    public class GetCandidateList
    {
        private readonly ILogger<GetCandidateList> _logger;
        private static readonly string storageConnectionString = Helper.GetEnvironmentVariable("CONTAINER_STORAGE_CONNECTIONSTRING");
        private static readonly string candidatePDFContainer = Helper.GetEnvironmentVariable("INPUT_CANDIDATE_RESUME_PDF_CONTAINER");
        private static readonly string candidateMDContainer = Helper.GetEnvironmentVariable("OUTPUT_CANDIDATE_RESUME_MD_CONTAINER");
        // input-rfps-pdfs
        // input-candidate-resumes-pdfs

        public GetCandidateList(ILogger<GetCandidateList> logger)
        {
            _logger = logger;
        }

        [Function("GetCandidateList")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // http://localhost:7071/api/HttpTrigger_GetCandidateList?retreiveFormat=md

            string retreiveFormat = req.Query["retreiveFormat"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(retreiveFormat))
            {
                return new BadRequestObjectResult("Please pass the inputFileName on the query string");
            }


            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var candidatePdfContainerClient = blobServiceClient.GetBlobContainerClient(candidatePDFContainer);
            var candidateMdContainerClient = blobServiceClient.GetBlobContainerClient(candidateMDContainer);

            // Load candidate resumes
            var candidatePdfList = new List<string>();
            var candidateMdList = new List<string>();
            if (retreiveFormat == "pdf")
            {
                await foreach (var blobItem in candidatePdfContainerClient.GetBlobsAsync())
                {
                    candidatePdfList.Add(blobItem.Name);
                }
                _logger.LogInformation("C# HTTP trigger function processed a request. Get RFP PDF List");
                return new OkObjectResult(candidatePdfList);
            }
            else if (retreiveFormat == "md")
            {
                await foreach (var blobItem in candidateMdContainerClient.GetBlobsAsync())
                {
                    candidateMdList.Add(blobItem.Name);
                }
                _logger.LogInformation("C# HTTP trigger function processed a request. Get RFP PDF List");
                return new OkObjectResult(candidateMdList);
            }
            else
            {
                return new BadRequestObjectResult("Please pass the retreiveFormat as pdf or md");
            }
        }
    }
}
