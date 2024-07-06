using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.Storage.Blobs;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Configuration;
using System.Net;
using api_convert_to_md.Utils;


namespace api_convert_to_md.Utils
{
    public class AIHelper
    {
        private Kernel _kernel;
        
        string _azure_StroageConnectionString = Helper.GetEnvironmentVariable("CONTAINER_STORAGE_CONNECTIONSTRING") ?? "";
        string _rfp_InputPdfContainer = Helper.GetEnvironmentVariable("RFP_INPUT_PDF_CONTAINER") ?? "";
        string _input_CandidateResumePdfContainer = Helper.GetEnvironmentVariable("INPUT_CANDIDATE_RESUME_PDF_CONTAINER") ?? "";
        string _output_CandidateResumeMdContainer = Helper.GetEnvironmentVariable("OUTPUT_CANDIDATE_RESUME_MD_CONTAINER") ?? "";
        string _output_IdealResumeMdContainer = Helper.GetEnvironmentVariable("OUTPUT_IDEAL_RESUME_MD_CONTAINER") ?? "";

        private string _promptGenerateMarkdownPrompt = @"Based on the following resume:
        ### Resume Text
        {{$pdfText}} 
        ### End of Resume 
        Output: Markdown format";

        public AIHelper(Kernel kernel)
        {
            this._kernel = kernel;
        }

        public async Task<string> GenerateMarkdownAsync(Stream pdfStream, string name, string outputContainerName)
        {
            string extractedText = ExtractTextFromPdf(pdfStream);

            // Step 3: Generate the ideal resume using the analyzed text
            string idealResume = await GenerateMarkdownResume(extractedText);

            // Step 4: Write the ideal resume to the "Ideal_Resumes" container
            await WriteToBlob(outputContainerName, $"{Path.GetFileNameWithoutExtension(name)}.md", idealResume);

            return "Finished converting PDF resume to Markdown format.";
        }

        private string ExtractTextFromPdf(Stream pdfStream)  // Step 1 - Extract text from the RFP PDF
        {
    
            StringBuilder text = new StringBuilder();

            using (PdfReader pdfReader = new PdfReader(pdfStream))
            using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
            {
                for (int pageNumber = 1; pageNumber <= pdfDoc.GetNumberOfPages(); pageNumber++)
                {
                    var page = pdfDoc.GetPage(pageNumber);
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.Append(pageText);
                }
            }

            return text.ToString();
        }

        private  async Task<string> GenerateMarkdownResume(string pdfText)
        {
            try 
            {   
                var executionSettings = new OpenAIPromptExecutionSettings()
                {
                    MaxTokens = 2000,
                    // ResponseFormat = "json_object", // setting JSON output mode
                };

                KernelArguments arguments = new(executionSettings) { { "pdfText", pdfText } };
                var response = await _kernel.InvokePromptAsync(_promptGenerateMarkdownPrompt, arguments);
                return response.GetValue<string>() ?? "";
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task WriteToBlob(string containerName, string blobName, string content)
        {
            try 
            {
                var blobServiceClient = new BlobServiceClient(_azure_StroageConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

    }
}