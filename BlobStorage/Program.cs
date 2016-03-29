﻿//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

namespace DataBlobStorageSample
{
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Auth;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure Storage Blob Sample - Demonstrate how to use the Blob Storage service. 
    /// Blob storage stores unstructured data such as text, binary data, documents or media files. 
    /// Blobs can be accessed from anywhere in the world via HTTP or HTTPS.
    ///
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Blobs - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/
    /// - Blob Service Concepts - http://msdn.microsoft.com/en-us/library/dd179376.aspx 
    /// - Blob Service REST API - http://msdn.microsoft.com/en-us/library/dd135733.aspx
    /// - Blob Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// - Delegating Access with Shared Access Signatures - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-shared-access-signature-part-1/
    /// - Storage Emulator - http://msdn.microsoft.com/en-us/library/azure/hh403989.aspx
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run using either the Azure Storage Emulator that installs as part of this SDK - or by
        // updating the App.Config file with your AccountName and Key. 
        // 
        // To run the sample using the Storage Emulator (default option)
        //      1. Start the Azure Storage Emulator (once only) by pressing the Start button or the Windows key and searching for it
        //         by typing "Azure Storage Emulator". Select it from the list of applications to start it.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // To run the sample using the Storage Service
        //      1. Open the app.config file and comment out the connection string for the emulator (UseDevelopmentStorage=True) and
        //         uncomment the connection string for the storage service (AccountName=[]...)
        //      2. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //      3. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Blob Sample\n ");

            // Block blob basics
            Console.WriteLine("Block Blob Sample");
            BasicStorageBlockBlobOperationsAsync().Wait();

            // Block Blobs basics using Account SAS
            BasicStorageBlockBlobOperationsWithAccountSASAsync().Wait();

