using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;


namespace BlobStorage
{
    public class BlobServiceClientOperations
    {
        async public static Task RunServiceClientOperations()
        {
            // Retrieve storage account information from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Microsoft.Azure.CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            await ConfigureAnalyticsForBlobService(blobClient);
            await ReadServiceProperties(blobClient);
        }

        async public static Task ConfigureAnalyticsForBlobService(CloudBlobClient blobClient)
        {
            // Enable analytics logging and set retention policy to 10 days. 
            ServiceProperties properties = new ServiceProperties();
            properties.Logging.LoggingOperations = LoggingOperations.All;
            properties.Logging.RetentionDays = 21;
            // Logging version is required.
            properties.Logging.Version = "1.0";

            // Configure service properties for metrics; these must be set at the same time as the logging properties.
            properties.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            properties.HourMetrics.RetentionDays = 14;
            properties.HourMetrics.Version = "1.0";

            properties.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            properties.MinuteMetrics.RetentionDays = 7;
            properties.MinuteMetrics.Version = "1.0";

            // Set the default service version to be used for anonymous requests.
            properties.DefaultServiceVersion = "2015-02-21";

            // Set the service properties.
            await blobClient.SetServicePropertiesAsync(properties);
        }


        async public static Task ReadServiceProperties(CloudBlobClient blobClient)
        {
            //Get service properties for the Blob service.
            ServiceProperties props = await blobClient.GetServicePropertiesAsync();

            Console.WriteLine("Default service version: {0}", props.DefaultServiceVersion);
            Console.WriteLine();

            Console.WriteLine("Logging operations: {0}", props.Logging.LoggingOperations);
            Console.WriteLine("Retention policy for logs: {0}", props.Logging.RetentionDays);
            Console.WriteLine("Storage Analytics version for logging: {0}", props.Logging.Version);
            Console.WriteLine();

            Console.WriteLine("Metrics level for hourly metrics: {0}", props.HourMetrics.MetricsLevel);
            Console.WriteLine("Retention policy for hourly metrics: {0}", props.HourMetrics.RetentionDays);
            Console.WriteLine("Storage Analytics version for hourly metrics: {0}", props.HourMetrics.Version);
            Console.WriteLine();

            Console.WriteLine("Metrics level for minute metrics: {0}", props.MinuteMetrics.MetricsLevel);
            Console.WriteLine("Retention policy for minute metrics: {0}", props.MinuteMetrics.RetentionDays);
            Console.WriteLine("Storage Analytics version for minute metrics: {0}", props.MinuteMetrics.Version);
            Console.WriteLine();
        }
    
    
    }
}
