//----------------------------------------------------------------------------------
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

namespace BlobStorage
{
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Azure.Storage.Sas;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Getting started samples for Blob storage
    /// These samples show how to:
    /// - Use shared key authentication to perform basic operations with block blobs, including creating a container, uploading, 
    ///   downloading, listing, and deleting blobs, and creating a snapshot of a blob, and deleting a container.
    /// - Use an account SAS (shared access signature) to perform the same operations.  
    /// - Use shared key authentication to perform basic operations with page blobs, including creating a page blob, writing to the 
    ///   page blob, and listing page blobs in a container.
    /// </summary>
    public static class GettingStarted
    {
        // Prefix for containers created by the sample.
        private const string ContainerPrefix = "sample-";
        
        /// <summary>
        /// Calls each of the methods in the getting started samples.
        /// </summary>
        public static void CallBlobGettingStartedSamples()
        {
            // Block blob basics
            Console.WriteLine("Block Blob Sample");
            BasicStorageBlockBlobOperationsAsync().Wait();

            // Block Blobs basics using Account SAS
            BasicStorageBlockBlobOperationsWithAccountSASAsync().Wait();

            // Page blob basics
            Console.WriteLine("\nPage Blob Sample");
            BasicStoragePageBlobOperationsAsync().Wait();
        }

