# RFP (Request for Proposal)  | api-rfp-construction GenAI
This Repo contains a set of APIs that can be used to identify the best candidates based on a specific RFP. I am specifically using the construction industry as an example, but this could be applied to any industry that works with RFPs.

## api-rfp-resume - This API uses GenAI
This API is an Azure Blob Trigger, when invoked parses the RFP.PDF and generates an **ideal** RFP resume and copies it to and Azure Storage Container.  The target container for this API is **output-rfps-ideal-resumes-md**.  Yes, the format of the ideal RFP resume is in **markdown**. There are benefits to using **markdown**, especially if a frontend would like to display the data, of course the code can be modified to so the ideal RFP resume in plain text if needed.  Or, if a Frontend needs to display the data, you have the option of using the original PDF files. 

### BlobTrigger |

### HttpTrigger |

## api-convert-resume-to-md - This API uses GenAI
This API is an Azure Blob Trigger, when invoked parses the Candidate.PDF and generates a Candidate.Md file RFP resume and copies it to and Azure Storage Container.  The target container for this API is **output-rfps-ideal-resumes-md**.  Yes, the output file is in **markdown** format

## api-resume-match - This API does not use GenAI
This API has one function **Httpis an Azure Http Trigger, that expects an **inputFileName** parameter to be passed when making a HTTP GET request.  Below is an example of what an HTTP GET request will look like:

   ~~~
     http://localhost:7071/api/HttpTrigger_Resume_Match?inputFileName=NY Housing Authority RFP_IdealResume.md
   ~~~

Below is an example of what a response body might look like:

   ~~~
     [
        {
            "name": "entry-level-construction-resume-example.md",
            "similarity": 0.5598912835121155
        },
        {
            "name": "construction-manager-resume-example.md",
            "similarity": 0.402847021818161
        },
        {
            "name": "construction-project-managerengineer-resume-example.md",
            "similarity": 0.36617550253868103
        },
        {
            "name": "construction-superintendent-resume-example.md",
            "similarity": 0.3093341886997223
        },
        {
            "name": "construction-laborer-resume-example.md",
            "similarity": 0.30616188049316406
        }
     ]
   ~~~

### HttpTrigger


## How to use these APIs
1. First you need to create an Azure Storage Account with the following containers

   ~~~
      input-rfps-pdfs
      input-candidate-resumes-pdfs
      output-rfps-ideal-resumes-md
      output-candidate-resumes-md
   ~~~

2. Next, create a local.settings.json file in the root folder of each API.  The structure of the file should be as follows:

   ~~~
      {
        "IsEncrypted": false,
        "Values": {
          "AzureWebJobsStorage": "<YOUR_STORAGE_ACCOUNT_CONNECTION_STRING>",
          "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
          "CONTAINER_STORAGE_CONNECTIONSTRING": "<YOUR_STORAGE_ACCOUNT_CONNECTION_STRING>",
          "AZURE_OPEN_API_KEY": "<YOUR_AZURE_OPEN_API_KEY>",
          "AZURE_OPEN_API_DEPLOYMENT_NAME": "<YOUR_AZURE_OPENAI_DEPLOYMENT_NAME",
          "AZURE_OPEN_API_ENDPOINT": "<YOUR_AZURE_OPENAI_ENDPOINT",
          "RFP_INPUT_PDF_CONTAINER": "input-rfps-pdfs",
          "INPUT_CANDIDATE_RESUME_PDF_CONTAINER": "input-candidate-resumes-pdfs",
          "OUTPUT_CANDIDATE_RESUME_MD_CONTAINER": "output-candidate-resumes-md",
          "OUTPUT_IDEAL_RESUME_MD_CONTAINER": "output-rfps-ideal-resumes-md",
          "stgcdwdemo_STORAGE": "<YOUR_STORAGE_ACCOUNT_CONNECTION_STRING>"
        }
      }
   ~~~

3. Run the **api-rfp-resume**, now log into your Azure Subsciption and navigate to the **storage account** and open the **input-rfps-pdfs** and upload an RFP.PDF document.  You can set breakpoints if you would like to walk through the logic.  If you have everything setup properly you will find a new file in the **output-rfps-ideal-resumes-md** folder.

4. Now, run the **api-convert-resume-to-md** and log into your Azure Subsciption and navigate to the **storage account** and open the **input-candidate-resumes-pdfs** and upload a Candidate.PDF document.  If everything is working properly you will find a new file in the **output-candidate-resumes-md**.

5. Now, you are ready to find the best **candidate resumes** that align to the **ideal RFP resumes**.  Run the **api-resume-match** and either use curl or PostMan to send and HTTP GET request, passing the name of one of the **RFP_Ideal_Resume.md** files found in the **output-rfps-ideal-resumes-md** container.

In the **api-resume-match** section above you will find an example of the HTTP GET request and a response.


