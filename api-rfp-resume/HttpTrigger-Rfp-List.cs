using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using api_rfp_resume.Utils;
using Azure.Storage.Blobs;

namespace api_rfp_resume.Function
{
    // GetCandidateList
    public class GetRfpList
    {
        private readonly ILogger<GetRfpList> _logger;
        private static readonly string storageConnectionString = Helper.GetEnvironmentVariable("CONTAINER_STORAGE_CONNECTIONSTRING");
        private static readonly string rfpPDFContainer = Helper.GetEnvironmentVariable("RFP_INPUT_PDF_CONTAINER");
        private static readonly string rfpMDContainer = Helper.GetEnvironmentVariable("OUTPUT_IDEAL_RESUME_MD_CONTAINER");
        // input-rfps-pdfs
        // input-candidate-resumes-pdfs

        public GetRfpList(ILogger<GetRfpList> logger)
        {
            _logger = logger;
        }

        [Function("GetRfpList")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // http://localhost:7071/api/HttpTrigger_Rfp_List?retreiveFormat=md

            string retreiveFormat = req.Query["retreiveFormat"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(retreiveFormat))
            {
                return new BadRequestObjectResult("Please pass the inputFileName on the query string");
            }


            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var rfpPdfContainerClient = blobServiceClient.GetBlobContainerClient(rfpPDFContainer);
            var rfpMdContainerClient = blobServiceClient.GetBlobContainerClient(rfpMDContainer);

            // Load candidate resumes
            var rfpPdfList = new List<string>();
            var rfpMdList = new List<string>();
            if (retreiveFormat == "pdf")
            {
                await foreach (var blobItem in rfpPdfContainerClient.GetBlobsAsync())
                {
                    rfpPdfList.Add(blobItem.Name);
                }
                _logger.LogInformation("C# HTTP trigger function processed a request. Get RFP PDF List");
                return new OkObjectResult(rfpPdfList);
            }
            else if (retreiveFormat == "md")
            {
                await foreach (var blobItem in rfpMdContainerClient.GetBlobsAsync())
                {
                    rfpMdList.Add(blobItem.Name);
                }
                _logger.LogInformation("C# HTTP trigger function processed a request. Get RFP PDF List");
                return new OkObjectResult(rfpMdList);
            }
            else
            {
                return new BadRequestObjectResult("Please pass the retreiveFormat as pdf or md");
            }
        }
    }
}
