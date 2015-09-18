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

namespace DataBlobStorageSample
{
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
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
            //JUST UNCOMMENT WHAT YOU WANT TO TRY OUT AND RUN THIS

            //Console.WriteLine("See Snapshots Work");
            //SeeSnapshotsWork();

            //Console.WriteLine("Try Out Leases");
            //TryOutLeases();

            ////Console.WriteLine("Azure Storage Blob Sample\n ");

            ////// Block blob basics
            ////Console.WriteLine("Block Blob Sample");
            ////BasicStorageBlockBlobOperationsAsync().Wait();

            ////// Page blob basics
            ////Console.WriteLine("\nPage Blob Sample");
            ////BasicStoragePageBlobOperationsAsync().Wait();

            ////Console.WriteLine("Press any key to exit");
            ////Console.ReadLine();
        }

        //these are the objects needed to access a blob in blob storage 
        //this is used by the snapshots and the leases code 
        private static CloudStorageAccount cloudStorageAccount;
        private static CloudBlobClient cloudBlobClient;
        private static CloudBlobContainer cloudBlobContainer;
        private static string cloudBlobContainerName;

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

            Console.WriteLine("5. Create a read-only snapshot of the blob");            
            CloudBlockBlob blockBlobSnapshot =  await blockBlob.CreateSnapshotAsync(null, null, null, null);
            // Clean up after the demo 
            Console.WriteLine("6. Delete block Blob and all of its snapshots");
            await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots,null,null,null);

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

        // SNAPSHOTS 
        /*
         * These code samples show how to create, list, delete, and promote snapshots on BlockBlobs. 
         * 
         * Here's one way to try this out and really see what snapshots can do.
         * 
         * Upload some text to blob storage and give it a generic name. 
         * Set the metadata on the blob to hold a file name.
         *  cloudBlockBlob.Metadata["OriginalFilename"] = fileName;
         *  cloudBlockBlob.SetMetadata();
         * Take a snapshot. 
         * 
         * Upload some different text to the same blob, set the metadata, and take another snapshot. 
         * After you do this 3 or 4 times, list the snapshots for the blob and retrieve the 
         *   metadata. This makes it really easy to see
         *   that the snapshots are different, and they each have their own metadata, 
         *   which came from the blob when they took the snapshot.
         *
         * Now for each snapshot, copy the snapshot to another blob using the 
         *   file name in the metadata as the blob name. 
         * Now you can look at each blob, and see what was in the snapshot, and you've
         *   created, listed, and promoted snapshots. 
         * 
         * */

        private static void SeeSnapshotsWork()
        {
            //call setup to set up the heirarchy of objects
            Console.WriteLine("Setting up the heirarchy of objects.");
            Setup();

            //instantiate the snapshots class 
            BlobSnapshots blobSnapshots = new BlobSnapshots();

            //this will be the name of the blob in blob storage
            string blobName = "snapshottest.txt";

            //we'll store a blob name in metadata, to be used later to copy the snapshot to a blob
            string metadataKey = "NewBlobName";
            
            //we'll iterate through this many times of making snapshots etc.
            int loopCount = 5;

            //set a reference to the blob you're going to create; this blob will have snapshots when you're done
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Create the text, upload the blob, set the metadata, take a snapshot *****");

            //create the text, upload the blob, set the metadata, and take a snapshot
            for (int i = 0; i < loopCount; i++)
            {
                Console.WriteLine("Snapshot {0}", i);
                //this will be the text in the blob
                string textToUpload = "Snapshot " + i;

                //upload the text to the blob
                cloudBlockBlob.UploadText(textToUpload);

                //set the metadata
                //later, we'll copy each snapshot to a blob, and this will be used as the blob name 
                cloudBlockBlob.Metadata[metadataKey] = "BlobSnapTest_" + i + ".txt"; 
                cloudBlockBlob.SetMetadata();

                //take the snapshot 
                //this includes the metadata of the blob at the time you take the snapshot
                //so if we change the metadata each time, then each snapshot will have unique metadata
                CloudBlockBlob newBlob = blobSnapshots.TakeASnapshot(cloudBlockBlob);

            }

            //so now you have a blob with snapshots. List them out. 

            Console.WriteLine(string.Empty);
            Console.WriteLine("Now you have a blob with snapshots. List the snapshots.");

            blobSnapshots.ListSnapshotsAndProperties(cloudBlobContainer, blobName);

            Console.WriteLine(string.Empty);
            Console.WriteLine("If you go look in blob storage, there's one file and it has {0} snapshots.", loopCount);
            Console.WriteLine("Press ENTER to continue.");
            Console.ReadLine();

            Console.WriteLine("***** Copy each snapshot into a separate blob *****");
            //Now let's copy each of the snapshots to a blob using the metadata to get the blob name
            //First get a list of the snapshots; this returns a list if IListBlobItem. 
            IEnumerable<IListBlobItem> listOfSnaps = blobSnapshots.GetListOfSnapshots(cloudBlobContainer, blobName);
            foreach (IListBlobItem blobItem in listOfSnaps)
            {
                //First, cast the blobItem as a CloudBlockBlob because IListBlobItem doesn't expose all of the properties.
                CloudBlockBlob theBlob = blobItem as CloudBlockBlob;

                //Call FetchAttributes to retrieve the metadata.
                theBlob.FetchAttributes();

                if (theBlob.IsSnapshot)
                { 
                    //Print the snapshot informatino
                    Console.WriteLine("theBlob IsSnapshot = {0}, SnapshotTime = {1}, snapshotURI = {2}",
                      theBlob.IsSnapshot, theBlob.SnapshotTime, theBlob.SnapshotQualifiedUri);
                    string newBlobName = theBlob.Metadata[metadataKey];
                    Console.WriteLine("  Copying snapshot to blob {0}", newBlobName);
                    CloudBlockBlob blobTarget = cloudBlobContainer.GetBlockBlobReference(newBlobName);
                    blobTarget.StartCopyFromBlob(theBlob);
                }
            }

            Console.WriteLine("Finished copying snapshots to blobs.");
            Console.WriteLine(string.Empty);
            //now go look in the storage account/container. Should have 6 blobs -- the original and 5 that used to be snapshots.
            Console.WriteLine("Go look in the storage account/container. {0} You should have 6 blobs -- the original with the embedded snapshots {0} and the 5 that were copied from the snapshots.{0}Press ENTER to continue.", Environment.NewLine);
            Console.ReadLine();

            //what if you want to delete one snapshot? First, find it in the list, then delete that blob.
            //let's delete the snapshot with the metadata value "BlobSnaptest_3.txt"

            Console.WriteLine("Now let's delete the fourth snapshot by looking for the right value in the metadata.");
            CloudBlockBlob snapshotToDelete = null;
            foreach (IListBlobItem blobItem in listOfSnaps)
            {
                //First, cast the blobItem as a CloudBlockBlob because IListBlobItem doesn't expose all of the properties.
                CloudBlockBlob theBlob = blobItem as CloudBlockBlob;

                //Call FetchAttributes to retrieve the metadata.
                theBlob.FetchAttributes();

                if (theBlob.IsSnapshot && theBlob.Metadata[metadataKey] == "BlobSnapTest_3.txt")
                {
                    Console.WriteLine("Found snapshot to delete");
                    snapshotToDelete = theBlob;
                    break;
                }
            }

            if (snapshotToDelete != null)
            {
                snapshotToDelete.Delete();
                Console.WriteLine("Snapshot deleted.");
            }

            Console.WriteLine(string.Empty);

            //now print out the snapshots again 
            Console.WriteLine("***** After deleting snapshot BlobSnaptest_3.txt, list of snapshots *****");
            blobSnapshots.ListSnapshotsAndProperties(cloudBlobContainer, blobName);

            //there are three other methods not used here
            //delete all snapshots for a blob
            //delete blob but only if it has no snapshots
            //delete blob and its snapshots

            AskToDeleteContainer();
        }

        // LEASES
        /*
         * This shows how to acquire a lease, change the leaseId, renew the lease, and release the lease. 
         * It then shows how to take out an infinite lease, and then break it. 
         */
        public static void TryOutLeases()
        {
            //call setup to set up the heirarchy of objects
            Console.WriteLine("Setting up the heirarchy of objects.");
            Setup();

            Console.WriteLine("Container name = {0}", cloudBlobContainerName);

            BlobLeases blobLeases = new BlobLeases();
            //this will be the name of the blob in blob storage
            string blobName = "leasetest.txt";
            //set a reference to the blob you're going to create
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            //upload some text to the blob
            cloudBlockBlob.UploadText("Now is the winter of our discontent made glorious summer by this son of York.");

            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Acquire a lease for 15 seconds *****");

            //this prints out the properties when it acquires the lesae 
            string leaseId = blobLeases.AcquireLease(30, cloudBlockBlob);
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, leaseId);

            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Change the lease *****");
            //change the lease Id
            string newLeaseId = blobLeases.ChangeLease(cloudBlockBlob, leaseId);
            //put it back in leaseId, which is used in the next examples
            leaseId = newLeaseId;
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, leaseId);
            
            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Renew the lease *****");

            //now renew the lease -- this uses the same interval you used when you originally leased the blob
            blobLeases.RenewLease(cloudBlockBlob, leaseId);
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, leaseId);

            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Release the lease *****");

            blobLeases.ReleaseLease(cloudBlockBlob, leaseId);
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, leaseId);

            Console.WriteLine(string.Empty);
            Console.WriteLine("***** Acquire an infinite lease, then break it *****");

            Console.WriteLine("     Acquire an infinite lease");
            //acquire an infinite lease
            leaseId = blobLeases.AcquireLease(0, cloudBlockBlob);
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, leaseId);

            Console.WriteLine("     Break the lease");
            //break the lease; set the lease break time to 1 second
            blobLeases.BreakLease(cloudBlockBlob, 1);
            blobLeases.DisplayLeaseProperties(cloudBlockBlob, null);

            AskToDeleteContainer();

        }

        /// <summary>
        /// There is a heirarchy of objects that you have to retrieve when acting against blob storage.
        /// First, you have to get a reference to the storage account.
        /// second, you have to get a reference to the cloud blob client.
        /// Third, you need a reference to the container where the blob resides.
        /// With a reference to the container, you can get a reference to the blob and act on it.
        /// This sets up the static variables used in this class for the heirarchy of objectds.
        /// </summary>
        private static void Setup()
        {
            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            cloudStorageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //set the reference to the cloud blob client 
            cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            //set the name of the container using a guid so you have a good chance that nobody else is using it  
            cloudBlobContainerName = string.Format("demotest-{0}", System.Guid.NewGuid().ToString().Substring(0, 12));

            //set a reference to the container, then create the container if it doesn't exist 
            cloudBlobContainer = cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            try
            {
                cloudBlobContainer.CreateIfNotExists();
            }
            catch (StorageException)
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Ask if they want to delete the container. If they say yes, delete it. 
        /// </summary>
        private static void AskToDeleteContainer()
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Finished running snapshot samples; delete container (Y/N)?");
            string response = Console.ReadLine();
            if (response.ToUpper() == "Y")
            {
                cloudBlobContainer.Delete();
            }
        }


    }
}
