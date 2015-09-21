using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobStorage
{
    class BlobContainerOperations
    {

        async public static Task RunContainerOperations()
        {
            // Retrieve the connection string from the app.config file.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Microsoft.Azure.CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a randomly named container for sample purposes.
            CloudBlobContainer container = blobClient.GetContainerReference("sample-container" + DateTime.Now.Ticks);

            //Create the container if it does not already exist. 
            await container.CreateIfNotExistsAsync();

            await AddContainerMetadata(container);

            await ListContainerPropertiesAndMetadata(container);

            // Uncomment to delete the sample container at the end of the sample.
            //await container.DeleteAsync();
        }


        async public static Task AddContainerMetadata(CloudBlobContainer container)
        {
            // Add some metadata to the container.
            container.Metadata.Add("docType", "textDocuments");
            container.Metadata["category"] = "guidance";

            // Set the container's metadata asynchronously.
            await container.SetMetadataAsync();
        }


        async public static Task ListContainerPropertiesAndMetadata(CloudBlobContainer container)
        {
            // You must first fetch the container's attributes in order to 
            // populate the container's properties and metadata.
            await container.FetchAttributesAsync();

            // Write out container property values.
            ReflectionTools.PrintTypeProperties(container);
            ReflectionTools.PrintTypeProperties(container.Properties);

            // Enumerate the container's metadata.
            Console.WriteLine("Container metadata:");
            foreach (var metadataItem in container.Metadata)
            {
                Console.WriteLine("\tKey: {0}", metadataItem.Key);
                Console.WriteLine("\tValue: {0}", metadataItem.Value);
            }
            Console.WriteLine();
        }



    }
}
