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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;

    /// <summary>
    /// Advanced samples for Blob storage, including samples demonstrating a variety of client library classes and methods.
    /// </summary>
    public static class Advanced
    {
        // Prefix for containers created by the sample.
        private const string ContainerPrefix = "sample-";

        // Prefix for blob created by the sample.
        private const string BlobPrefix = "sample-blob-";

        /// <summary>
        /// Calls the advanced samples for Blob storage.
        /// </summary>
        /// <returns>A Task object.</returns>
        public static async Task CallBlobAdvancedSamples()
        {
            CloudStorageAccount storageAccount;

            try
            {
                storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Create service client for credentialed access to the Blob service.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = null;
            ServiceProperties userServiceProperties = null;

            try
            {
                // Save the user's current service property/storage analytics settings.
                // This ensures that the sample does not permanently overwrite the user's analytics settings.
                // Note however that logging and metrics settings will be modified for the duration of the sample.
                userServiceProperties = await blobClient.GetServicePropertiesAsync();

                // Get a reference to a sample container.
                container = await CreateSampleContainerAsync(blobClient);

                // Call Blob service client samples. 
                await CallBlobClientSamples(blobClient);

                // Call blob container samples using the sample container just created.
                await CallContainerSamples(container);

                // Call blob samples using the same sample container.
                await CallBlobSamples(container);

                // Call shared access signature samples (both container and blob).
                await CallSasSamples(container);

                // CORS Rules
                await CorsSample(blobClient);

                // Page Blob Ranges
                await PageRangesSample(container);
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            finally
            {
                // Delete the sample container created by this session.
                if (container != null)
                {
                    await container.DeleteIfExistsAsync();
                }

                // The sample code deletes any containers it created if it runs completely. However, if you need to delete containers 
                // created by previous sessions (for example, if you interrupted the running code before it completed), 
                // you can uncomment and run the following line to delete them.

                //await DeleteContainersWithPrefix(blobClient, ContainerPrefix);

                // Return the service properties/storage analytics settings to their original values.
                await blobClient.SetServicePropertiesAsync(userServiceProperties);
            }
        }
        
        /// <summary>
        /// Calls samples that demonstrate how to use the Blob service client (CloudBlobClient) object.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallBlobClientSamples(CloudBlobClient blobClient)
        {
            // Create a buffer manager for the Blob service client. The buffer manager enables the Blob service client to
            // re-use an existing buffer across multiple operations.
            blobClient.BufferManager = new WCFBufferManagerAdapter(
                BufferManager.CreateBufferManager(32 * 1024, 256 * 1024), 256 * 1024);

            // Print out properties for the service client.
            PrintServiceClientProperties(blobClient);

            // Configure storage analytics (metrics and logging) on Blob storage.
            await ConfigureBlobAnalyticsAsync(blobClient);

            // Get geo-replication stats for Blob storage.
            await GetServiceStatsForSecondaryAsync(blobClient);

            // List all containers in the storage account.
            ListAllContainers(blobClient, "sample-");

            // List containers beginning with the specified prefix.
            await ListContainersWithPrefixAsync(blobClient, "sample-");
        }

        /// <summary>
        /// Calls samples that demonstrate how to work with a blob container.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallContainerSamples(CloudBlobContainer container)
        {
            // Fetch container attributes in order to populate the container's properties and metadata.
            await container.FetchAttributesAsync();

            // Read container metadata and properties.
            PrintContainerPropertiesAndMetadata(container);

            // Add container metadata.
            await AddContainerMetadataAsync(container);

            // Fetch the container's attributes again, and read container metadata to see the new additions.
            await container.FetchAttributesAsync();
            PrintContainerPropertiesAndMetadata(container);

            // Make the container available for public access.
            // With Container-level permissions, container and blob data can be read via anonymous request. 
            // Clients can enumerate blobs within the container via anonymous request, but cannot enumerate containers.
            await SetAnonymousAccessLevelAsync(container, BlobContainerPublicAccessType.Container);

            // Try an anonymous operation to read container properties and metadata.
            Uri containerUri = container.Uri;

            // Note that we obtain the container reference using only the URI. No account credentials are used.
            CloudBlobContainer publicContainer = new CloudBlobContainer(containerUri);
            Console.WriteLine("Read container metadata anonymously");
            await container.FetchAttributesAsync();
            PrintContainerPropertiesAndMetadata(container);

            // Make the container private again.
            await SetAnonymousAccessLevelAsync(container, BlobContainerPublicAccessType.Off);

            // Test container lease conditions.
            // This code creates and deletes additional containers.
            await ManageContainerLeasesAsync(container.ServiceClient);
        }

        /// <summary>
        /// Calls samples that demonstrate how to work with blobs.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallBlobSamples(CloudBlobContainer container)
        {
            // Create a blob with a random name.
            CloudBlockBlob blob = await CreateRandomlyNamedBlockBlobAsync(container);

            // Get a reference to the blob created above from the server.
            // This call will fail if the blob does not yet exist.
            await GetExistingBlobReferenceAsync(container, blob.Name);

            // Create a specified number of block blobs in a flat structure.
            await CreateSequentiallyNamedBlockBlobsAsync(container, 5);

            // List blobs in a flat listing
            await ListBlobsFlatListingAsync(container, 10);

            // Create a specified number of block blobs in a hierarchical structure.
            await CreateNestedBlockBlobsAsync(container, 4, 3);

            // List blobs with a hierarchical listing.
            await ListBlobsHierarchicalListingAsync(container, null);

            // List blobs whose names begin with "s" hierarchically, passing the container name as part of the prefix.
            ListBlobsFromServiceClient(container.ServiceClient, string.Format("{0}/s", container.Name));

            // List blobs whose names begin with "0" hierarchically, passing the container name as part of the prefix.
            await ListBlobsFromServiceClientAsync(container.ServiceClient, string.Format("{0}/0", container.Name));

            // Create a snapshot of a block blob.
            await CreateBlockBlobSnapshotAsync(container);

            // Copy a block blob to another blob in the same container.
            await CopyBlockBlobAsync(container);

            // To create and copy a large blob, uncomment this method.
            // By default it creates a 100 MB blob and then copies it; change the value of the sizeInMb parameter 
            // to create a smaller or larger blob.
            // await CopyLargeBlob(container, 100);

            // Upload a blob in blocks.
            await UploadBlobInBlocksAsync(container);

            // Upload a 5 MB array of bytes to a block blob.
            await UploadByteArrayAsync(container, 1024 * 5);
        }

        /// <summary>
        /// Calls shared access signature samples for both containers and blobs.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallSasSamples(CloudBlobContainer container)
        {
            const string BlobName1 = "sasBlob1.txt";
            const string BlobContent1 = "Blob created with an ad-hoc SAS granting write permissions on the container.";

            const string BlobName2 = "sasBlob2.txt";
            const string BlobContent2 = "Blob created with a SAS based on a stored access policy granting write permissions on the container.";

            const string BlobName3 = "sasBlob3.txt";
            const string BlobContent3 = "Blob created with an ad-hoc SAS granting create/write permissions to the blob.";

            const string BlobName4 = "sasBlob4.txt";
            const string BlobContent4 = "Blob created with a SAS based on a stored access policy granting create/write permissions to the blob.";

            string sharedAccessPolicyName = "sample-policy-" + DateTime.Now.Ticks.ToString();

            // Create the container if it does not already exist.
            await container.CreateIfNotExistsAsync();

            // Create a new shared access policy on the container.
            // The access policy may be optionally used to provide constraints for
            // shared access signatures on the container and the blob.
            await CreateSharedAccessPolicyAsync(container, sharedAccessPolicyName);

            // Generate an ad-hoc SAS URI for the container with write and list permissions.
            string adHocContainerSAS = GetContainerSasUri(container);

            // Test the SAS. The write and list operations should succeed, and 
            // the read and delete operations should fail with error code 403 (Forbidden).
            await TestContainerSASAsync(adHocContainerSAS, BlobName1, BlobContent1);

            // Generate a SAS URI for the container, using the stored access policy to set constraints on the SAS.
            string sharedPolicyContainerSAS = GetContainerSasUri(container, sharedAccessPolicyName);

            // Test the SAS. The write, read, list, and delete operations should all succeed.
            await TestContainerSASAsync(sharedPolicyContainerSAS, BlobName2, BlobContent2);

            // Generate an ad-hoc SAS URI for a blob within the container. The ad-hoc SAS has create, write, and read permissions.
            string adHocBlobSAS = GetBlobSasUri(container, BlobName3, null);
            
            // Test the SAS. The create, write, and read operations should succeed, and 
            // the delete operation should fail with error code 403 (Forbidden).
            await TestBlobSASAsync(adHocBlobSAS, BlobContent3);

            // Generate a SAS URI for a blob within the container, using the stored access policy to set constraints on the SAS.
            string sharedPolicyBlobSAS = GetBlobSasUri(container, BlobName4, sharedAccessPolicyName);
            
            // Test the SAS. The create, write, read, and delete operations should all succeed.
            await TestBlobSASAsync(sharedPolicyBlobSAS, BlobContent4);
        }

        #region BlobClientSamples

        /// <summary>
        /// Configures logging and metrics for Blob storage, as well as the default service version.
        /// Note that if you have already enabled analytics for your storage account, running this sample 
        /// will change those settings. For that reason, it's best to run with a test storage account if possible.
        /// The sample saves your settings and resets them after it has completed running.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task ConfigureBlobAnalyticsAsync(CloudBlobClient blobClient)
        {
            try
            {
                // Get current service property settings.
                ServiceProperties serviceProperties = await blobClient.GetServicePropertiesAsync();

                // Enable analytics logging and set retention policy to 14 days. 
                serviceProperties.Logging.LoggingOperations = LoggingOperations.All;
                serviceProperties.Logging.RetentionDays = 14;
                serviceProperties.Logging.Version = "1.0";

                // Configure service properties for hourly and minute metrics. 
                // Set retention policy to 7 days.
                serviceProperties.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
                serviceProperties.HourMetrics.RetentionDays = 7;
                serviceProperties.HourMetrics.Version = "1.0";

                serviceProperties.MinuteMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
                serviceProperties.MinuteMetrics.RetentionDays = 7;
                serviceProperties.MinuteMetrics.Version = "1.0";

                // Set the default service version to be used for anonymous requests.
                serviceProperties.DefaultServiceVersion = "2015-04-05";

                // Set the service properties.
                await blobClient.SetServicePropertiesAsync(serviceProperties);            
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Gets the Blob service stats for the secondary endpoint for an RA-GRS (read-access geo-redundant) storage account.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task GetServiceStatsForSecondaryAsync(CloudBlobClient blobClient)
        {
            try
            {
                // Get the URI for the secondary endpoint for Blob storage.
                Uri secondaryUri = blobClient.StorageUri.SecondaryUri;

                // Create a new service client based on the secondary endpoint.
                CloudBlobClient blobClientSecondary = new CloudBlobClient(secondaryUri, blobClient.Credentials);

                // Get the current stats for the secondary.
                // The call will fail if your storage account does not have RA-GRS enabled.
                ServiceStats blobStats = await blobClientSecondary.GetServiceStatsAsync();

                Console.WriteLine("Geo-replication status: {0}", blobStats.GeoReplication.Status);
                Console.WriteLine("Last geo-replication sync time: {0}", blobStats.GeoReplication.LastSyncTime);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                // In this case, we do not re-throw the exception, so that the sample will continue to run even if RA-GRS is not enabled
                // for this storage account.
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("This storage account does not appear to support RA-GRS.");
                    Console.WriteLine("More information: {0}", e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }
        }

        /// <summary>
        /// Lists all containers in the storage account.
        /// Note that the ListContainers method is called synchronously, for the purposes of the sample. However, in a real-world
        /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <param name="prefix">The container prefix.</param>
        private static void ListAllContainers(CloudBlobClient blobClient, string prefix)
        {
            // List all containers in this storage account.
            Console.WriteLine("List all containers in account:");

            try
            {
                // List containers beginning with the specified prefix, and without returning container metadata.
                foreach (var container in blobClient.ListContainers(prefix, ContainerListingDetails.None, null, null))
                {
                    Console.WriteLine("\tContainer:" + container.Name);
                }

                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Lists containers in the storage account whose names begin with the specified prefix, and return container metadata
        /// as part of the listing operation.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <param name="prefix">The container name prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListContainersWithPrefixAsync(CloudBlobClient blobClient, string prefix)
        {
            Console.WriteLine("List all containers beginning with prefix {0}, plus container metadata:", prefix);

            BlobContinuationToken continuationToken = null;
            ContainerResultSegment resultSegment = null;

            try
            {
                do
                {
                    // List containers beginning with the specified prefix, returning segments of 5 results each. 
                    // Note that passing in null for the maxResults parameter returns the maximum number of results (up to 5000).
                    // Requesting the container's metadata as part of the listing operation populates the metadata, 
                    // so it's not necessary to call FetchAttributes() to read the metadata.
                    resultSegment = await blobClient.ListContainersSegmentedAsync(
                        prefix, ContainerListingDetails.Metadata, 5, continuationToken, null, null);

                    // Enumerate the containers returned.
                    foreach (var container in resultSegment.Results)
                    {
                        Console.WriteLine("\tContainer:" + container.Name);

                        // Write the container's metadata keys and values.
                        foreach (var metadataItem in container.Metadata)
                        {
                            Console.WriteLine("\t\tMetadata key: " + metadataItem.Key);
                            Console.WriteLine("\t\tMetadata value: " + metadataItem.Value);
                        }
                    }

                    // Get the continuation token.
                    continuationToken = resultSegment.ContinuationToken;

                } while (continuationToken != null);

                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Lists blobs beginning with the specified prefix, which must include the container name.
        /// Note that the ListBlobs method is called synchronously, for the purposes of the sample. However, in a real-world
        /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <param name="prefix">The prefix.</param>
        private static void ListBlobsFromServiceClient(CloudBlobClient blobClient, string prefix)
        {
            Console.WriteLine("List blobs by prefix. Prefix must include container name:");

            try
            {
                // The prefix is required when listing blobs from the service client. The prefix must include
                // the container name.
                foreach (var blob in  blobClient.ListBlobs(prefix, true, BlobListingDetails.None, null, null))
                {
                    Console.WriteLine("\tBlob:" + blob.Uri);
                }

                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Lists blobs beginning with the specified prefix, which must include the container name.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <param name="prefix">The prefix.</param>
        private static async Task ListBlobsFromServiceClientAsync(CloudBlobClient blobClient, string prefix)
        {
            Console.WriteLine("List blobs by prefix. Prefix must include container name:");

            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null; 

            try
            {
                do
                {
                    // The prefix is required when listing blobs from the service client. The prefix must include
                    // the container name.
                    resultSegment = await blobClient.ListBlobsSegmentedAsync(prefix, continuationToken);
                    foreach (var blob in resultSegment.Results)
                    {
                        Console.WriteLine("\tBlob:" + blob.Uri);
                    }

                    Console.WriteLine();

                    // Get the continuation token.
                    continuationToken = resultSegment.ContinuationToken;

                } while (continuationToken != null);

            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Prints properties for the Blob service client to the console window.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        private static void PrintServiceClientProperties(CloudBlobClient blobClient)
        {
            Console.WriteLine("-----Blob Service Client Properties-----");
            Console.WriteLine("Storage account name: {0}", blobClient.Credentials.AccountName);
            Console.WriteLine("Authentication Scheme: {0}", blobClient.AuthenticationScheme);
            Console.WriteLine("Base URI: {0}", blobClient.BaseUri);
            Console.WriteLine("Primary URI: {0}", blobClient.StorageUri.PrimaryUri);
            Console.WriteLine("Secondary URI: {0}", blobClient.StorageUri.SecondaryUri);
            Console.WriteLine("Default buffer size: {0}", blobClient.BufferManager.GetDefaultBufferSize());
            Console.WriteLine("Default delimiter: {0}", blobClient.DefaultDelimiter);
            Console.WriteLine();
        }

        #endregion

        #region BlobContainerSamples

        /// <summary>
        /// Creates a sample container for use in the sample application.
        /// </summary>
        /// <param name="blobClient">The blob service client.</param>
        /// <returns>A CloudBlobContainer object.</returns>
        private static async Task<CloudBlobContainer> CreateSampleContainerAsync(CloudBlobClient blobClient)
        {
            // Name sample container based on new GUID, to ensure uniqueness.
            string containerName = ContainerPrefix + Guid.NewGuid();

            // Get a reference to a sample container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            try
            {
                // Create the container if it does not already exist.
                await container.CreateIfNotExistsAsync();
            }
            catch (StorageException e)
            {
                // Ensure that the storage emulator is running if using emulator connection string.
                Console.WriteLine(e.Message);
                Console.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return container;
        }

        /// <summary>
        /// Add some sample metadata to the container.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task AddContainerMetadataAsync(CloudBlobContainer container)
        {
            try
            {
                // Add some metadata to the container.
                container.Metadata.Add("docType", "textDocuments");
                container.Metadata["category"] = "guidance";

                // Set the container's metadata asynchronously.
                await container.SetMetadataAsync();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Sets the anonymous access level.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="accessType">Type of the access.</param>
        /// <returns>A Task object.</returns>
        private static async Task SetAnonymousAccessLevelAsync(CloudBlobContainer container, BlobContainerPublicAccessType accessType)
        {
            try
            {
                // Read the existing permissions first so that we have all container permissions. 
                // This ensures that we do not inadvertently remove any shared access policies while setting the public access level.
                BlobContainerPermissions permissions = await container.GetPermissionsAsync();

                // Set the container's public access level.
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                await container.SetPermissionsAsync(permissions);

                Console.WriteLine("Container public access set to {0}", accessType.ToString());
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }
        
        /// <summary>
        /// Reads the container's properties.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        private static void PrintContainerPropertiesAndMetadata(CloudBlobContainer container)
        {
            Console.WriteLine("-----Container Properties-----");
            Console.WriteLine("Name: {0}", container.Name);
            Console.WriteLine("URI: {0}", container.Uri);
            Console.WriteLine("ETag: {0}", container.Properties.ETag);
            Console.WriteLine("Last modified: {0}", container.Properties.LastModified);
            PrintContainerLeaseProperties(container);

            // Enumerate the container's metadata.
            Console.WriteLine("Container metadata:");
            foreach (var metadataItem in container.Metadata)
            {
                Console.WriteLine("\tKey: {0}", metadataItem.Key);
                Console.WriteLine("\tValue: {0}", metadataItem.Value);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates container lease states: available, breaking, broken, and expired.
        /// A lease is used in each example to delete the container.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task ManageContainerLeasesAsync(CloudBlobClient blobClient)
        {
            CloudBlobContainer container1 = null;
            CloudBlobContainer container2 = null;
            CloudBlobContainer container3 = null;
            CloudBlobContainer container4 = null;
            CloudBlobContainer container5 = null;

            // Lease duration is 15 seconds.
            TimeSpan leaseDuration = new TimeSpan(0, 0, 15);

            const string LeasingPrefix = "leasing-";

            try
            {
                string leaseId = null;
                AccessCondition condition = null;

                /* 
                    Case 1: Lease is available
                */

                // Lease is available on the new container. Acquire the lease and delete the leased container.
                container1 = blobClient.GetContainerReference(LeasingPrefix + Guid.NewGuid());
                await container1.CreateIfNotExistsAsync();

                // Get container properties to see the available lease.
                await container1.FetchAttributesAsync();
                PrintContainerLeaseProperties(container1);

                // Acquire the lease.
                leaseId = await container1.AcquireLeaseAsync(leaseDuration, leaseId);

                // Get container properties again to see that the container is leased.
                await container1.FetchAttributesAsync();
                PrintContainerLeaseProperties(container1);

                // Create an access condition using the lease ID, and use it to delete the leased container..
                condition = new AccessCondition() { LeaseId = leaseId };
                await container1.DeleteAsync(condition, null, null);
                Console.WriteLine("Deleted container {0}", container1.Name);

                /* 
                    Case 2: Lease is breaking
                */

                container2 = blobClient.GetContainerReference(LeasingPrefix + Guid.NewGuid());
                await container2.CreateIfNotExistsAsync();

                // Acquire the lease.
                leaseId = await container2.AcquireLeaseAsync(leaseDuration, null);
                
                // Break the lease. Passing null indicates that the break interval will be the remainder of the current lease.
                await container2.BreakLeaseAsync(null);

                // Get container properties to see the breaking lease.
                // The lease break interval has not yet elapsed.
                await container2.FetchAttributesAsync();
                PrintContainerLeaseProperties(container2);

                // Delete the container. If the lease is breaking, the container can be deleted by
                // passing the lease ID. 
                condition = new AccessCondition() { LeaseId = leaseId };
                await container2.DeleteAsync(condition, null, null);
                Console.WriteLine("Deleted container {0}", container2.Name);

                /* 
                    Case 3: Lease is broken
                */

                container3 = blobClient.GetContainerReference(LeasingPrefix + Guid.NewGuid());
                await container3.CreateIfNotExistsAsync();

                // Acquire the lease.
                leaseId = await container3.AcquireLeaseAsync(leaseDuration, null);
                
                // Break the lease. Passing 0 breaks the lease immediately.
                TimeSpan breakInterval = await container3.BreakLeaseAsync(new TimeSpan(0));

                // Get container properties to see that the lease is broken.
                await container3.FetchAttributesAsync();
                PrintContainerLeaseProperties(container3);

                // Once the lease is broken, delete the container without the lease ID.
                await container3.DeleteAsync();
                Console.WriteLine("Deleted container {0}", container3.Name);

                /* 
                    Case 4: Lease has expired.
                */

                container4 = blobClient.GetContainerReference(LeasingPrefix + Guid.NewGuid());
                await container4.CreateIfNotExistsAsync();
                
                // Acquire the lease.
                leaseId = await container4.AcquireLeaseAsync(leaseDuration, null);
                
                // Sleep for 16 seconds to allow lease to expire.
                Console.WriteLine("Waiting 16 seconds for lease break interval to expire....");
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 16));

                // Get container properties to see that the lease has expired.
                await container4.FetchAttributesAsync();
                PrintContainerLeaseProperties(container4);

                // Delete the container without the lease ID.
                await container4.DeleteAsync();

                /* 
                    Case 5: Attempt to delete leased container without lease ID.
                */

                container5 = blobClient.GetContainerReference(LeasingPrefix + Guid.NewGuid());
                await container5.CreateIfNotExistsAsync();
                
                // Acquire the lease.
                await container5.AcquireLeaseAsync(leaseDuration, null);

                // Get container properties to see that the container has been leased.
                await container5.FetchAttributesAsync();
                PrintContainerLeaseProperties(container5);

                // Attempt to delete the leased container without the lease ID.
                // This operation will result in an error.
                // Note that in a real-world scenario, it would most likely be another client attempting to delete the container.
                await container5.DeleteAsync();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 412)
                {
                    // Handle the error demonstrated for case 5 above and continue execution.
                    Console.WriteLine("The container is leased and cannot be deleted without specifying the lease ID.");
                    Console.WriteLine("More information: {0}", e.Message);
                }
                else
                {
                    // Output error information for any other errors, but continue execution.
                    Console.WriteLine(e.Message);
                }
            }
            finally
            {
                // Enumerate containers based on the prefix used to name them, and delete any remaining containers.
                foreach (var container in blobClient.ListContainers(LeasingPrefix))
                {
                    await container.FetchAttributesAsync();
                    if (container.Properties.LeaseState == LeaseState.Leased || container.Properties.LeaseState == LeaseState.Breaking)
                    {
                        await container.BreakLeaseAsync(new TimeSpan(0));
                    }

                    Console.WriteLine();
                    Console.WriteLine("Deleting container: {0}", container.Name);
                    await container.DeleteAsync();
                }
            }
        }

        /// <summary>
        /// Reads the lease properties for the container.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        private static void PrintContainerLeaseProperties(CloudBlobContainer container)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Leasing properties for container: {0}", container.Name);
                Console.WriteLine("\t Lease state: {0}", container.Properties.LeaseState);
                Console.WriteLine("\t Lease duration: {0}", container.Properties.LeaseDuration);
                Console.WriteLine("\t Lease status: {0}", container.Properties.LeaseStatus);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Deletes containers starting with specified prefix.
        /// Note that the ListContainers method is called synchronously, for the purposes of the sample. However, in a real-world
        /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
        /// </summary>
        /// <param name="blobClient">The Blob service client.</param>
        /// <param name="prefix">The container name prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task DeleteContainersWithPrefixAsync(CloudBlobClient blobClient, string prefix)
        {
            Console.WriteLine("Delete all containers beginning with the specified prefix");
            try
            {
                foreach (var container in blobClient.ListContainers(prefix))
                {
                    Console.WriteLine("\tContainer:" + container.Name);
                    if (container.Properties.LeaseState == LeaseState.Leased)
                    {
                        await container.BreakLeaseAsync(null);
                    }

                    await container.DeleteAsync();
                }

                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Creates a shared access policy on the container.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="policyName">The name of the stored access policy.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateSharedAccessPolicyAsync(CloudBlobContainer container, string policyName)
        {
            // Create a new shared access policy and define its constraints.
            // The access policy provides create, write, read, list, and delete permissions.
            SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List |
                    SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Delete
            };

            // Get the container's existing permissions.
            BlobContainerPermissions permissions = await container.GetPermissionsAsync();

            // Add the new policy to the container's permissions, and set the container's permissions.
            permissions.SharedAccessPolicies.Add(policyName, sharedPolicy);
            await container.SetPermissionsAsync(permissions);
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob container.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="storedPolicyName">A string containing the name of the stored access policy. If null, an ad-hoc SAS is created.</param>
        /// <returns>A string containing the URI for the container, with the SAS token appended.</returns>
        private static string GetContainerSasUri(CloudBlobContainer container, string storedPolicyName = null)
        {
            string sasContainerToken;

            // If no stored policy is specified, create a new access policy and define its constraints.
            if (storedPolicyName == null)
            {
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
                // to construct a shared access policy that is saved to the container's shared access policies. 
                SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
                {
                    // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                    // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
                };

                // Generate the shared access signature on the container, setting the constraints directly on the signature.
                sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);

                Console.WriteLine("SAS for blob container (ad hoc): {0}", sasContainerToken);
                Console.WriteLine();
            }
            else
            {
                // Generate the shared access signature on the container. In this case, all of the constraints for the
                // shared access signature are specified on the stored access policy, which is provided by name.
                // It is also possible to specify some constraints on an ad-hoc SAS and others on the stored access policy.
                sasContainerToken = container.GetSharedAccessSignature(null, storedPolicyName);

                Console.WriteLine("SAS for blob container (stored access policy): {0}", sasContainerToken);
                Console.WriteLine();
            }

            // Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }

        /// <summary>
        /// Tests a container SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        /// <returns>A Task object.</returns>
        private static async Task TestContainerSASAsync(string sasUri, string blobName, string blobContent)
        {
            // Try performing container operations with the SAS provided.
            // Note that the storage account credentials are not required here; the SAS provides the necessary
            // authentication information on the URI.

            // Return a reference to the container using the SAS URI.
            CloudBlobContainer container = new CloudBlobContainer(new Uri(sasUri));

            // Return a reference to a blob to be created in the container.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            // Write operation: Upload a new blob to the container.
            try
            {
                MemoryStream msWrite = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    await blob.UploadFromStreamAsync(msWrite);
                }

                Console.WriteLine("Write operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Write operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            // List operation: List the blobs in the container.
            try
            {
                foreach (ICloudBlob blobItem in container.ListBlobs(
                                                                    prefix: null, 
                                                                    useFlatBlobListing: true, 
                                                                    blobListingDetails: BlobListingDetails.None, 
                                                                    options: null, 
                                                                    operationContext: null))
                {
                    Console.WriteLine(blobItem.Uri);
                }

                Console.WriteLine("List operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("List operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            // Read operation: Read the contents of the blob we created above.
            try
            {
                MemoryStream msRead = new MemoryStream();
                msRead.Position = 0;
                using (msRead)
                {
                    await blob.DownloadToStreamAsync(msRead);
                    Console.WriteLine(msRead.Length);
                }

                Console.WriteLine();
                Console.WriteLine("Read operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Read operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            Console.WriteLine();

            // Delete operation: Delete the blob we created above.
            try
            {
                await blob.DeleteAsync();
                Console.WriteLine("Delete operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Delete operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            Console.WriteLine();
        }

        #endregion

        #region AllBlobTypeSamples

        /// <summary>
        /// Reads the blob's properties.
        /// </summary>
        /// <param name="blob">A CloudBlob object. All blob types (block blob, append blob, and page blob) are derived from CloudBlob.</param>
        private static void PrintBlobPropertiesAndMetadata(CloudBlob blob)
        {
            // Write out properties that are common to all blob types.
            Console.WriteLine();
            Console.WriteLine("-----Blob Properties-----");
            Console.WriteLine("\t Name: {0}", blob.Name);
            Console.WriteLine("\t Container: {0}", blob.Container.Name);
            Console.WriteLine("\t BlobType: {0}", blob.Properties.BlobType);
            Console.WriteLine("\t IsSnapshot: {0}", blob.IsSnapshot);

            // If the blob is a snapshot, write out snapshot properties.
            if (blob.IsSnapshot)
            {
                Console.WriteLine("\t SnapshotTime: {0}", blob.SnapshotTime);
                Console.WriteLine("\t SnapshotQualifiedUri: {0}", blob.SnapshotQualifiedUri);
            }

            Console.WriteLine("\t LeaseState: {0}", blob.Properties.LeaseState);

            // If the blob has been leased, write out lease properties.
            if (blob.Properties.LeaseState != LeaseState.Available)
            {
                Console.WriteLine("\t LeaseDuration: {0}", blob.Properties.LeaseDuration);
                Console.WriteLine("\t LeaseStatus: {0}", blob.Properties.LeaseStatus);
            }

            Console.WriteLine("\t CacheControl: {0}", blob.Properties.CacheControl);
            Console.WriteLine("\t ContentDisposition: {0}", blob.Properties.ContentDisposition);
            Console.WriteLine("\t ContentEncoding: {0}", blob.Properties.ContentEncoding);
            Console.WriteLine("\t ContentLanguage: {0}", blob.Properties.ContentLanguage);
            Console.WriteLine("\t ContentMD5: {0}", blob.Properties.ContentMD5);
            Console.WriteLine("\t ContentType: {0}", blob.Properties.ContentType);
            Console.WriteLine("\t ETag: {0}", blob.Properties.ETag);
            Console.WriteLine("\t LastModified: {0}", blob.Properties.LastModified);
            Console.WriteLine("\t Length: {0}", blob.Properties.Length);

            // Write out properties specific to blob type.
            switch (blob.BlobType)
            {
                case BlobType.AppendBlob:
                    CloudAppendBlob appendBlob = blob as CloudAppendBlob;
                    Console.WriteLine("\t AppendBlobCommittedBlockCount: {0}", appendBlob.Properties.AppendBlobCommittedBlockCount);
                    Console.WriteLine("\t StreamWriteSizeInBytes: {0}", appendBlob.StreamWriteSizeInBytes);
                    break;
                case BlobType.BlockBlob:
                    CloudBlockBlob blockBlob = blob as CloudBlockBlob;
                    Console.WriteLine("\t StreamWriteSizeInBytes: {0}", blockBlob.StreamWriteSizeInBytes);
                    break;
                case BlobType.PageBlob:
                    CloudPageBlob pageBlob = blob as CloudPageBlob;
                    Console.WriteLine("\t PageBlobSequenceNumber: {0}", pageBlob.Properties.PageBlobSequenceNumber);
                    Console.WriteLine("\t StreamWriteSizeInBytes: {0}", pageBlob.StreamWriteSizeInBytes);
                    break;
                default:
                    break;
            }

            Console.WriteLine("\t StreamMinimumReadSizeInBytes: {0}", blob.StreamMinimumReadSizeInBytes);
            Console.WriteLine();

            // Enumerate the blob's metadata.
            Console.WriteLine("Blob metadata:");
            foreach (var metadataItem in blob.Metadata)
            {
                Console.WriteLine("\tKey: {0}", metadataItem.Key);
                Console.WriteLine("\tValue: {0}", metadataItem.Value);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Reads the virtual directory's properties.
        /// </summary>
        /// <param name="dir">A CloudBlobDirectory object.</param>
        private static void PrintVirtualDirectoryProperties(CloudBlobDirectory dir)
        {
            Console.WriteLine();
            Console.WriteLine("-----Virtual Directory Properties-----");
            Console.WriteLine("\t Container: {0}", dir.Container.Name);
            Console.WriteLine("\t Parent: {0}", dir.Parent);
            Console.WriteLine("\t Prefix: {0}", dir.Prefix);
            Console.WriteLine("\t Uri: {0}", dir.Uri);
            Console.WriteLine();
        }

        /// <summary>
        /// Lists blobs in the specified container using a flat listing, with an optional segment size specified, and writes 
        /// their properties and metadata to the console window.
        /// The flat listing returns a segment containing all of the blobs matching the listing criteria.
        /// In a flat listing, blobs are not organized by virtual directory.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="segmentSize">The size of the segment to return in each call to the listing operation.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListBlobsFlatListingAsync(CloudBlobContainer container, int? segmentSize)
        {
            // List blobs to the console window.
            Console.WriteLine("List blobs in segments (flat listing):");
            Console.WriteLine();

            int i = 0;
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            try
            {
                // Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
                // When the continuation token is null, the last segment has been returned and execution can exit the loop.
                do
                {
                    // This overload allows control of the segment size. You can return all remaining results by passing null for the maxResults parameter, 
                    // or by calling a different overload.
                    // Note that requesting the blob's metadata as part of the listing operation 
                    // populates the metadata, so it's not necessary to call FetchAttributes() to read the metadata.
                    resultSegment = await container.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.Metadata, segmentSize, continuationToken, null, null);
                    if (resultSegment.Results.Count() > 0)
                    {
                        Console.WriteLine("Page {0}:", ++i);
                    }

                    foreach (var blobItem in resultSegment.Results)
                    {
                        Console.WriteLine("************************************");
                        Console.WriteLine(blobItem.Uri);

                        // A flat listing operation returns only blobs, not virtual directories.
                        // Write out blob properties and metadata.
                        if (blobItem is CloudBlob)
                        {
                            PrintBlobPropertiesAndMetadata((CloudBlob)blobItem);
                        }
                    }

                    Console.WriteLine();

                    // Get the continuation token.
                    continuationToken = resultSegment.ContinuationToken;

                } while (continuationToken != null);
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Lists blobs in the specified container using a hierarchical listing, and calls this method recursively to return the contents of each 
        /// virtual directory. Reads the properties on each blob or virtual directory returned and writes them to the console window.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="prefix">The blob prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListBlobsHierarchicalListingAsync(CloudBlobContainer container, string prefix)
        {
            // List blobs in segments.
            Console.WriteLine("List blobs (hierarchical listing):");
            Console.WriteLine();

            // Enumerate the result segment returned.
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;

            try
            {
                // Call ListBlobsSegmentedAsync recursively and enumerate the result segment returned, while the continuation token is non-null.
                // When the continuation token is null, the last segment has been returned and execution can exit the loop.
                // Note that blob snapshots cannot be listed in a hierarchical listing operation.
                do
                {
                    resultSegment = await container.ListBlobsSegmentedAsync(prefix, false, BlobListingDetails.Metadata, null, null, null, null);

                    foreach (var blobItem in resultSegment.Results)
                    {
                        Console.WriteLine("************************************");
                        Console.WriteLine(blobItem.Uri);

                        // A hierarchical listing returns both virtual directories and blobs.
                        // Call recursively with the virtual directory prefix to enumerate the contents of each virtual directory.
                        if (blobItem is CloudBlobDirectory)
                        {
                            PrintVirtualDirectoryProperties((CloudBlobDirectory)blobItem);
                            CloudBlobDirectory dir = blobItem as CloudBlobDirectory;
                            await ListBlobsHierarchicalListingAsync(container, dir.Prefix);
                        }
                        else
                        {
                            // Write out blob properties and metadata.
                            PrintBlobPropertiesAndMetadata((CloudBlob)blobItem);
                        }
                    }

                    Console.WriteLine();

                    // Get the continuation token, if there are additional segments of results.
                    continuationToken = resultSegment.ContinuationToken;

                } while (continuationToken != null);
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        #endregion

        #region BlockBlobSamples

        /// <summary>
        /// Creates a randomly named block blob.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>A Task object.</returns>
        private static async Task<CloudBlockBlob> CreateRandomlyNamedBlockBlobAsync(CloudBlobContainer container)
        {
            // Get a reference to a blob that does not yet exist.
            // The GetBlockBlobReference method does not make a request to the service, but only creates the object in memory.
            string blobName = BlobPrefix + Guid.NewGuid();
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            // For the purposes of the sample, check to see whether the blob exists.
            Console.WriteLine("Blob {0} exists? {1}", blobName, await blob.ExistsAsync());

            try
            {
                // Writing to the blob creates it on the service.
                await blob.UploadTextAsync(string.Format("This is a blob named {0}", blobName));
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Check again to see whether the blob exists.
            Console.WriteLine("Blob {0} exists? {1}", blobName, await blob.ExistsAsync());

            return blob;
        }

        /// <summary>
        /// Gets a reference to a blob by making a request to the service.
        /// Note that the GetBlobReferenceFromServer method is called synchronously, for the purposes of the sample. However, in a real-world
        /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="blobName">The blob name.</param>
        /// <returns>A Task object.</returns>
        private static void GetExistingBlobReference(CloudBlobContainer container, string blobName)
        {
            try
            {
                // Get a reference to a blob with a request to the server.
                // If the blob does not exist, this call will fail with a 404 (Not Found).
                ICloudBlob blob = container.GetBlobReferenceFromServer(blobName);

                // The previous call gets the blob's properties, so it's not necessary to call FetchAttributes
                // to read a property.
                Console.WriteLine("Blob {0} was last modified at {1} local time.", blobName,
                    blob.Properties.LastModified.Value.LocalDateTime);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    Console.WriteLine("Blob {0} does not exist.", blobName);
                    Console.WriteLine("Additional error information: " + e.Message);
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Gets a reference to a blob by making a request to the service.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="blobName">The blob name.</param>
        /// <returns>A Task object.</returns>
        private static async Task GetExistingBlobReferenceAsync(CloudBlobContainer container, string blobName)
        {
            try
            {
                // Get a reference to a blob with a request to the server.
                // If the blob does not exist, this call will fail with a 404 (Not Found).
                ICloudBlob blob = await container.GetBlobReferenceFromServerAsync(blobName);

                // The previous call gets the blob's properties, so it's not necessary to call FetchAttributes
                // to read a property.
                Console.WriteLine("Blob {0} was last modified at {1} local time.", blobName,
                    blob.Properties.LastModified.Value.LocalDateTime);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    Console.WriteLine("Blob {0} does not exist.", blobName);
                    Console.WriteLine("Additional error information: " + e.Message);
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Creates the specified number of sequentially named block blobs, in a flat structure.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="numberOfBlobs">The number of blobs to create.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateSequentiallyNamedBlockBlobsAsync(CloudBlobContainer container, int numberOfBlobs)
        {
            try
            {
                CloudBlockBlob blob;
                string blobName = string.Empty;
                MemoryStream msWrite;

                for (int i = 1; i <= numberOfBlobs; i++)
                {
                    // Format string for blob name.
                    blobName = i.ToString("00000") + ".txt";

                    // Get a reference to the blob.
                    blob = container.GetBlockBlobReference(blobName);

                    // Set a property on the blob.
                    blob.Properties.ContentType = "text/html";

                    // Set some metadata on the blob.
                    blob.Metadata.Add("DateCreated", DateTime.UtcNow.ToLongDateString());
                    blob.Metadata.Add("TimeCreated", DateTime.UtcNow.ToLongTimeString());

                    // Write the name of the blob to its contents as well.
                    msWrite = new MemoryStream(Encoding.UTF8.GetBytes("This is blob " + blobName + "."));
                    msWrite.Position = 0;
                    using (msWrite)
                    {
                        // Uploading the blob sets the properties and metadata on the new blob.
                        await blob.UploadFromStreamAsync(msWrite);
                    }
                }
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Creates the specified number of nested block blobs at a specified number of levels.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="numberOfLevels">The number of levels of blobs to create.</param>
        /// <param name="numberOfBlobsPerLevel">The number of blobs to create per level.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateNestedBlockBlobsAsync(CloudBlobContainer container, short numberOfLevels, short numberOfBlobsPerLevel)
        {
            try
            {
                CloudBlockBlob blob;
                MemoryStream msWrite;
                string blobName = string.Empty;
                string virtualDirName = string.Empty;

                // Create a nested blob structure.
                for (int i = 1; i <= numberOfLevels; i++)
                {
                    // Construct the virtual directory name, which becomes part of the blob name.
                    virtualDirName += string.Format("level{0}{1}", i, container.ServiceClient.DefaultDelimiter);
                    for (int j = 1; j <= numberOfBlobsPerLevel; j++)
                    {
                        // Construct string for blob name.
                        blobName = virtualDirName + string.Format("{0}-{1}.txt", i, j.ToString("00000"));

                        // Get a reference to the blob.
                        blob = container.GetBlockBlobReference(blobName);

                        // Write the blob URI to its contents.
                        msWrite = new MemoryStream(Encoding.UTF8.GetBytes("Absolute URI to blob: " + blob.StorageUri.PrimaryUri + "."));
                        msWrite.Position = 0;
                        using (msWrite)
                        {
                            await blob.UploadFromStreamAsync(msWrite);
                        }

                        // Set a property on the blob.
                        blob.Properties.ContentType = "text/html";
                        await blob.SetPropertiesAsync();

                        // Set some metadata on the blob.
                        blob.Metadata.Add("DateCreated", DateTime.UtcNow.ToLongDateString());
                        blob.Metadata.Add("TimeCreated", DateTime.UtcNow.ToLongTimeString());
                        await blob.SetMetadataAsync();
                    }
                }
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Creates a new blob and takes a snapshot of the blob.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateBlockBlobSnapshotAsync(CloudBlobContainer container)
        {
            // Create a new block blob in the container.
            CloudBlockBlob baseBlob = container.GetBlockBlobReference("sample-base-blob.txt");

            // Add blob metadata.
            baseBlob.Metadata.Add("ApproxBlobCreatedDate", DateTime.UtcNow.ToString());

            try
            {
                // Upload the blob to create it, with its metadata.
                await baseBlob.UploadTextAsync(string.Format("Base blob: {0}", baseBlob.Uri.ToString()));

                // Sleep 5 seconds.
                System.Threading.Thread.Sleep(5000);

                // Create a snapshot of the base blob.
                // Specify metadata at the time that the snapshot is created to specify unique metadata for the snapshot.
                // If no metadata is specified when the snapshot is created, the base blob's metadata is copied to the snapshot.
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("ApproxSnapshotCreatedDate", DateTime.UtcNow.ToString());
                await baseBlob.CreateSnapshotAsync(metadata, null, null, null);
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Gets a reference to a blob created previously, and copies it to a new blob in the same container.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CopyBlockBlobAsync(CloudBlobContainer container)
        {
            CloudBlockBlob sourceBlob = null;
            CloudBlockBlob destBlob = null;
            string leaseId = null;

            try
            {
                // Get a block blob from the container to use as the source.
                sourceBlob = container.ListBlobs().OfType<CloudBlockBlob>().FirstOrDefault();

                // Lease the source blob for the copy operation to prevent another client from modifying it.
                // Specifying null for the lease interval creates an infinite lease.
                leaseId = await sourceBlob.AcquireLeaseAsync(null);

                // Get a reference to a destination blob (in this case, a new blob).
                destBlob = container.GetBlockBlobReference("copy of " + sourceBlob.Name);

                // Ensure that the source blob exists.
                if (await sourceBlob.ExistsAsync())
                {
                    // Get the ID of the copy operation.
                    string copyId = await destBlob.StartCopyAsync(sourceBlob);

                    // Fetch the destination blob's properties before checking the copy state.
                    await destBlob.FetchAttributesAsync();

                    Console.WriteLine("Status of copy operation: {0}", destBlob.CopyState.Status);
                    Console.WriteLine("Completion time: {0}", destBlob.CopyState.CompletionTime);
                    Console.WriteLine("Bytes copied: {0}", destBlob.CopyState.BytesCopied.ToString());
                    Console.WriteLine("Total bytes: {0}", destBlob.CopyState.TotalBytes.ToString());
                    Console.WriteLine();
                }
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            finally
            {
                // Break the lease on the source blob.
                if (sourceBlob != null)
                {
                    await sourceBlob.FetchAttributesAsync();

                    if (sourceBlob.Properties.LeaseState != LeaseState.Available)
                    {
                        await sourceBlob.BreakLeaseAsync(new TimeSpan(0));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a large block blob, and copies it to a new blob in the same container. If the copy operation
        /// does not complete within the specified interval, abort the copy operation.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <param name="sizeInMb">The size of block blob to create, in MB.</param>
        /// <returns>A Task object.</returns>
        private static async Task CopyLargeBlockBlobAsync(CloudBlobContainer container, int sizeInMb)
        {
            // Create an array of random bytes, of the specified size.
            byte[] bytes = new byte[sizeInMb * 1024 * 1024];
            Random rng = new Random();
            rng.NextBytes(bytes);

            // Get a reference to a new block blob.
            CloudBlockBlob sourceBlob = container.GetBlockBlobReference("LargeSourceBlob");

            // Get a reference to the destination blob (in this case, a new blob).
            CloudBlockBlob destBlob = container.GetBlockBlobReference("copy of " + sourceBlob.Name);

            MemoryStream msWrite = null;
            string copyId = null;
            string leaseId = null;

            try
            {
                // Create a new block blob comprised of random bytes to use as the source of the copy operation.
                msWrite = new MemoryStream(bytes);
                msWrite.Position = 0;
                using (msWrite)
                {
                    await sourceBlob.UploadFromStreamAsync(msWrite);
                }

                // Lease the source blob for the copy operation to prevent another client from modifying it.
                // Specifying null for the lease interval creates an infinite lease.
                leaseId = await sourceBlob.AcquireLeaseAsync(null);

                // Get the ID of the copy operation.
                copyId = await destBlob.StartCopyAsync(sourceBlob);

                // Fetch the destination blob's properties before checking the copy state.
                await destBlob.FetchAttributesAsync();

                // Sleep for 1 second. In a real-world application, this would most likely be a longer interval.
                System.Threading.Thread.Sleep(1000);

                // Check the copy status. If it is still pending, abort the copy operation.
                if (destBlob.CopyState.Status == CopyStatus.Pending)
                {
                    await destBlob.AbortCopyAsync(copyId);
                    Console.WriteLine("Copy operation {0} has been aborted.", copyId);
                }

                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            finally
            {
                // Close the stream.
                if (msWrite != null)
                {
                    msWrite.Close();
                }

                // Break the lease on the source blob.
                if (sourceBlob != null)
                {
                    await sourceBlob.FetchAttributesAsync();

                    if (sourceBlob.Properties.LeaseState != LeaseState.Available)
                    {
                        await sourceBlob.BreakLeaseAsync(new TimeSpan(0));
                    }
                }

                // Delete the source blob.
                if (sourceBlob != null)
                {
                    await sourceBlob.DeleteIfExistsAsync();
                }

                // Delete the destination blob.
                if (destBlob != null)
                {
                    await destBlob.DeleteIfExistsAsync();
                }
            }
        }

        /// <summary>
        /// Uploads the blob as a set of 256 KB blocks.
        /// </summary>
        /// <param name="container">A CloudBlobContainer object.</param>
        /// <returns>A Task object.</returns>
        private static async Task UploadBlobInBlocksAsync(CloudBlobContainer container)
        {
            // Create an array of random bytes, of the specified size.
            byte[] randomBytes = new byte[5 * 1024 * 1024];
            Random rnd = new Random();
            rnd.NextBytes(randomBytes);

            // Get a reference to a new block blob.
            CloudBlockBlob blob = container.GetBlockBlobReference("sample-blob-" + Guid.NewGuid());

            // Specify the block size as 256 KB.
            int blockSize = 256 * 1024;

            MemoryStream msWrite = null;

            // Create new stream of bytes.
            msWrite = new MemoryStream(randomBytes);
            msWrite.Position = 0;
            using (msWrite)
            {
                long streamSize = msWrite.Length;

                // Create a list of block IDs.
                List<string> blockIDs = new List<string>();

                // Indicate the starting block number.
                int blockNumber = 1;

                try
                {
                    // The number of bytes read so far.
                    int bytesRead = 0;

                    // The number of bytes left to read and upload.
                    long bytesLeft = streamSize;

                    // Loop until all of the bytes in the stream have been uploaded.
                    while (bytesLeft > 0)
                    {
                        int bytesToRead;

                        // Check whether the remaining bytes constitute at least another block.
                        if (bytesLeft >= blockSize)
                        {
                            // Read another whole block.
                            bytesToRead = blockSize;
                        }
                        else
                        {
                            // There's less than a whole block left, so read the rest of it.
                            bytesToRead = (int)bytesLeft;
                        }

                        // Create a block ID from the block number, and add it to the block ID list.
                        // Note that the block ID is a base64 string.
                        string blockId = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("BlockId{0}", blockNumber.ToString("0000000"))));
                        blockIDs.Add(blockId);

                        // Set up a new buffer for writing the block, and read that many bytes into it .
                        byte[] bytesToWrite = new byte[bytesToRead];
                        msWrite.Read(bytesToWrite, 0, bytesToRead);

                        // Calculate the MD5 hash of the buffer.
                        MD5 md5 = new MD5CryptoServiceProvider();
                        byte[] blockHash = md5.ComputeHash(bytesToWrite);
                        string md5Hash = Convert.ToBase64String(blockHash, 0, 16);

                        // Upload the block with the hash.
                        await blob.PutBlockAsync(blockId, new MemoryStream(bytesToWrite), md5Hash);

                        // Increment and decrement the counters.
                        bytesRead += bytesToRead;
                        bytesLeft -= bytesToRead;
                        blockNumber++;
                    }

                    // Read the block list that we just created. The blocks will all be uncommitted at this point.
                    await ReadBlockListAsync(blob);

                    // Commit the blocks to a new blob.
                    await blob.PutBlockListAsync(blockIDs);

                    // Read the block list again. Now all blocks will be committed.
                    await ReadBlockListAsync(blob);
                }
                catch (StorageException e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }
        }

        /// <summary>
        /// Reads the blob's block list, and indicates whether the blob has been committed.
        /// </summary>
        /// <param name="blob">A CloudBlockBlob object.</param>
        /// <returns>A Task object.</returns>
        private static async Task ReadBlockListAsync(CloudBlockBlob blob)
        {
            // Get the blob's block list.
            foreach (var listBlockItem in await blob.DownloadBlockListAsync(BlockListingFilter.All, null, null, null))
            {
                if (listBlockItem.Committed)
                {
                    Console.WriteLine(
                                      "Block {0} has been committed to block list. Block length = {1}",
                                      listBlockItem.Name, 
                                      listBlockItem.Length);
                }
                else
                {
                    Console.WriteLine(
                                      "Block {0} is uncommitted. Block length = {1}",
                                      listBlockItem.Name, 
                                      listBlockItem.Length);
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="policyName">A string containing the name of the stored access policy. If null, an ad-hoc SAS is created.</param>
        /// <returns>A string containing the URI for the blob, with the SAS token appended.</returns>
        private static string GetBlobSasUri(CloudBlobContainer container, string blobName, string policyName = null)
        {
            string sasBlobToken;

            // Get a reference to a blob within the container.
            // Note that the blob may not exist yet, but a SAS can still be created for it.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            if (policyName == null)
            {
                // Create a new access policy and define its constraints.
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
                // to construct a shared access policy that is saved to the container's shared access policies. 
                SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
                {
                    // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request. 
                    // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
                };

                // Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);

                Console.WriteLine("SAS for blob (ad hoc): {0}", sasBlobToken);
                Console.WriteLine();
            }
            else
            {
                // Generate the shared access signature on the blob. In this case, all of the constraints for the
                // shared access signature are specified on the container's stored access policy.
                sasBlobToken = blob.GetSharedAccessSignature(null, policyName);

                Console.WriteLine("SAS for blob (stored access policy): {0}", sasBlobToken);
                Console.WriteLine();
            }

            // Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }

        /// <summary>
        /// Tests a blob SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        /// <returns>A Task object.</returns>
        private static async Task TestBlobSASAsync(string sasUri, string blobContent)
        {
            // Try performing blob operations using the SAS provided.

            // Return a reference to the blob using the SAS URI.
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(sasUri));

            // Create operation: Upload a blob with the specified name to the container.
            // If the blob does not exist, it will be created. If it does exist, it will be overwritten.
            try
            {
                MemoryStream msWrite = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    await blob.UploadFromStreamAsync(msWrite);
                }

                Console.WriteLine("Create operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Create operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            // Write operation: Add metadata to the blob
            try
            {
                await blob.FetchAttributesAsync();
                string rnd = new Random().Next().ToString();
                string metadataName = "name";
                string metadataValue = "value";
                blob.Metadata.Add(metadataName, metadataValue);
                await blob.SetMetadataAsync();

                Console.WriteLine("Write operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Write operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            // Read operation: Read the contents of the blob.
            try
            {
                MemoryStream msRead = new MemoryStream();
                using (msRead)
                {
                    await blob.DownloadToStreamAsync(msRead);
                    msRead.Position = 0;
                    using (StreamReader reader = new StreamReader(msRead, true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Console.WriteLine(line);
                        }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine("Read operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Read operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }

            // Delete operation: Delete the blob.
            try
            {
                await blob.DeleteAsync();
                Console.WriteLine("Delete operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 403)
                {
                    Console.WriteLine("Delete operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                    throw;
                }
            }
        }

        /// <summary>
        /// Uploads an array of bytes to a new blob.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="numBytes">The number of bytes to upload.</param>
        /// <returns></returns>
        private static async Task UploadByteArrayAsync(CloudBlobContainer container, long numBytes)
        {
            CloudBlockBlob blob = container.GetBlockBlobReference(BlobPrefix + "byte-array-" + Guid.NewGuid());

            // Write an array of random bytes to a block blob. 
            Console.WriteLine("Write an array of bytes to a block blob");
            byte[] sampleBytes = new byte[numBytes];
            Random random = new Random();
            random.NextBytes(sampleBytes);

            try
            {
                await blob.UploadFromByteArrayAsync(sampleBytes, 0, sampleBytes.Length);
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Query the Cross-Origin Resource Sharing (CORS) rules for the Queue service
        /// </summary>
        /// <param name="blobClient"></param>
        private static async Task CorsSample(CloudBlobClient blobClient)
        {
            // Get CORS rules
            Console.WriteLine("Get CORS rules");

            ServiceProperties serviceProperties = await blobClient.GetServicePropertiesAsync();

            // Add CORS rule
            Console.WriteLine("Add CORS rule");

            CorsRule corsRule = new CorsRule
            {
                AllowedHeaders = new List<string> { "*" },
                AllowedMethods = CorsHttpMethods.Get,
                AllowedOrigins = new List<string> { "*" },
                ExposedHeaders = new List<string> { "*" },
                MaxAgeInSeconds = 3600
            };

            serviceProperties.Cors.CorsRules.Add(corsRule);
            await blobClient.SetServicePropertiesAsync(serviceProperties);
            Console.WriteLine();
        }

        /// <summary>
        /// Get a list of valid page ranges for a page blob
        /// </summary>
        /// <param name="container"></param>
        /// <returns>A Task object.</returns>
        private static async Task PageRangesSample(CloudBlobContainer container)
        {
            BlobRequestOptions requestOptions = new BlobRequestOptions { RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1), 3) };
            await container.CreateIfNotExistsAsync(requestOptions, null);

            Console.WriteLine("Create Page Blob");
            CloudPageBlob pageBlob = container.GetPageBlobReference("blob1");
            pageBlob.Create(4 * 1024);

            Console.WriteLine("Write Pages to Blob");
            byte[] buffer = GetRandomBuffer(1024);
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                pageBlob.WritePages(memoryStream, 512);
            }

            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                pageBlob.WritePages(memoryStream, 3 * 1024);
            }

            Console.WriteLine("Get Page Ranges");
            IEnumerable<PageRange> pageRanges = pageBlob.GetPageRanges();
            foreach (PageRange pageRange in pageRanges)
            {
                Console.WriteLine(pageRange.ToString());
            }

            // Clean up after the demo. This line is not strictly necessary as the container is deleted in the next call.
            // It is included for the purposes of the example. 
            Console.WriteLine("Delete page blob");
            await pageBlob.DeleteIfExistsAsync();
            Console.WriteLine();
        }

        private static byte[] GetRandomBuffer(int size)
        {
            byte[] buffer = new byte[size];
            Random random = new Random();
            random.NextBytes(buffer);
            return buffer;
        }
    }
}