        /// <summary>
        /// Basic operations to work with block blobs
        /// </summary>
        /// <returns>A Task object.</returns>
        private static async Task BasicStorageBlockBlobOperationsAsync()
        {
            const string ImageToUpload = "HelloWorld.png";
            string containerName = ContainerPrefix + Guid.NewGuid();

            // Retrieve storage account information from connection string
            BlobServiceClient blobServiceClient = Common.CreateblobServiceClientFromConnectionString();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            try
            {
                // The call below will fail if the sample is configured to use the azurite in the connection string, but 
                // the azurite is not running.
                await container.CreateIfNotExistsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("If you're running with the default connection string, ensure you have started the Azurite emulator. Press the Windows key and type Azure Storage to select and run it from the list of apps. Then restart the sample.");
                Console.ReadLine();
                throw;
            }

            // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
            // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
            // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
            // using: https://[InsertYourblobServiceClientNameHere].blob.core.windows.net/democontainer/HelloWorld.png
            // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Upload a BlockBlob to the newly created container
            Console.WriteLine("2. Uploading BlockBlob");
            BlobClient blobClient = container.GetBlobClient(ImageToUpload);
            
            // Set the blob's content type so that the browser knows to treat it as an image.
            await blobClient.UploadAsync(File.OpenRead(ImageToUpload));

            // List all the blobs in the container.
            /// Note that the ListBlobs method is called synchronously, for the purposes of the sample. However, in a real-world
            /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
            Console.WriteLine("3. List Blobs in Container");

            foreach (var blob in container.GetBlobs())
            {
                // Blob type will be BlobClient, CloudPageBlob or BlobClientDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("- {0} (type: {1})", blob.Name, blob.GetType());
            }

            // Download a blob to your file system
            Console.WriteLine("4. Download Blob from {0}", blobClient.Uri.AbsoluteUri);
            await blobClient.DownloadToAsync(string.Format("./CopyOf{0}", ImageToUpload));
            
            // Create a read-only snapshot of the blob
            Console.WriteLine("5. Create a read-only snapshot of the blob");
            var blockBlobSnapshot = await blobClient.CreateSnapshotAsync();

            // Clean up after the demo. This line is not strictly necessary as the container is deleted in the next call.
            // It is included for the purposes of the example. 
            Console.WriteLine("6. Delete block blob and all of its snapshots");
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

            // Note that deleting the container also deletes any blobs in the container, and their snapshots.
            // In the case of the sample, we delete the blob and its snapshots, and then the container,
            // to show how to delete each kind of resource.
            Console.WriteLine("7. Delete Container");
            await container.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Basic operations to work with block blobs
        /// </summary>
        /// <returns>A Task object.</returns>
        private static async Task BasicStorageBlockBlobOperationsWithAccountSASAsync()
        {
            const string ImageToUpload = "HelloWorld.png";
            string containerName = ContainerPrefix + Guid.NewGuid();
            BlobServiceClient blobServiceClient = Common.CreateblobServiceClientFromConnectionString();
            // Get an account SAS token.
            Uri sasToken = GetAccountSASToken(blobServiceClient);

            // Informational: Print the Account SAS Signature and Token.
            Console.WriteLine();
            //Console.WriteLine("Account SAS Signature: " + sasToken.Signature);
            Console.WriteLine("Account SAS Token: " + sasToken.Query);
            Console.WriteLine();

            // Get the URI for the container.
            Uri containerUri = GetContainerUri(containerName);

            // Get a reference to a container using the URI and the SAS token.
            var sasUri = new UriBuilder(containerUri);
            sasUri.Query = sasToken.Query.Substring(1, sasToken.Query.Length - 1);
            var container = new BlobContainerClient(sasUri.Uri);

            try
            {
                // Create a container for organizing blobs within the storage account.
                Console.WriteLine("1. Creating Container using Account SAS");

                await container.CreateIfNotExistsAsync();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("If you are running with the default configuration, please make sure you have started the Azurite. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            try
            {
                // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
                // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
                // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
                // using: https://[InsertYourblobServiceClientNameHere].blob.core.windows.net/democontainer/HelloWorld.png
                // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                // Upload a BlockBlob to the newly created container
                Console.WriteLine("2. Uploading BlockBlob");
                BlobClient blobClient = container.GetBlobClient(ImageToUpload);
                await blobClient.UploadAsync(File.OpenRead(ImageToUpload));

                // List all the blobs in the container 
                Console.WriteLine("3. List Blobs in Container");           
                var resultSegment = container.GetBlobsAsync();
 
                await foreach (var blob in resultSegment)
                {
                    // Blob type will be BlobClient, CloudPageBlob or BlobClientDirectory
                    Console.WriteLine("{0} (type: {1}", blob.Name, blob.GetType());
                }

                // Download a blob to your file system
                Console.WriteLine("4. Download Blob from {0}", blobClient.Uri.AbsoluteUri);
                await blobClient.DownloadToAsync(string.Format("./CopyOf{0}", ImageToUpload));
                
                // Create a read-only snapshot of the blob
                Console.WriteLine("5. Create a read-only snapshot of the blob");
                var blockBlobSnapshot = await blobClient.CreateSnapshotAsync();

                // Delete the blob and its snapshots.
                Console.WriteLine("6. Delete block Blob and all of its snapshots");
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            finally
            {
                // Clean up after the demo.
                // Note that it is not necessary to delete all of the blobs in the container first; they will be deleted
                // with the container. 
                Console.WriteLine("7. Delete Container");
                await container.DeleteIfExistsAsync();
            }
        }

        /// <summary>
        /// Basic operations to work with page blobs
        /// </summary>
        /// <returns>A Task object.</returns>
        private static async Task BasicStoragePageBlobOperationsAsync()
        {
            const string PageBlobName = "samplepageblob";
            string containerName = ContainerPrefix + Guid.NewGuid();

            // Retrieve storage account information from connection string
            BlobServiceClient blobServiceClient = Common.CreateblobServiceClientFromConnectionString();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();

            // Create a page blob in the newly created container.  
            Console.WriteLine("2. Creating Page Blob");
            PageBlobClient pageBlob = container.GetPageBlobClient(PageBlobName);
            await pageBlob.CreateAsync(512 * 2 /*size*/); // size needs to be multiple of 512 bytes

            // Write to a page blob 
            Console.WriteLine("3. Write to a Page Blob");
            byte[] samplePagedata = new byte[512];
            Random random = new Random();
            random.NextBytes(samplePagedata);
            using (var stream = new MemoryStream(samplePagedata))
            {
                await pageBlob.UploadPagesAsync(stream, 0);
            }

            // List all blobs in this container. Because a container can contain a large number of blobs the results 
            // are returned in segments with a maximum of 5000 blobs per segment. You can define a smaller maximum segment size
            // using the maxResults parameter on ListBlobsSegmentedAsync.
            Console.WriteLine("4. List Blobs in Container");
            var resultSegment = container.GetBlobsAsync();

            await foreach (var blob in resultSegment)
            {
                // Blob type will be BlobClient, CloudPageBlob or BlobClientDirectory
                Console.WriteLine("{0} (type: {1}", blob.Name, blob.GetType());
            }

            // Read from a page blob
            Console.WriteLine("5. Read from a Page Blob");
            var httpRange = new HttpRange(0, samplePagedata.Count());
            var downloadInfo = await pageBlob.DownloadAsync(httpRange);

            // Clean up after the demo 
            Console.WriteLine("6. Delete page Blob");
            await pageBlob.DeleteIfExistsAsync();

            Console.WriteLine("7. Delete Container");
            await container.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Returns the container URI.  
        /// </summary>
        /// <param name="containerName">The container name</param>
        /// <returns>A URI for the container.</returns>
        private static Uri GetContainerUri(string containerName)
        {
            // Retrieve storage account information from connection string
            BlobServiceClient blobServiceClient = Common.CreateblobServiceClientFromConnectionString();

            return blobServiceClient.GetBlobContainerClient(containerName).Uri;
        }

        /// <summary>
        /// Creates an Account SAS Token
        /// </summary>
        /// <returns>A SAS token.</returns>
        private static Uri GetAccountSASToken(BlobServiceClient blobServiceClient)
        {
            // Create a new access policy for the account with the following properties:
            // Permissions: Read, Write, List, Create, Delete
            // ResourceType: Container
            // Expires in 24 hours
            var sasToken = blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read | AccountSasPermissions.Create | AccountSasPermissions.Write | AccountSasPermissions.List | AccountSasPermissions.Delete, DateTimeOffset.UtcNow.AddHours(1), AccountSasResourceTypes.All);
           
            // Return the SASToken
            return sasToken;
        }
    }
}
