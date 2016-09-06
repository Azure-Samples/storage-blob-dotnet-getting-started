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
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

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
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString();

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            try
            {
                // The call below will fail if the sample is configured to use the storage emulator in the connection string, but 
                // the emulator is not running.
                // Change the retry policy for this call so that if it fails, it fails quickly.
                BlobRequestOptions requestOptions = new BlobRequestOptions() { RetryPolicy = new NoRetry() };
                await container.CreateIfNotExistsAsync(requestOptions, null);
            }
            catch (StorageException)
            {
                Console.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
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
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageToUpload);

            // Set the blob's content type so that the browser knows to treat it as an image.
            blockBlob.Properties.ContentType = "image/png";
            await blockBlob.UploadFromFileAsync(ImageToUpload);

            // List all the blobs in the container.
            /// Note that the ListBlobs method is called synchronously, for the purposes of the sample. However, in a real-world
            /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
            Console.WriteLine("3. List Blobs in Container");
            foreach (IListBlobItem blob in container.ListBlobs())
            {
                // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("- {0} (type: {1})", blob.Uri, blob.GetType());
            }

            // Download a blob to your file system
            Console.WriteLine("4. Download Blob from {0}", blockBlob.Uri.AbsoluteUri);
            await blockBlob.DownloadToFileAsync(string.Format("./CopyOf{0}", ImageToUpload), FileMode.Create);

            // Create a read-only snapshot of the blob
            Console.WriteLine("5. Create a read-only snapshot of the blob");
            CloudBlockBlob blockBlobSnapshot = await blockBlob.CreateSnapshotAsync(null, null, null, null);

            // Clean up after the demo. This line is not strictly necessary as the container is deleted in the next call.
            // It is included for the purposes of the example. 
            Console.WriteLine("6. Delete block blob and all of its snapshots");
            await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null);

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

            // Get an account SAS token.
            string sasToken = GetAccountSASToken();

            // Use the account SAS token to create authentication credentials.
            StorageCredentials accountSAS = new StorageCredentials(sasToken);

            // Informational: Print the Account SAS Signature and Token.
            Console.WriteLine();
            Console.WriteLine("Account SAS Signature: " + accountSAS.SASSignature);
            Console.WriteLine("Account SAS Token: " + accountSAS.SASToken);
            Console.WriteLine();

            // Get the URI for the container.
            Uri containerUri = GetContainerUri(containerName);

            // Get a reference to a container using the URI and the SAS token.
            CloudBlobContainer container = new CloudBlobContainer(containerUri, accountSAS);

            try
            {
                // Create a container for organizing blobs within the storage account.
                Console.WriteLine("1. Creating Container using Account SAS");

                await container.CreateIfNotExistsAsync();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("If you are running with the default configuration, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            try
            {
                // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
                // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
                // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
                // using: https://[InsertYourStorageAccountNameHere].blob.core.windows.net/democontainer/HelloWorld.png
                // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                // Upload a BlockBlob to the newly created container
                Console.WriteLine("2. Uploading BlockBlob");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageToUpload);
                await blockBlob.UploadFromFileAsync(ImageToUpload);

                // List all the blobs in the container 
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
                }
                while (token != null);

                // Download a blob to your file system
                Console.WriteLine("4. Download Blob from {0}", blockBlob.Uri.AbsoluteUri);
                await blockBlob.DownloadToFileAsync(string.Format("./CopyOf{0}", ImageToUpload), FileMode.Create);

                // Create a read-only snapshot of the blob
                Console.WriteLine("5. Create a read-only snapshot of the blob");
                CloudBlockBlob blockBlobSnapshot = await blockBlob.CreateSnapshotAsync(null, null, null, null);

                // Delete the blob and its snapshots.
                Console.WriteLine("6. Delete block Blob and all of its snapshots");
                await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null);
            }
            catch (Exception e)
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
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString();

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            Console.WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
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
            // are returned in segments with a maximum of 5000 blobs per segment. You can define a smaller maximum segment size
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
            }
            while (token != null);

            // Read from a page blob
            Console.WriteLine("4. Read from a Page Blob");
            int bytesRead = await pageBlob.DownloadRangeToByteArrayAsync(samplePagedata, 0, 0, samplePagedata.Count());

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
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString();

            return storageAccount.CreateCloudBlobClient().GetContainerReference(containerName).Uri;
        }

        /// <summary>
        /// Creates an Account SAS Token
        /// </summary>
        /// <returns>A SAS token.</returns>
        private static string GetAccountSASToken()
        {
            // Retrieve storage account information from connection string
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString();

            // Create a new access policy for the account with the following properties:
            // Permissions: Read, Write, List, Create, Delete
            // ResourceType: Container
            // Expires in 24 hours
            // Protocols: HTTPS or HTTP (note that the storage emulator does not support HTTPS)
            SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
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
    }
}
