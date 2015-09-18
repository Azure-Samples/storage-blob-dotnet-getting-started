using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace BlobStorage
{
    public class BlobListingOperations
    {
        
        async public static Task RunListingOperations()
        {
            // Retrieve the connection string from the app.config file.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Microsoft.Azure.CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a randomly named container for sample purposes.
            CloudBlobContainer container = blobClient.GetContainerReference("sample-container" + DateTime.Now.Ticks);

            //Create the container if it does not already exist. 
            await container.CreateIfNotExistsAsync();

            // Container listing operations

            ListContainersInAccount(blobClient);
            ListContainersWithPrefix(blobClient);

            // Blob listing operations
            
            // Create some small block blobs in the container, in a flat structure.
            CreateSequentiallyNamedBlockBlobs(container, 20).Wait();

            // List blobs in the container in pages, using a flat listing. The flat listing includes snapshots.
            ListBlobsWithFlatListing(container).Wait();

            // Create some additional small block blobs, in a nested structure. Specify the number of virtual directory levels and the number
            // of blobs per level.
            CreateNestedBlockBlobs(container, 4, 3).Wait();

            // List blobs in the container in pages, in a hierarchical listing. The hierarchical listing does not include snapshots.
            ListBlobsRecursively(container, "").Wait();

            // Uncomment to delete the sample container at the end of the sample.
            //await container.DeleteAsync();
        }

        public static void ListContainersInAccount(CloudBlobClient blobClient)
        {
            // List all containers in this storage account.
            Console.WriteLine("List all containers in account:");
            foreach (var container in blobClient.ListContainers())
            {
                Console.WriteLine("\tContainer:" + container.Name);
            }
            Console.WriteLine();
        }

        public static void ListContainersWithPrefix(CloudBlobClient blobClient)
        {
            // List containers in this storage account whose names begin with the specified prefix.
            // Include container metadata in the listing details.
            // Note that requesting the container's metadata as part of the listing operation 
            // populates the metadata, so it's not necessary to call FetchAttributes().
            Console.WriteLine("List all containers beginning with defined container prefix, plus any metadata:");
            foreach (var container in blobClient.ListContainers("sample-container", ContainerListingDetails.Metadata))
            {
                Console.WriteLine("\tContainer:" + container.Name);
                //Write metadata keys and values.
                foreach (var metadataItem in container.Metadata)
                {
                    Console.WriteLine("\t\tMetadata key: " + metadataItem.Key);
                    Console.WriteLine("\t\tMetadata value: " + metadataItem.Value);
                }
            }
            Console.WriteLine();
        }

        async public static Task CreateSequentiallyNamedBlockBlobs(CloudBlobContainer container, Int16 numberOfBlobs)
        {
            CloudBlockBlob blob;
            string blobName = "";
            MemoryStream msWrite;

            for (int i = 1; i <= numberOfBlobs; i++)
            {
                //Format string for blob name.
                blobName = i.ToString("00000") + ".txt";

                //Get a reference to the blob.
                blob = container.GetBlockBlobReference(blobName);

                //Write the name of the blob to its contents as well.
                msWrite = new MemoryStream(Encoding.UTF8.GetBytes("This is blob " + blobName + "."));
                msWrite.Position = 0;
                using (msWrite)
                {
                    await blob.UploadFromStreamAsync(msWrite);
                }
            }
        }

        async public static Task CreateNestedBlockBlobs(CloudBlobContainer container, Int16 numberOfLevels, Int16 numberOfBlobsPerLevel)
        {
            CloudBlockBlob blob;
            MemoryStream msWrite;
            string blobName = "";
            string virtualDirName = "";

            //Create a nested blob structure.
            for (int i = 1; i <= numberOfLevels; i++)
            {
                //Construct the virtual directory name, which becomes part of the blob name.
                virtualDirName += String.Format("level{0}{1}", i, container.ServiceClient.DefaultDelimiter);
                for (int j = 1; j <= numberOfBlobsPerLevel; j++)
                {
                    //Construct string for blob name.
                    blobName = virtualDirName + String.Format("{0}-{1}.txt", i, j.ToString("00000"));

                    //Get a reference to the blob.
                    blob = container.GetBlockBlobReference(blobName);

                    //Write the blob URI to its contents.
                    msWrite = new MemoryStream(Encoding.UTF8.GetBytes("Absolute URI to blob: " + blob.StorageUri.PrimaryUri + "."));
                    msWrite.Position = 0;
                    using (msWrite)
                    {
                        await blob.UploadFromStreamAsync(msWrite);
                    }
                }
            }
        }

        async public static Task ListBlobsWithFlatListing(CloudBlobContainer container)
        {
            // List blobs to the console window, with paging.
            Console.WriteLine("List blobs in pages using flat listing:");

            int i = 0;
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            // Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
            // When the continuation token is null, the last page has been returned, so exit the loop.
            do
            {
                // This overload allows control of the page size. You can return all remaining results by passing null for the maxResults parameter, 
                // or by calling a different overload.
                resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                if (resultSegment.Results.Count<IListBlobItem>() > 0) { Console.WriteLine("Page {0}:", ++i); }
                foreach (var blobItem in resultSegment.Results)
                {
                    Console.WriteLine("\t{0}", blobItem.StorageUri.PrimaryUri);
                }
                Console.WriteLine();

                // Get the continuation token. If it is non-null, return the next page of results.
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
        }

        async public static Task ListBlobsRecursively(CloudBlobContainer container, string blobPrefix)
        {
            Console.WriteLine();
            Console.WriteLine("Current blob prefix: {0}", blobPrefix);
            Console.WriteLine();

            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            // Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
            // When the continuation token is null, the last page has been returned, so exit the loop.
            do
            {
                // This overload allows control of the page size. You can return all remaining results by passing null for the maxResults parameter, 
                // or by calling a different overload.
                resultSegment = await container.ListBlobsSegmentedAsync(blobPrefix, false, BlobListingDetails.None, null, continuationToken, null, null);
                foreach (IListBlobItem blobItem in resultSegment.Results)
                {
                    Console.WriteLine("\t{0}", blobItem.StorageUri.PrimaryUri);
                    
                    if (blobItem is CloudBlobDirectory)
                    {
                        string blobItemUri = blobItem.StorageUri.PrimaryUri.ToString();
                        string blobItemContainerUri = blobItem.Container.StorageUri.PrimaryUri.ToString();

                        // Get the name of the virtual directory to use as the prefix for the next listing operation.
                        // TODO: is there a better way to do this than with string parsing???
                        blobPrefix = blobItemUri.Substring(blobItemContainerUri.Length + 1, blobItemUri.Length - (blobItemContainerUri.Length + 1));

                        await ListBlobsRecursively(container, blobPrefix);
                    }
                }
                Console.WriteLine();

                // Get the continuation token. If it is non-null, return the next page of results.
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
        }

    }
}
