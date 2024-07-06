using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using api_resume_match.Utils;

namespace ai_resume_match.Function
{
    public class HttpTrigger_Resume_Match
    {
        private readonly ILogger<HttpTrigger_Resume_Match> _logger;
        private static readonly string storageConnectionString = Helper.GetEnvironmentVariable("CONTAINER_STORAGE_CONNECTIONSTRING");

        private static readonly MLContext mlContext = new MLContext();

        public HttpTrigger_Resume_Match(ILogger<HttpTrigger_Resume_Match> logger)
        {
            _logger = logger;
        }

        [Function("HttpTrigger_Resume_Match")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // http://localhost:7071/api/HttpTrigger_Resume_Match?inputFileName=NY Housing Authority RFP_IdealResume.md
            string inputBlobName = req.Query["inputFileName"].FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(inputBlobName))
            {
                return new BadRequestObjectResult("Please pass the inputFileName on the query string");
            }

            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var inputContainerClient = blobServiceClient.GetBlobContainerClient("output-rfps-ideal-resumes-md");
            var candidatesContainerClient = blobServiceClient.GetBlobContainerClient("output-candidate-resumes-md");

            // Load input resume
            string inputResume = await LoadBlobContentAsync(inputContainerClient, inputBlobName);

            // Load candidate resumes
            var candidates = new List<(string Name, string Content)>();
            await foreach (var blobItem in candidatesContainerClient.GetBlobsAsync())
            {
                string candidateResume = await LoadBlobContentAsync(candidatesContainerClient, blobItem.Name);
                candidates.Add((blobItem.Name, candidateResume));
            }

            // VectorizeText3(inputResume);
            // Calculate similarities
            // var inputVector = VectorizeText5(inputResume);
            var inputVector = VectorizeAndPad(inputResume, 100);
            var candidateVectors = candidates.Select(c => (c.Name, VectorizeAndPad(c.Content,100))).ToList();
            var similarities = candidateVectors.Select(cv => new ResumeMatch { Name = cv.Name, Similarity = CosineSimilarity(inputVector, cv.Item2) }).ToList();

            // var similarities = candidateVectors.Select(cv => (cv.Name, CosineSimilarity(inputVector, cv.Item2))).ToList();

            // // Get top matches
            // Get top matches
            var topMatches = similarities.OrderByDescending(s => s.Similarity).Take(5).ToList();
            // var topMatches = similarities.OrderByDescending(s => s.Item2).Take(5).ToList();

            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult(topMatches);
            // return new OkObjectResult("test");
        }

        private static async Task<string> LoadBlobContentAsync(BlobContainerClient containerClient, string blobName)
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync();
            using var reader = new StreamReader(response.Value.Content);
            return await reader.ReadToEndAsync();
        }

        // Function to vectorize a document using BERT embeddings

        private static void VectorizeText3(string resumetext)
        {
            // Create ML context
            var mlContext = new MLContext();

            // Define data structure
            var data = new[] { new ResumeData { Text = resumetext } };
            var dataView = mlContext.Data.LoadFromEnumerable(data);

            // Build a text featurization pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(ResumeData.Text))
                .Append(mlContext.Transforms.Concatenate("FeaturesVector", "Features"));

            // Fit to data
            var transformer = pipeline.Fit(dataView);

            // Create prediction engine
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ResumeData, ResumePrediction>(transformer);

            // Predict
            var resumeVector = predictionEngine.Predict(new ResumeData { Text = resumetext }).FeaturesVector;

            // Print the vector representation
            Console.WriteLine("Vectorized Resume:");
            if (resumeVector != null)
            {
                foreach (var value in resumeVector)
                {
                    Console.Write(value + " ");
                }
            }
        }

        static float[] VectorizeText4(string resumeText)
        {
            // Create ML context
            var mlContext = new MLContext();

            // Define data structure
            var data = new[] { new ResumeData { Text = resumeText } };
            var dataView = mlContext.Data.LoadFromEnumerable(data);

            // Build a text featurization pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(ResumeData.Text))
                .Append(mlContext.Transforms.Concatenate("FeaturesVector", "Features"));

            // Fit to data
            var transformer = pipeline.Fit(dataView);

            // Create prediction engine
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ResumeData, ResumePrediction>(transformer);

            // Predict
            var vector = predictionEngine.Predict(new ResumeData { Text = resumeText }).FeaturesVector;

            return vector ?? Array.Empty<float>();
        }


        static float[] VectorizeAndPad(string resumeText, int maxLength)
        {
            // Vectorize the resume text
            var vector = VectorizeText5(resumeText);

            // Perform padding or truncation
            if (vector.Length >= maxLength)
            {
                return vector.Take(maxLength).ToArray(); // Truncate to maxLength
            }
            else
            {
                // Pad with zeros to reach maxLength
                var paddedVector = new float[maxLength];
                Array.Copy(vector, paddedVector, vector.Length);
                return paddedVector;
            }
        }

        static float[] VectorizeText5(string resumeText)
        {
            // Create ML context
            var mlContext = new MLContext();

            // Define data structure
            var data = new[] { new ResumeData { Text = resumeText } };
            var dataView = mlContext.Data.LoadFromEnumerable(data);

            // Build a text featurization pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(ResumeData.Text))
                .Append(mlContext.Transforms.Concatenate("FeaturesVector", "Features"));

            // Fit to data
            var transformer = pipeline.Fit(dataView);

            // Create prediction engine
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ResumeData, ResumePrediction>(transformer);

            // Predict
            var prediction = predictionEngine.Predict(new ResumeData { Text = resumeText });

            // Check if prediction succeeded and FeaturesVector is not null
            if (prediction == null || prediction.FeaturesVector == null)
            {
                // Handle the case where prediction failed or returned null
                throw new InvalidOperationException("Failed to vectorize resume text.");
            }

            // Return FeaturesVector
            return prediction.FeaturesVector;
        }

    
        private static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
            {
                throw new ArgumentException("Vectors must be of the same length");
            }
            try 
            {
                float dotProduct = 0;
                float magnitudeA = 0;
                float magnitudeB = 0;

                for (int i = 0; i < vectorA.Length; i++)
                {
                    dotProduct += vectorA[i] * vectorB[i];
                    magnitudeA += vectorA[i] * vectorA[i];
                    magnitudeB += vectorB[i] * vectorB[i];
                }

                if (magnitudeA == 0 || magnitudeB == 0)
                {
                    return 0;
                }
                
                return dotProduct / ((float)Math.Sqrt(magnitudeA) * (float)Math.Sqrt(magnitudeB));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }   
        }

    }

    // Define data structures
    public class ResumeData
    {
        [LoadColumn(0)]
        public string? Text { get; set; }
    }

    public class ResumePrediction
    {
        [VectorType(100)] // Adjust vector size as needed
        public float[]? FeaturesVector { get; set; }
    }
    public class ResumeMatch
{
    public string? Name { get; set; }
    public double Similarity { get; set; }
}
}