            // Page blob basics
            Console.WriteLine("\nPage Blob Sample");
            BasicStoragePageBlobOperationsAsync().Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        /// <summary>
        /// Basic operations to work with block blobs
        /// </summary>
        /// <returns>Task<returns>
        private static async Task BasicStorageBlockBlobOperationsAsync()
        {
            const string imageToUpload = "HelloWorld.png";
            string blockBlobContainerName = "demoblockblobcontainer-" + Guid.NewGuid();

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference(blockBlobContainerName);
            try
            {
                await container.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
            // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
            // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
            // using: https://[InsertYourStorageAccountNameHere].blob.core.windows.net/democontainer/HelloWorld.png
            // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Upload a BlockBlob to the newly created container
            Console.WriteLine("2. Uploading BlockBlob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(imageToUpload);
            await blockBlob.UploadFromFileAsync(imageToUpload, FileMode.Open);

            // List all the blobs in the container 
            Console.WriteLine("3. List Blobs in Container");
            foreach (IListBlobItem blob in container.ListBlobs())
            {
                // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("- {0} (type: {1})", blob.Uri, blob.GetType());
            }

            // Download a blob to your file system
            Console.WriteLine("4. Download Blob from {0}", blockBlob.Uri.AbsoluteUri);
            await blockBlob.DownloadToFileAsync(string.Format("./CopyOf{0}", imageToUpload), FileMode.Create);

            // Create a read-only snapshot of the blob
            Console.WriteLine("5. Create a read-only snapshot of the blob");            
            CloudBlockBlob blockBlobSnapshot =  await blockBlob.CreateSnapshotAsync(null, null, null, null);
            
            // Clean up after the demo 
            Console.WriteLine("6. Delete block Blob and all of its snapshots");
            await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots,null,null,null);

            Console.WriteLine("7. Delete Container");
            await container.DeleteIfExistsAsync();
        }
        /// <summary>
        /// Basic operations to work with block blobs
        /// </summary>
        /// <returns>Task<returns>
        private static async Task BasicStorageBlockBlobOperationsWithAccountSASAsync()
        {
            const string imageToUpload = "HelloWorld.png";
            string blockBlobContainerName = "demoblockblobcontainer-" + Guid.NewGuid();

            // Call GetAccountSASToken to get a sasToken based on the Storage Account Key
            string sasToken = GetAccountSASToken();
          
            // Create an AccountSAS from the SASToken
            StorageCredentials accountSAS = new StorageCredentials(sasToken);

            //Informational: Print the Account SAS Signature and Token
            Console.WriteLine();
            Console.WriteLine("Account SAS Signature: " + accountSAS.SASSignature);
            Console.WriteLine("Account SAS Token: " + accountSAS.SASToken);
            Console.WriteLine();
            
            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container using Account SAS");

            // Get the Container Uri  by passing the Storage Account and the container Name
            Uri ContainerUri = GetContainerSASUri(blockBlobContainerName);

            // Create a CloudBlobContainer by using the Uri and the sasToken
            CloudBlobContainer container = new CloudBlobContainer(ContainerUri, new StorageCredentials(sasToken));
            try
            {
                await container.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
            // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
            // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
            // using: https://[InsertYourStorageAccountNameHere].blob.core.windows.net/democontainer/HelloWorld.png
            // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Upload a BlockBlob to the newly created container
            Console.WriteLine("2. Uploading BlockBlob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(imageToUpload);
            await blockBlob.UploadFromFileAsync(imageToUpload, FileMode.Open);

            // List all the blobs in the container 
            Console.WriteLine("3. List Blobs in Container");
            foreach (IListBlobItem blob in container.ListBlobs())
            {
                // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("- {0} (type: {1})", blob.Uri, blob.GetType());
            }

            // Download a blob to your file system
            Console.WriteLine("4. Download Blob from {0}", blockBlob.Uri.AbsoluteUri);
            await blockBlob.DownloadToFileAsync(string.Format("./CopyOf{0}", imageToUpload), FileMode.Create);

            // Create a read-only snapshot of the blob
            Console.WriteLine("5. Create a read-only snapshot of the blob");
            CloudBlockBlob blockBlobSnapshot = await blockBlob.CreateSnapshotAsync(null, null, null, null);

            // Clean up after the demo 
            Console.WriteLine("6. Delete block Blob and all of its snapshots");
            await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null);

            Console.WriteLine("7. Delete Container");
            await container.DeleteIfExistsAsync();

        }
        /// <summary>
        /// Basic operations to work with page blobs
        /// </summary>
        /// <returns>Task</returns>
        private static async Task BasicStoragePageBlobOperationsAsync()
        {
            const string PageBlobName = "samplepageblob";
            string pageBlobContainerName = "demopageblobcontainer-" + Guid.NewGuid();

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference(pageBlobContainerName);
            await container.CreateIfNotExistsAsync();

            // Create a page blob in the newly created container.  
            Console.WriteLine("2. Creating Page Blob");
            CloudPageBlob pageBlob = container.GetPageBlobReference(PageBlobName);
            await pageBlob.CreateAsync(512 * 2 /*size*/); // size needs to be multiple of 512 bytes

            // Write to a page blob 
            Console.WriteLine("2. Write to a Page Blob");
            byte[] samplePagedata = new byte[512];
            Random random = new Random();
            random.NextBytes(samplePagedata);
            await pageBlob.UploadFromByteArrayAsync(samplePagedata, 0, samplePagedata.Length);

            // List all blobs in this container. Because a container can contain a large number of blobs the results 
            // are returned in segments (pages) with a maximum of 5000 blobs per segment. You can define a smaller size
            // using the maxResults parameter on ListBlobsSegmentedAsync.
            Console.WriteLine("3. List Blobs in Container");
            BlobContinuationToken token = null;
            do
            {
                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(token);
                token = resultSegment.ContinuationToken;
                foreach (IListBlobItem blob in resultSegment.Results)
                {
                    // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                    Console.WriteLine("{0} (type: {1}", blob.Uri, blob.GetType());
                }
            } while (token != null);

            // Read from a page blob
            //Console.WriteLine("4. Read from a Page Blob");
            int bytesRead = await pageBlob.DownloadRangeToByteArrayAsync(samplePagedata, 0, 0, samplePagedata.Count());

            // Clean up after the demo 
            Console.WriteLine("6. Delete page Blob");
            await pageBlob.DeleteIfExistsAsync();

            Console.WriteLine("7. Delete Container");
            await container.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Creates the Container SAS Uri.  
        /// </summary>
        /// <param name="storageAccount">The storage account object</param>
        /// <param name="containerName">The BlockBlob container Name</param>
        /// <returns>Uri</returns>
        private static Uri GetContainerSASUri(string containerName)
        {
            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            return new Uri(storageAccount.BlobStorageUri.PrimaryUri.OriginalString + containerName);
        }

        /// <summary>
        /// Creates an Account SAS Token
        /// </summary>
        /// <param name="storageAccount">The storage account object</param>
        /// <returns>sasToken</returns>
        private static string GetAccountSASToken()
        {
            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a new access policy for the account with the following properties:
            // Permissions: Read, Write, List, Create, Delete
            // ResourceType: Container
            // Expires in 24 hours
            // Protocols: Https or Http (As Emulator does not yet support Https)
            SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
            {
                Permissions = SharedAccessAccountPermissions.Read | SharedAccessAccountPermissions.Write | SharedAccessAccountPermissions.List | SharedAccessAccountPermissions.Create | SharedAccessAccountPermissions.Delete,
                Services = SharedAccessAccountServices.Blob,
                ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Protocols = SharedAccessProtocol.HttpsOrHttp
            };

            // Create new storage credentials using the SAS token.
            string sasToken = storageAccount.GetSharedAccessSignature(policy);

            // Return the SASToken
            return sasToken;
        }

        /// <summary>
        /// Validates the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string</param>
        /// <returns>CloudStorageAccount object</returns>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }
    }
}
