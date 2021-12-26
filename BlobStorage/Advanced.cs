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
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Azure.Storage.Sas;

    /// <summary>
    /// Advanced samples for Blob storage, including samples demonstrating a variety of client library classes and methods.
    /// </summary>
    public static class Advanced
    {
        // Prefix for containers created by the sample.
        private const string ContainerPrefix = "sample-";

        // Prefix for blob created by the sample.
        private const string BlobPrefix = "sample-blob-";

        private static BlobServiceClient blobServiceClient;

        /// <summary>
        /// Calls the advanced samples for Blob storage.
        /// </summary>
        /// <returns>A Task object.</returns>
        public static async Task CallBlobAdvancedSamples()
        {
            blobServiceClient = new BlobServiceClient(ConfigurationManager.AppSettings.Get("StorageConnectionString"));

            BlobContainerClient container = null;
            BlobServiceProperties userServiceProperties = null;

            try
            {
                // Save the user's current service property/storage analytics settings.
                // This ensures that the sample does not permanently overwrite the user's analytics settings.
                // Note however that logging and metrics settings will be modified for the duration of the sample.
                userServiceProperties = await blobServiceClient.GetPropertiesAsync();

                // Get a reference to a sample container.
                container = await CreateSampleContainerAsync(blobServiceClient);

                // Call Blob service client samples. 
                await CallBlobServiceClientSamplesAsync(blobServiceClient);

                // Call blob container samples using the sample container just created.
                await CallContainerSamplesAsync(container);

                // Call blob samples using the same sample container.
                await CallBlobSamplesAsync(container);

                // Call shared access signature samples (both container and blob).
                await CallSasSamplesAsync(container);

                // CORS Rules
                await CorsSampleAsync(blobServiceClient);

                // Page Blob Ranges
                await PageRangesSampleAsync(container);
            }
            catch (RequestFailedException e)
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
                //await DeleteContainersWithPrefix(blobServiceClient, ContainerPrefix);

                // Return the service properties/storage analytics settings to their original values.
                await blobServiceClient.SetPropertiesAsync(userServiceProperties);
            }
        }

        /// <summary>
        /// Calls samples that demonstrate how to use the Blob service client (BlobServiceClient) object.
        /// </summary>
        /// <param name="blobServiceClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallBlobServiceClientSamplesAsync(BlobServiceClient blobServiceClient)
        {
            // Print out properties for the service client.
            PrintServiceClientProperties(blobServiceClient);

            // Configure storage analytics (metrics and logging) on Blob storage.
            await ConfigureBlobAnalyticsAsync(blobServiceClient);

            // List all containers in the storage account.
            ListAllContainers(blobServiceClient, "sample-");

            // List containers beginning with the specified prefix.
            await ListContainersWithPrefixAsync(blobServiceClient, "sample-");
        }

        /// <summary>
        /// Calls samples that demonstrate how to work with a blob container.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallContainerSamplesAsync(BlobContainerClient container)
        {

            // Read container metadata and properties.
            await PrintContainerPropertiesAndMetadataAsync(container);

            // Add container metadata.
            await AddContainerMetadataAsync(container);

            // read container metadata to see the new additions.
            await PrintContainerPropertiesAndMetadataAsync(container);

            // Make the container available for public access.
            // With Container-level permissions, container and blob data can be read via anonymous request. 
            // Clients can enumerate blobs within the container via anonymous request, but cannot enumerate containers.
            await SetAnonymousAccessLevelAsync(container, PublicAccessType.BlobContainer);

            // Note that we obtain the container reference using only the URI. No account credentials are used.
            Console.WriteLine("Read container metadata anonymously");
            await PrintContainerPropertiesAndMetadataAsync(container);

            // Make the container private again.
            await SetAnonymousAccessLevelAsync(container, PublicAccessType.None);

            // Test container lease conditions.
            // This code creates and deletes additional containers.
            await ManageContainerLeasesAsync(blobServiceClient);
        }

        /// <summary>
        /// Calls samples that demonstrate how to work with blobs.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallBlobSamplesAsync(BlobContainerClient container)
        {
            // Create a blob with a random name.
            BlobClient blob = await CreateRandomlyNamedBlockBlobAsync(container);

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


            // Create a snapshot of a block blob.
            await CreateBlockBlobSnapshotAsync(container);

            // Copy a block blob to another blob in the same container.
            await CopyBlockBlobAsync(container);

            // To create and copy a large blob, uncomment this method.
            // By default it creates a 100 MB blob and then copies it; change the value of the sizeInMb parameter 
            // to create a smaller or larger blob.
            //await CopyLargeBlob(container, 100);

            // Upload a blob in blocks.
            await UploadBlobInBlocksAsync(container);

            // Upload a 5 MB array of bytes to a block blob.
            await UploadByteArrayAsync(container, 1024 * 5);
        }

        /// <summary>
        /// Calls shared access signature samples for both containers and blobs.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CallSasSamplesAsync(BlobContainerClient container)
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

            var storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, ConfigurationManager.AppSettings.Get("StorageAccountKey"));

            // Create the container if it does not already exist.
            await container.CreateIfNotExistsAsync();

            // Generate an  SAS URI for the container with write and list permissions.
            Uri ContainerSAS = container.GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Delete | BlobContainerSasPermissions.List, DateTime.UtcNow.AddHours(1));

            // Test the SAS. The write and list operations should succeed, and 
            // the read and delete operations should fail with error code 403 (Forbidden).
            await TestContainerSASAsync(ContainerSAS, BlobName1, BlobContent1);

            // Generate a SAS URI for the container, using the stored access policy to set constraints on the SAS.
            Uri sharedPolicyContainerSAS = container.GenerateSasUri(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Delete, DateTimeOffset.UtcNow.AddHours(1));
            // Test the SAS. The write, read, list, and delete operations should all succeed.
            await TestContainerSASAsync(sharedPolicyContainerSAS, BlobName2, BlobContent2);

            // Generate an ad-hoc SAS URI for a blob within the container. The ad-hoc SAS has create, write, and read permissions.
            Uri adHocBlobSAS = container.GetBlobClient(BlobName3).GenerateSasUri(BlobSasPermissions.All, DateTimeOffset.UtcNow.AddHours(1));
            // Test the SAS. The create, write, and read operations should succeed, and 
            // the delete operation should fail with error code 403 (Forbidden).
            await TestBlobSASAsync(adHocBlobSAS, BlobContent3);

            // Generate a SAS URI for a blob within the container, using the stored access policy to set constraints on the SAS.
            Uri sharedPolicyBlobSAS = GetBlobSasUri(container, BlobName4, storageSharedKeyCredential, sharedAccessPolicyName);

            // Test the SAS. The create, write, read, and delete operations should all succeed.
            await TestBlobSASAsync(sharedPolicyBlobSAS, BlobContent4);
        }

        #region blobServiceClientSamples

        /// <summary>
        /// Configures logging and metrics for Blob storage, as well as the default service version.
        /// Note that if you have already enabled analytics for your storage account, running this sample 
        /// will change those settings. For that reason, it's best to run with a test storage account if possible.
        /// The sample saves your settings and resets them after it has completed running.
        /// </summary>
        /// <param name="blobServiceClient">The Blob service client.</param>
        /// <returns>A Task object.</returns>
        private static async Task ConfigureBlobAnalyticsAsync(BlobServiceClient blobServiceClient)
        {
            try
            {
                // Get current service property settings.
                BlobServiceProperties serviceProperties = await blobServiceClient.GetPropertiesAsync();

                // Enable analytics logging and set retention policy to 14 days. 
                serviceProperties.Logging.Read = true;
                serviceProperties.Logging.Delete = true;
                serviceProperties.Logging.Write = true;
                var blobRetentionPolicy = new BlobRetentionPolicy();
                blobRetentionPolicy.Days = 14;
                blobRetentionPolicy.Enabled = true;
                serviceProperties.Logging.RetentionPolicy.Enabled = true;
                serviceProperties.Logging.RetentionPolicy.Days = 14;
                serviceProperties.Logging.Version = "1.0";

                // Configure service properties for hourly and minute metrics. 
                // Set retention policy to 7 days.
                serviceProperties.HourMetrics.Enabled = true;
                serviceProperties.HourMetrics.IncludeApis = true;
                serviceProperties.HourMetrics.RetentionPolicy.Enabled = true;
                serviceProperties.HourMetrics.RetentionPolicy.Days = 7;
                serviceProperties.HourMetrics.Version = "1.0";

                serviceProperties.MinuteMetrics.Enabled = true;
                serviceProperties.MinuteMetrics.IncludeApis = true;
                serviceProperties.MinuteMetrics.RetentionPolicy.Enabled = true;
                serviceProperties.MinuteMetrics.RetentionPolicy.Days = 7;
                serviceProperties.MinuteMetrics.Version = "1.0";

                // Set the default service version to be used for anonymous requests.
                serviceProperties.DefaultServiceVersion = "2018-11-09";

                // Set the service properties.
                await blobServiceClient.SetPropertiesAsync(serviceProperties);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Lists all containers in the storage account.
        /// Note that the ListContainers method is called synchronously, for the purposes of the sample. However, in a real-world
        /// application using the async/await pattern, best practices recommend using asynchronous methods consistently.
        /// </summary>
        /// <param name="blobServiceClient">The Blob service client.</param>
        /// <param name="prefix">The container prefix.</param>
        private static void ListAllContainers(BlobServiceClient blobServiceClient, string prefix)
        {
            // List all containers in this storage account.
            Console.WriteLine("List all containers in account:");

            try
            {
                // List containers beginning with the specified prefix, and without returning container metadata.
                foreach (var container in blobServiceClient.GetBlobContainers(BlobContainerTraits.None, prefix: prefix))
                {
                    Console.WriteLine("\tContainer:" + container.Name);
                }

                Console.WriteLine();
            }
            catch (RequestFailedException e)
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
        /// <param name="blobServiceClient">The Blob service client.</param>
        /// <param name="prefix">The container name prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListContainersWithPrefixAsync(BlobServiceClient blobServiceClient, string prefix)
        {
            Console.WriteLine("List all containers beginning with prefix {0}, plus container metadata:", prefix);

            try
            {
                // List containers beginning with the specified prefix, returning segments of 5 results each. 
                // Note that passing in null for the maxResults parameter returns the maximum number of results (up to 5000).
                // Requesting the container's metadata as part of the listing operation populates the metadata, 
                // so it's not necessary to call GetProperties() to read the metadata.
                AsyncPageable<BlobContainerItem> containers = blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, prefix: prefix);

                // Enumerate the containers returned.
                await foreach (var container in containers)
                {
                    Console.WriteLine("\tContainer:" + container.Name);

                    // Write the container's metadata keys and values.
                    foreach (var metadataItem in container.Properties.Metadata)
                    {
                        Console.WriteLine("\t\tMetadata key: " + metadataItem.Key);
                        Console.WriteLine("\t\tMetadata value: " + metadataItem.Value);
                    }
                }
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Prints properties for the Blob service client to the console window.
        /// </summary>
        /// <param name="blobServiceClient">The Blob service client.</param>
        private static void PrintServiceClientProperties(BlobServiceClient blobServiceClient)
        {
            Console.WriteLine("-----Blob Service Client Properties-----");
            Console.WriteLine("Storage account name: {0}", blobServiceClient.AccountName);
            Console.WriteLine("Authentication Scheme: {0}", blobServiceClient.Uri.Scheme);
            Console.WriteLine("Base URI: {0}", blobServiceClient.Uri);
            Console.WriteLine();
        }

        #endregion

        #region BlobContainerSamples

        /// <summary>
        /// Creates a sample container for use in the sample application.
        /// </summary>
        /// <param name="blobServiceClient">The blob service client.</param>
        /// <returns>A BlobContainerClient object.</returns>
        private static async Task<BlobContainerClient> CreateSampleContainerAsync(BlobServiceClient blobServiceClient)
        {
            // Name the sample container based on new GUID, to ensure uniqueness.
            string containerName = ContainerPrefix + Guid.NewGuid();

            // Get a reference to a sample container.
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                // Create the container if it does not already exist.
                await container.CreateIfNotExistsAsync();
            }
            catch (RequestFailedException e)
            {
                // Ensure the Azurite emulator is running if using the Azurite connection string.
                Console.WriteLine(e.Message);
                Console.WriteLine("If you're running with the default connection string, make sure you've started the Azurite emulator. Press the Windows key and type Azure Storage to select and run it from the list of apps. Then restart the sample.");
                Console.ReadLine();
                throw;
            }

            return container;
        }

        /// <summary>
        /// Add some sample metadata to the container.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task AddContainerMetadataAsync(BlobContainerClient container)
        {
            try
            {
                await container.GetPropertiesAsync();
                // Add some metadata to the container.
                IDictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("docType", "textDocuments");
                metadata["category"] = "guidance";

                // Set the container's metadata asynchronously.
                await container.SetMetadataAsync(metadata);
            }
            catch (RequestFailedException e)
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
        private static async Task SetAnonymousAccessLevelAsync(BlobContainerClient container, PublicAccessType accessType)
        {
            try
            {
                // Set the container's public access level.       
                await container.SetAccessPolicyAsync(PublicAccessType.BlobContainer);

                Console.WriteLine("Container public access set to {0}", accessType.ToString());
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Reads the container's properties.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        private static async Task PrintContainerPropertiesAndMetadataAsync(BlobContainerClient container)
        {
            BlobContainerProperties properties = await container.GetPropertiesAsync();
            Console.WriteLine("-----Container Properties-----");
            Console.WriteLine("Name: {0}", container.Name);
            Console.WriteLine("URI: {0}", container.Uri);
            Console.WriteLine("ETag: {0}", properties.ETag);
            Console.WriteLine("Last modified: {0}", properties.LastModified);
            await PrintContainerLeasePropertiesAsync(container);

            // Enumerate the container's metadata.
            Console.WriteLine("Container metadata:");
            foreach (var metadataItem in properties.Metadata)
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
        private static async Task ManageContainerLeasesAsync(BlobServiceClient blobServiceClient)
        {
            BlobContainerClient container1 = null;
            BlobContainerClient container2 = null;
            BlobContainerClient container3 = null;
            BlobContainerClient container4 = null;
            BlobContainerClient container5 = null;

            // Lease duration is 15 seconds.
            TimeSpan leaseDuration = new TimeSpan(0, 0, 15);

            const string LeasingPrefix = "leasing-";

            try
            {
                string leaseId = null;
                BlobRequestConditions condition = null;

                /* 
                    Case 1: Lease is available
                */

                // Lease is available on the new container. Acquire the lease and delete the leased container.
                container1 = blobServiceClient.GetBlobContainerClient(LeasingPrefix + Guid.NewGuid());
                await container1.CreateIfNotExistsAsync();

                // Get container properties to see the available lease.
                await PrintContainerLeasePropertiesAsync(container1);

                // Acquire the lease.
                BlobLeaseClient leaseclient1 = container1.GetBlobLeaseClient();
                BlobLease containerLease1 = await leaseclient1.AcquireAsync(leaseDuration);
                leaseId = containerLease1.LeaseId;
                // Get container properties again to see that the container is leased.
                await PrintContainerLeasePropertiesAsync(container1);

                // Create an access condition using the lease ID, and use it to delete the leased container..
                condition = new BlobRequestConditions() { LeaseId = leaseId };
                await container1.DeleteAsync(condition);
                Console.WriteLine("Deleted container {0}", container1.Name);

                /* 
                    Case 2: Lease is breaking
                */

                container2 = blobServiceClient.GetBlobContainerClient(LeasingPrefix + Guid.NewGuid());
                await container2.CreateIfNotExistsAsync();

                // Acquire the lease.
                BlobLeaseClient leaseClient2 = container2.GetBlobLeaseClient();
                BlobLease containerLease2 = await leaseClient2.AcquireAsync(leaseDuration);
                leaseId = containerLease2.LeaseId;

                // Break the lease. Passing null indicates that the break interval will be the remainder of the current lease.
                await leaseClient2.BreakAsync(null);

                // Get container properties to see the breaking lease.
                // The lease break interval has not yet elapsed.
                await PrintContainerLeasePropertiesAsync(container2);

                // Delete the container. If the lease is breaking, the container can be deleted by
                // passing the lease ID. 
                condition = new BlobRequestConditions() { LeaseId = leaseId };
                await container2.DeleteAsync(condition);
                Console.WriteLine("Deleted container {0}", container2.Name);

                /* 
                    Case 3: Lease is broken
                */

                container3 = blobServiceClient.GetBlobContainerClient(LeasingPrefix + Guid.NewGuid());
                await container3.CreateIfNotExistsAsync();

                // Acquire the lease.
                BlobLeaseClient leaseClient3 = container3.GetBlobLeaseClient();
                BlobLease containerLease3 = await leaseClient3.AcquireAsync(leaseDuration);
                leaseId = containerLease3.LeaseId;

                // Break the lease. Passing 0 breaks the lease immediately.
                Response<BlobLease> result = await leaseClient3.BreakAsync(new TimeSpan(0));

                // Get container properties to see that the lease is broken.
                await PrintContainerLeasePropertiesAsync(container3);

                // Once the lease is broken, delete the container without the lease ID.
                await container3.DeleteAsync();
                Console.WriteLine("Deleted container {0}", container3.Name);

                /* 
                    Case 4: Lease has expired.
                */

                container4 = blobServiceClient.GetBlobContainerClient(LeasingPrefix + Guid.NewGuid());
                await container4.CreateIfNotExistsAsync();

                // Acquire the lease.
                BlobLeaseClient leaseClient4 = container4.GetBlobLeaseClient();
                BlobLease containerLease4 = await leaseClient4.AcquireAsync(leaseDuration);
                leaseId = containerLease4.LeaseId;

                // Sleep for 16 seconds to allow lease to expire.
                Console.WriteLine("Waiting 16 seconds for lease break interval to expire....");
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 16));

                // Get container properties to see that the lease has expired.
                await PrintContainerLeasePropertiesAsync(container4);

                // Delete the container without the lease ID.
                await container4.DeleteAsync();

                /* 
                    Case 5: Attempt to delete leased container without lease ID.
                */

                container5 = blobServiceClient.GetBlobContainerClient(LeasingPrefix + Guid.NewGuid());
                await container5.CreateIfNotExistsAsync();

                // Acquire the lease.
                BlobLeaseClient leaseClient5 = container5.GetBlobLeaseClient();
                BlobLease containerLease5 = await leaseClient5.AcquireAsync(leaseDuration);
                leaseId = containerLease5.LeaseId;

                // Get container properties to see that the container has been leased.
                await PrintContainerLeasePropertiesAsync(container5);

                // Attempt to delete the leased container without the lease ID.
                // This operation will result in an error.
                // Note that in a real-world scenario, it would most likely be another client attempting to delete the container.
                await container5.DeleteAsync();
            }
            catch (RequestFailedException e) when (e.Status == 412 && e.ErrorCode == BlobErrorCode.LeaseIdMissing)
            {
                // Handle the error demonstrated for case 5 above and continue execution.
                Console.WriteLine("The container is leased and cannot be deleted without specifying the lease ID.");
                Console.WriteLine("More information: {0}", e.Message);
            }
            catch (RequestFailedException e)
            {
                // Output error information for any other errors, but continue execution.
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Enumerate containers based on the prefix used to name them, and delete any remaining containers.
                foreach (var container in blobServiceClient.GetBlobContainers(prefix: LeasingPrefix))
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
                    if (container.Properties.LeaseState == LeaseState.Leased || container.Properties.LeaseState == LeaseState.Breaking)
                    {
                        var leaseClient = containerClient.GetBlobLeaseClient();
                        await leaseClient.BreakAsync(new TimeSpan(0));
                    }

                    Console.WriteLine();
                    Console.WriteLine("Deleting container: {0}", container.Name);
                    await containerClient.DeleteAsync();
                }
            }
        }

        /// <summary>
        /// Reads the lease properties for the container.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        private static async Task PrintContainerLeasePropertiesAsync(BlobContainerClient container)
        {
            BlobContainerProperties properties = await container.GetPropertiesAsync();
            try
            {
                Console.WriteLine();
                Console.WriteLine("Leasing properties for container: {0}", container.Name);
                Console.WriteLine("\t Lease state: {0}", properties.LeaseState);
                Console.WriteLine("\t Lease duration: {0}", properties.LeaseDuration);
                Console.WriteLine("\t Lease status: {0}", properties.LeaseStatus);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
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
        /// <param name="blobServiceClient">The Blob service client.</param>
        /// <param name="prefix">The container name prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task DeleteContainersWithPrefixAsync(BlobServiceClient blobServiceClient, string prefix)
        {
            Console.WriteLine("Delete all containers beginning with the specified prefix");
            try
            {
                foreach (var container in blobServiceClient.GetBlobContainers(prefix: prefix))
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
                    Console.WriteLine("\tContainer:" + container.Name);
                    if (container.Properties.LeaseState == LeaseState.Leased)
                    {
                        var leaseClient = containerClient.GetBlobLeaseClient();
                        await leaseClient.BreakAsync(null);
                    }
                    await blobServiceClient.DeleteBlobContainerAsync(container.Name);
                }

                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }


        /// <summary>
        /// Tests a container SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        /// <returns>A Task object.</returns>
        private static async Task TestContainerSASAsync(Uri sasUri, string blobName, string blobContent)
        {
            // Try performing container operations with the SAS provided.
            // Note that the storage account credentials are not required here; the SAS provides the necessary
            // authentication information on the URI.

            // Return a reference to the container using the SAS URI.
            var container = new BlobContainerClient(sasUri);

            // Return a reference to a blob to be created in the container.
            BlobClient blob = container.GetBlobClient(blobName);

            // Write operation: Upload a new blob to the container.
            try
            {
                await blob.UploadAsync(BinaryData.FromString(blobContent));

                Console.WriteLine("Write operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Write operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            // List operation: List the blobs in the container.
            try
            {
                foreach (BlobItem blobItem in container.GetBlobs())
                {
                    Console.WriteLine(blobItem.Name);
                }

                Console.WriteLine("List operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("List operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Read operation: Read the contents of the blob we created above.
            try
            {
                int downloadLength = (await blob.DownloadContentAsync()).Value.Content.ToString().Length;
                Console.WriteLine(downloadLength);

                Console.WriteLine();
                Console.WriteLine("Read operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Read operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            Console.WriteLine();

            // Delete operation: Delete the blob we created above.
            try
            {
                await blob.DeleteAsync();
                Console.WriteLine("Delete operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Delete operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            Console.WriteLine();
        }

        #endregion

        #region AllBlobTypeSamples

        /// <summary>
        /// Reads the blob's properties.
        /// </summary>
        /// <param name="blob">A BlobClient object. All blob types (block blob, append blob, and page blob) are derived from BlobClient.</param>
        private static async Task PrintBlobPropertiesAndMetadata(BlobBaseClient blob)
        {
            // Write out properties that are common to all blob types.
            Console.WriteLine();
            Console.WriteLine("-----Blob Properties-----");
            Console.WriteLine("\t Name: {0}", blob.Name);
            Console.WriteLine("\t Container: {0}", blob.BlobContainerName);
            Console.WriteLine("\t BlobType: {0}", (await blob.GetPropertiesAsync()).Value.BlobType);

            // If the blob is a snapshot, write out snapshot properties.
            var builder = new BlobUriBuilder(blob.Uri);
            if (builder.Snapshot != null)
            {
                blob.CreateSnapshot();
                Console.WriteLine($"Snapshot Time: {builder.Snapshot}");
                Console.WriteLine($"Snapshot URI: {blob.Uri}");
            }
            var properties = (await blob.GetPropertiesAsync()).Value;
            Console.WriteLine("\t LeaseState: {0}", properties.LeaseState);
            // If the blob has been leased, write out lease properties.
            if (properties.LeaseState != LeaseState.Available)
            {
                Console.WriteLine("\t LeaseDuration: {0}", properties.LeaseDuration);
                Console.WriteLine("\t LeaseStatus: {0}", properties.LeaseStatus);
            }

            Console.WriteLine("\t CacheControl: {0}", properties.CacheControl);
            Console.WriteLine("\t ContentDisposition: {0}", properties.ContentDisposition);
            Console.WriteLine("\t ContentEncoding: {0}", properties.ContentEncoding);
            Console.WriteLine("\t ContentLanguage: {0}", properties.ContentLanguage);
            Console.WriteLine("\t ContentMD5: {0}", properties.ContentHash);
            Console.WriteLine("\t ContentType: {0}", properties.ContentType);
            Console.WriteLine("\t ETag: {0}", properties.ETag);
            Console.WriteLine("\t LastModified: {0}", properties.LastModified);
            Console.WriteLine("\t Length: {0}", properties.ContentLength);

            // Write out properties specific to blob type.
            switch (properties.BlobType)
            {
                case BlobType.Append:
                    Console.WriteLine("\t AppendBlobCommittedBlockCount: {0}", properties.BlobCommittedBlockCount);
                    break;
                case BlobType.Page:
                    Console.WriteLine("\t PageBlobSequenceNumber: {0}", properties.BlobSequenceNumber);
                    break;
                default:
                    break;
            }

            Console.WriteLine();

            // Enumerate the blob's metadata.
            Console.WriteLine("Blob metadata:");
            foreach (var metadataItem in properties.Metadata)
            {
                Console.WriteLine("\tKey: {0}", metadataItem.Key);
                Console.WriteLine("\tValue: {0}", metadataItem.Value);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Reads the virtual directory's properties.
        /// </summary>
        /// <param name="dir">A BlobContainerClient object.</param>
        private static async IAsyncEnumerable<string> PrintVirtualDirectoryProperties(BlobContainerClient container, string root = "/")
        {
            var remainingFolders = new Queue<string>();
            remainingFolders.Enqueue(root);
            while (remainingFolders.Count > 0)
            {
                string path = remainingFolders.Dequeue();
                await foreach (BlobHierarchyItem blob in container.GetBlobsByHierarchyAsync(prefix: path, delimiter: "/"))
                {
                    if (blob.IsPrefix)
                    {
                        yield return blob.Prefix;
                        remainingFolders.Enqueue(blob.Prefix);
                    }
                }
            }
        }


        /// <summary>
        /// Lists blobs in the specified container using a flat listing, with an optional segment size specified, and writes 
        /// their properties and metadata to the console window.
        /// The flat listing returns a segment containing all of the blobs matching the listing criteria.
        /// In a flat listing, blobs are not organized by virtual directory.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <param name="segmentSize">The size of the segment to return in each call to the listing operation.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListBlobsFlatListingAsync(BlobContainerClient container, int? segmentSize)
        {
            // List blobs to the console window.
            Console.WriteLine("List blobs in segments (flat listing):");
            Console.WriteLine();

            try
            {
                // Call ListBlobsSegmentedAsync and enumerate the result segment returned, while the continuation token is non-null.
                // When the continuation token is null, the last segment has been returned and execution can exit the loop.
                // This overload allows control of the segment size. You can return all remaining results by passing null for the maxResults parameter, 
                // or by calling a different overload.
                // Note that requesting the blob's metadata as part of the listing operation 
                // populates the metadata, so it's not necessary to call FetchAttributes() to read the metadata.
                IAsyncEnumerable<Page<BlobItem>> pages = container.GetBlobsAsync(traits: BlobTraits.Metadata).AsPages(pageSizeHint: segmentSize);
                await foreach (Page<BlobItem> page in pages)
                {
                    foreach (BlobItem blobItem in page.Values)
                    {
                        Console.WriteLine("************************************");
                        Console.WriteLine(blobItem.Name);

                        // A flat listing operation returns only blobs, not virtual directories.
                        // Write out blob properties and metadata.
                        BlobClient blob = container.GetBlobClient(blobItem.Name);
                        await PrintBlobPropertiesAndMetadata(blob);

                    }
                }
                Console.WriteLine();
            }
            catch (RequestFailedException e)
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
        /// <param name="container">A BlobContainerClient object.</param>
        /// <param name="prefix">The blob prefix.</param>
        /// <returns>A Task object.</returns>
        private static async Task ListBlobsHierarchicalListingAsync(BlobContainerClient container, string prefix)
        {
            // List blobs in segments.
            Console.WriteLine("List blobs (hierarchical listing):");
            Console.WriteLine();
            await container.CreateIfNotExistsAsync();
            Func<string, Task> CreateVirtualFile = async path =>
                await container.GetBlobClient(path).UploadAsync(
                    new MemoryStream(Encoding.UTF8.GetBytes("Hello, world")),
                    overwrite: true);
            await CreateVirtualFile("toplevel.txt");
            await CreateVirtualFile("/foo/bar/baz/qux.txt");
            await CreateVirtualFile("/foo/bar/baz/quux.txt");
            await CreateVirtualFile("/azure/storage/blobs/demo.txt");
            await CreateVirtualFile("/azure/storage/queues/demo.txt");
            await CreateVirtualFile("/azure/storage/files/demo.txt");
            await CreateVirtualFile("/azure/storage/datalake/demo.txt");
            Console.WriteLine("Storage folders:");
            await foreach (string folder in PrintVirtualDirectoryProperties(container, "/azure/storage/"))
            {
                Console.WriteLine($"    {folder}");
            }

            Console.WriteLine("\nAll folders:");
            await foreach (string folder in PrintVirtualDirectoryProperties(container))
            {
                Console.WriteLine($"    {folder}");
            }
            try
            {
                // Call ListBlobsSegmentedAsync recursively and enumerate the result segment returned, while the continuation token is non-null.
                // When the continuation token is null, the last segment has been returned and execution can exit the loop.
                // Note that blob snapshots cannot be listed in a hierarchical listing operation.
                var resultSegment = container.GetBlobsByHierarchy(BlobTraits.Metadata, BlobStates.None, "/", prefix);

                foreach (var blobItem in resultSegment)
                {
                    if (blobItem.IsBlob)
                    {
                        Console.WriteLine("************************************");
                        Console.WriteLine(blobItem.Blob.Name);
                        BlobClient blob = container.GetBlobClient(blobItem.Blob.Name);
                        // Write out blob properties and metadata.
                        await PrintBlobPropertiesAndMetadata(blob);
                    }
                }
                Console.WriteLine();
            }
            catch (RequestFailedException e)
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
        private static async Task<BlobClient> CreateRandomlyNamedBlockBlobAsync(BlobContainerClient container)
        {
            // Get a reference to a blob that does not yet exist.
            // The GetBlockBlobReference method does not make a request to the service, but only creates the object in memory.
            string blobName = BlobPrefix + Guid.NewGuid();
            BlobClient blob = container.GetBlobClient(blobName);

            // For the purposes of the sample, check to see whether the blob exists.
            Console.WriteLine("Blob {0} exists? {1}", blobName, (await blob.ExistsAsync()).Value);

            try
            {
                await blob.UploadAsync(BinaryData.FromString($"This is a blob named {blobName}"));
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Check again to see whether the blob exists.
            Console.WriteLine("Blob {0} exists? {1}", blobName, (await blob.ExistsAsync()).Value);

            return blob;
        }


        /// <summary>
        /// Gets a reference to a blob by making a request to the service.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="blobName">The blob name.</param>
        /// <returns>A Task object.</returns>
        private static async Task GetExistingBlobReferenceAsync(BlobContainerClient container, string blobName)
        {
            try
            {
                // Get a reference to a blob.
                BlobClient blob = container.GetBlobClient(blobName);

                // The previous call gets the blob's properties, so it's not necessary to call FetchAttributes
                // to read a property.
                Console.WriteLine("Blob {0} was last modified at {1} local time.", blobName,
                  (await blob.GetPropertiesAsync()).Value.LastModified.LocalDateTime);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                Console.WriteLine("Blob {0} does not exist.", blobName);
                Console.WriteLine("Additional error information: " + e.Message);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Creates the specified number of sequentially named block blobs, in a flat structure.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <param name="numberOfBlobs">The number of blobs to create.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateSequentiallyNamedBlockBlobsAsync(BlobContainerClient container, int numberOfBlobs)
        {
            try
            {
                BlobClient blob;
                string blobName = string.Empty;

                for (int i = 1; i <= numberOfBlobs; i++)
                {
                    // Format string for blob name.
                    blobName = i.ToString("00000") + ".txt";

                    // Get a reference to the blob.
                    blob = container.GetBlobClient(blobName);

                    var metadata = new Dictionary<string, string> {
                      { "DateCreated", DateTime.UtcNow.ToLongDateString() },
                      { "TimeCreated", DateTime.UtcNow.ToLongTimeString() }
                    };

                    await blob.UploadAsync(BinaryData.FromString($"This is blob {blobName}"), new BlobUploadOptions
                    {
                        Metadata = metadata
                    });
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Creates the specified number of nested block blobs at a specified number of levels.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <param name="numberOfLevels">The number of levels of blobs to create.</param>
        /// <param name="numberOfBlobsPerLevel">The number of blobs to create per level.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateNestedBlockBlobsAsync(BlobContainerClient container, short numberOfLevels, short numberOfBlobsPerLevel)
        {
            try
            {
                BlobClient blob;
                string blobName = string.Empty;
                string virtualDirName = string.Empty;

                // Create a nested blob structure.
                for (int i = 1; i <= numberOfLevels; i++)
                {
                    // Construct the virtual directory name, which becomes part of the blob name.
                    virtualDirName += string.Format("level{0}", i);
                    for (int j = 1; j <= numberOfBlobsPerLevel; j++)
                    {
                        // Construct string for blob name.
                        blobName = virtualDirName + string.Format("{0}-{1}.txt", i, j.ToString("00000"));

                        // Get a reference to the blob.
                        blob = container.GetBlobClient(blobName);

                        // Set some metadata on the blob.
                        var metadata = new Dictionary<string, string>
                        {
                          {"DateCreated", DateTime.UtcNow.ToLongDateString()},
                          {"TimeCreated", DateTime.UtcNow.ToLongTimeString()}
                        };

                        // Write the blob URI to its contents.
                        await blob.UploadAsync(BinaryData.FromString($"Absolute URI to blob: " + blob.Uri.AbsoluteUri + "."), new BlobUploadOptions
                        {
                            Metadata = metadata
                        });
                    }
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Creates a new blob and takes a snapshot of the blob.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CreateBlockBlobSnapshotAsync(BlobContainerClient container)
        {
            // Create a new blob in the container.
            BlobClient baseBlob = container.GetBlobClient("sample-base-blob.txt");

            try
            {
                // Add blob metadata.
                var metadata = new Dictionary<string, string> { { "ApproxBlobCreatedDate", DateTime.UtcNow.ToString() } };
                await baseBlob.UploadAsync(BinaryData.FromString($"Base blob: {0}"), new BlobUploadOptions
                {
                    Metadata = metadata
                });

                // Sleep 5 seconds.
                await Task.Delay(5000);

                // Create a snapshot of the base blob.
                // Specify metadata at the time that the snapshot is created to specify unique metadata for the snapshot.
                // If no metadata is specified when the snapshot is created, the base blob's metadata is copied to the snapshot.
                metadata = new Dictionary<string, string>();
                metadata.Add("ApproxSnapshotCreatedDate", DateTime.UtcNow.ToString());
                await baseBlob.CreateSnapshotAsync(metadata);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        /// <summary>
        /// Gets a reference to a blob created previously, and copies it to a new blob in the same container.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task CopyBlockBlobAsync(BlobContainerClient container)
        {
            BlobBaseClient sourceBlob = null;
            BlobBaseClient destBlob = null;
            string leaseId = null;

            try
            {
                string blockblobname = container.GetBlobs().Where(blobs => blobs.Properties.BlobType == BlobType.Block)
                  .Select(blobs => blobs.Name).FirstOrDefault();
                // Get a block blob from the container to use as the source.
                sourceBlob = container.GetBlockBlobClient(blockblobname);

                // Lease the source blob for the copy operation to prevent another client from modifying it.
                // Specifying null for the lease interval creates an infinite lease.
                var leaseClient = sourceBlob.GetBlobLeaseClient(null);
                var blobLease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(15));
                leaseId = blobLease.Value.LeaseId;
                // Get a reference to a destination blob (in this case, a new blob).
                destBlob = container.GetBlockBlobClient("copy of " + sourceBlob.Name);

                // Ensure that the source blob exists.
                if (await sourceBlob.ExistsAsync())
                {
                    // Get the ID of the copy operation.
                    CopyFromUriOperation copyFromUriOperation = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);

                    // Fetch the destination blob's properties before checking the copy state.
                    BlobProperties properties = await destBlob.GetPropertiesAsync();

                    Console.WriteLine("Status of copy operation: {0}", properties.CopyStatus);
                    Console.WriteLine("Copy Status Description : {0}", properties.CopyStatusDescription);
                    Console.WriteLine("Copy Progress: {0}", properties.CopyProgress);
                    Console.WriteLine();
                    await copyFromUriOperation.WaitForCompletionAsync();
                }
            }
            catch (RequestFailedException e)
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
                    if ((await sourceBlob.GetPropertiesAsync()).Value.LeaseState != LeaseState.Available)
                    {
                        var leaseClient = sourceBlob.GetBlobLeaseClient(leaseId);
                        await leaseClient.BreakAsync(new TimeSpan(0));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a large block blob, and copies it to a new blob in the same container. If the copy operation
        /// does not complete within the specified interval, abort the copy operation.
        /// </summary>
        /// <param name="container">A BlobContainerClient object.</param>
        /// <param name="sizeInMb">The size of block blob to create, in MB.</param>
        /// <returns>A Task object.</returns>
        private static async Task CopyLargeBlockBlobAsync(BlobContainerClient container, int sizeInMb)
        {
            // Create an array of random bytes, of the specified size.
            byte[] bytes = new byte[sizeInMb * 1024 * 1024];
            Random rng = new Random();
            rng.NextBytes(bytes);

            // Get a reference to a new block blob.
            BlobClient sourceBlob = container.GetBlobClient("LargeSourceBlob");

            // Get a reference to the destination blob (in this case, a new blob).
            BlobClient destBlob = container.GetBlobClient("copy of " + sourceBlob.Name);

            string leaseId = null;

            try
            {
                // Create a new block blob comprised of random bytes to use as the source of the copy operation.
                await sourceBlob.UploadAsync(BinaryData.FromBytes(bytes));

                // Get the ID of the copy operation.
                CopyFromUriOperation copyFromUriOperation = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                string copyId = copyFromUriOperation.Id;

                // Sleep for 1 second. In a real-world application, this would most likely be a longer interval.
                System.Threading.Thread.Sleep(1000);

                // Check the copy status. If it is still pending, abort the copy operation.
                if (!copyFromUriOperation.HasCompleted)
                {
                    await destBlob.AbortCopyFromUriAsync(copyId);
                    Console.WriteLine("Copy operation {0} has been aborted.", copyId);
                }

                Console.WriteLine();
            }
            catch (RequestFailedException e)
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

                    if ((await sourceBlob.GetPropertiesAsync()).Value.LeaseState != LeaseState.Available)
                    {
                        var leaseClient = sourceBlob.GetBlobLeaseClient(leaseId);
                        await leaseClient.BreakAsync(new TimeSpan(0)); ;
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
        /// <param name="container">A BlobContainerClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task UploadBlobInBlocksAsync(BlobContainerClient container)
        {
            // Create an array of random bytes, of the specified size.
            byte[] randomBytes = new byte[5 * 1024 * 1024];
            Random rnd = new Random();
            rnd.NextBytes(randomBytes);

            // Get a reference to a new block blob.
            BlockBlobClient blob = container.GetBlockBlobClient("sample-blob-" + Guid.NewGuid());

            // Specify the block size as 256 KB.
            int blockSize = 256 * 1024;

            MemoryStream msWrite = null;

            for (int index = 0; index < randomBytes.Length; index += blockSize)
            {
                // Create new stream of bytes.
                msWrite = new MemoryStream(randomBytes, index, Math.Min(blockSize, randomBytes.Length - index));
                msWrite.Position = 0;
            }
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
                        await blob.StageBlockAsync(blockId, new MemoryStream(bytesToWrite), blockHash);
                        // Increment and decrement the counters.
                        bytesRead += bytesToRead;
                        bytesLeft -= bytesToRead;
                        blockNumber++;
                    }

                    // Read the block list that we just created. The blocks will all be uncommitted at this point.
                    await ReadBlockListAsync(blob);

                    // Commit the blocks to a new blob.
                    await blob.CommitBlockListAsync(blockIDs);

                    // Read the block list again. Now all blocks will be committed.
                    await ReadBlockListAsync(blob);
                }
                catch (RequestFailedException e)
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
        /// <param name="blob">A BlockBlobClient object.</param>
        /// <returns>A Task object.</returns>
        private static async Task ReadBlockListAsync(BlockBlobClient blob)
        {
            var blockList = await blob.GetBlockListAsync();
            // Get the blob's block list.
            foreach (var listBlockItem in blockList.Value.CommittedBlocks)
            {
                Console.WriteLine(
                                    "Block {0} has been committed to block list. Block length = {1}",
                                    listBlockItem.Name,
                                    listBlockItem.Size);
            }
            foreach (var listBlockItem in blockList.Value.UncommittedBlocks)
            {
                Console.WriteLine(
                                      "Block {0} is uncommitted. Block length = {1}",
                                      listBlockItem.Name,
                                      listBlockItem.Size);

            }

            Console.WriteLine();
        }


        private static Uri GetBlobSasUri(BlobContainerClient container, string blobName, StorageSharedKeyCredential storageSharedKeyCredential, string sharedAccessPolicyName)
        {
            var policy = new BlobSasBuilder
            {
                BlobContainerName = container.Name,
                Identifier = sharedAccessPolicyName,
                BlobName = blobName
            };

            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            var sasUri = new UriBuilder(container.GetBlobClient(blobName).Uri);
            sasUri.Query = sas;
            //Return the URI string for the container, including the SAS token.
            return sasUri.Uri;

        }

        /// <summary>
        /// Tests a blob SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        /// <returns>A Task object.</returns>
        private static async Task TestBlobSASAsync(Uri sasUri, string blobContent)
        {
            // Try performing blob operations using the SAS provided.

            // Return a reference to the blob using the SAS URI.
            var blob = new BlobClient(sasUri);

            // Create operation: Upload a blob with the specified name to the container.
            // If the blob does not exist, it will be created. If it does exist, it will be overwritten.
            try
            {
                await blob.UploadAsync(BinaryData.FromString(blobContent));

                Console.WriteLine("Create operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Create operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Write operation: Add metadata to the blob
            try
            {
                string metadataName = "name";
                string metadataValue = "value";
                IDictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add(metadataName, metadataValue);
                await blob.SetMetadataAsync(metadata);

                Console.WriteLine("Write operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Write operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Read operation: Read the contents of the blob.
            try
            {
                MemoryStream msRead = new MemoryStream();
                using (msRead)
                {
                    await blob.DownloadToAsync(msRead);
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
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Read operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            // Delete operation: Delete the blob.
            try
            {
                await blob.DeleteAsync();
                Console.WriteLine("Delete operation succeeded for SAS {0}", sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e) when (e.Status == 403)
            {
                Console.WriteLine("Delete operation failed for SAS {0}", sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }


        /// <summary>
        /// Uploads an array of bytes to a new blob.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="numBytes">The number of bytes to upload.</param>
        /// <returns></returns>
        private static async Task UploadByteArrayAsync(BlobContainerClient container, long numBytes)
        {
            BlobClient blob = container.GetBlobClient(BlobPrefix + "byte-array-" + Guid.NewGuid());

            // Write an array of random bytes to a block blob. 
            Console.WriteLine("Write an array of bytes to a block blob");
            byte[] sampleBytes = new byte[numBytes];
            Random random = new Random();
            random.NextBytes(sampleBytes);

            try
            {
                await blob.UploadAsync(BinaryData.FromBytes(sampleBytes));
            }
            catch (RequestFailedException e)
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
        /// <param name="blobServiceClient"></param>
        private static async Task CorsSampleAsync(BlobServiceClient blobServiceClient)
        {
            // Get CORS rules
            Console.WriteLine("Get CORS rules");

            Response<BlobServiceProperties> serviceProperties = await blobServiceClient.GetPropertiesAsync();

            // Add CORS rule
            Console.WriteLine("Add CORS rule");

            serviceProperties.Value.Cors =
                new[]
                {
                    new BlobCorsRule
                    {
                        MaxAgeInSeconds = 3600,
                        AllowedHeaders = "*",
                        AllowedMethods = "GET",
                        AllowedOrigins = "*",
                        ExposedHeaders = "*"
                    }
                };
            await blobServiceClient.SetPropertiesAsync(serviceProperties);
            Console.WriteLine();
        }

        /// <summary>
        /// Get a list of valid page ranges for a page blob
        /// </summary>
        /// <param name="container"></param>
        /// <returns>A Task object.</returns>
        private static async Task PageRangesSampleAsync(BlobContainerClient container)
        {
            await container.CreateIfNotExistsAsync();

            Console.WriteLine("Create Page Blob");
            PageBlobClient pageBlob = container.GetPageBlobClient("blob1");
            pageBlob.Create(4 * 1024);

            Console.WriteLine("Write Pages to Blob");
            byte[] buffer = GetRandomBuffer(1024);
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                await pageBlob.UploadPagesAsync(memoryStream, 512);
            }

            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                {
                    await pageBlob.UploadPagesAsync(memoryStream, 3 * 1024);
                }


                Console.WriteLine("Get Page Ranges");
                IEnumerable<HttpRange> pageRanges = (await pageBlob.GetPageRangesAsync()).Value.PageRanges;
                foreach (HttpRange pageRange in pageRanges)
                {
                    Console.WriteLine(pageRange.ToString());
                }

                // Clean up after the demo. This line is not strictly necessary as the container is deleted in the next call.
                // It is included for the purposes of the example. 
                Console.WriteLine("Delete page blob");
                await pageBlob.DeleteIfExistsAsync();
                Console.WriteLine();
            }
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
