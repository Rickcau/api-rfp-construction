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
using api_rfp_resume.Utils;


namespace api_rfp_resume.Utils
{
    public class AIHelper
    {
        private Kernel _kernel;
        
        string _azure_StroageConnectionString = Helper.GetEnvironmentVariable("CONTAINER_STORAGE_CONNECTIONSTRING") ?? "";
        string _rfp_InputPdfContainer = Helper.GetEnvironmentVariable("RFP_INPUT_PDF_CONTAINER") ?? "";
        string _input_CandidateResumePdfContainer = Helper.GetEnvironmentVariable("INPUT_CANDIDATE_RESUME_PDF_CONTAINER") ?? "";
        string _output_CandidateResumeMdContainer = Helper.GetEnvironmentVariable("OUTPUT_CANDIDATE_RESUME_MD_CONTAINER") ?? "";
        string _output_IdealResumeMdContainer = Helper.GetEnvironmentVariable("OUTPUT_IDEAL_RESUME_MD_CONTAINER") ?? "";


        // private string _promptTranslation = @"Translate the input below into {{$language}}

        // MAKE SURE YOU ONLY USE {{$language}}.

        // {{$input}}

        // Translation:";

        private string _promptAnalyzeJobRequirementsPrompt = @"Analyze the following job requirements and list key skills, qualifications, and experiences:\n\n{{$pdfText}} and use Markdown format";
        private string _promptIdealCandidateResumePrompt = @"Based on the following job requirements, create a resume for an ideal candidate:\n\n{{$analysis}} and use Markdown format";


        public AIHelper(Kernel kernel)
        {
            this._kernel = kernel;
        }

        public async Task<string> GenerateIdealResumeAsync(Stream pdfStream, string name, string outputContainerName)
        {
            string extractedText = ExtractTextFromPdf(pdfStream);

            // Step 2: Analyze the extracted text using Azure OpenAI
            string analysis = await AnalyzePdfText(extractedText);

            // Step 3: Generate the ideal resume using the analyzed text
            string idealResume = await GenerateResume(analysis);

            // Step 4: Write the ideal resume to the "Ideal_Resumes" container
            await WriteToBlob(outputContainerName, $"{Path.GetFileNameWithoutExtension(name)}_IdealResume.md", idealResume);

            return "Finished generating ideal resume.";
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

        private async Task<string> AnalyzePdfText(string pdfText)  // Step 2 -Analyze the extracted PFF test extracting the skills, qualifications and experiences for the RFP
        {
            try 
            {
                  
                var executionSettings = new OpenAIPromptExecutionSettings()
                {
                    MaxTokens = 2000,
                    // ResponseFormat = "json_object", // setting JSON output mode
                };

                KernelArguments arguments = new(executionSettings) { { "pdfText", pdfText } };
                var response = await _kernel.InvokePromptAsync(_promptAnalyzeJobRequirementsPrompt, arguments);
                return response.GetValue<string>() ?? "";
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }            
        }

        private  async Task<string> GenerateResume(string analysis)
        {
            try 
            {   
                var executionSettings = new OpenAIPromptExecutionSettings()
                {
                    MaxTokens = 2000,
                    // ResponseFormat = "json_object", // setting JSON output mode
                };

                KernelArguments arguments = new(executionSettings) { { "analysis", analysis } };
                var response = await _kernel.InvokePromptAsync(_promptIdealCandidateResumePrompt, arguments);
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