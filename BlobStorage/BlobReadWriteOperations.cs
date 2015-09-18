using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobStorage
{
    public class BlobReadWriteOperations
    {

        public static void RunReadWriteOperations()
        {
            // Retrieve the connection string from the app.config file.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Microsoft.Azure.CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a randomly named container for sample purposes.
            CloudBlobContainer container = blobClient.GetContainerReference("sample-container" + DateTime.Now.Ticks);

            // Create the container if it does not exist.
            container.CreateIfNotExists();

            // Set container's public access level to "blob", which means user can access blobs in the 
            // container anonymously, but cannot access container data, or list the blobs in the container.
            // The user must have a URL to the blob to access it anonymously.
            BlobContainerPermissions permissions = container.GetPermissions();
            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            container.SetPermissions(permissions);

            Console.WriteLine("Enter the path to a local file to upload to a blob:");
            string localFilePath = Console.ReadLine();
            Console.WriteLine("Blob address: {0}", UploadFromFile(container, localFilePath));
            Console.WriteLine();

            Console.WriteLine("Enter some text to write to a blob:");
            string textToUpload = Console.ReadLine();
            Console.WriteLine("Blob address: {0}", UploadText(container, textToUpload));

        }

        static string UploadFromFile(CloudBlobContainer container, string localFilePath)
        {
            string blobAddress = string.Empty;
            CloudBlockBlob blob = 
            container.GetBlockBlobReference(Path.GetFileName(localFilePath));
            
            try
            { 
                blob.UploadFromFile(localFilePath, FileMode.Open);
                blobAddress = blob.Uri.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown: {0}", e.Message);
            }

            return blobAddress;
        }

        static string UploadText(CloudBlobContainer container, string textToUpload)
        {
            string blobAddress = string.Empty;
            CloudBlockBlob blob = container.GetBlockBlobReference(GetRandomBlobName());
            blob.UploadText(textToUpload);
            blobAddress = blob.Uri.ToString();
            return blobAddress;
        }

        static string UploadFromByteArray(CloudBlobContainer container, Byte[] uploadBytes)
        {
            string blobAddress = string.Empty;

            // Generate an array of ten random bytes.
            Random rnd = new Random();
            byte[] bytes = new byte[10];
            rnd.NextBytes(bytes);

            CloudBlockBlob blob = container.GetBlockBlobReference(GetRandomBlobName());
            blob.UploadFromByteArray(uploadBytes, 0, uploadBytes.Length);
            blobAddress = blob.Uri.ToString();
            return blobAddress;
        }

        public string UploadFromStream(CloudBlobContainer container, Stream stream)
        {
            string blobAddress = string.Empty;

            // Reset the stream back to its starting point (no partial saves)
            stream.Position = 0;
            CloudBlockBlob blob = container.GetBlockBlobReference(GetRandomBlobName());
            blob.UploadFromStream(stream);
            blobAddress = blob.Uri.ToString();
            return blobAddress;
        }

        // TODO: Robin wrote these methods initially. I am rewriting them to fit into the sample, but am not done yet.

        //internal string DownloadFile(CloudBlobContainer container, string blobName, string downloadFolder)
        //{
        //    string status = string.Empty;
        //    CloudBlockBlob blobSource = container.GetBlockBlobReference(blobName);
        //    if (blobSource.Exists())
        //    {
        //    //blob storage uses forward slashes, windows uses backward slashes; do a replace
        //    //  so localPath will be right
        //    string localPath = Path.Combine(downloadFolder, blobSource.Name.Replace(@"/", @"\"));
        //    //if the directory path matching the "folders" in the blob name don't exist, create them
        //    string dirPath = Path.GetDirectoryName(localPath);
        //    if (!Directory.Exists(localPath))
        //    {
        //        Directory.CreateDirectory(dirPath);
        //    }
        //    blobSource.DownloadToFile(localPath, FileMode.Create);
        //    }
        //    status = "Downloaded file.";
        //    return status;
        //}

        //internal Byte[] DownloadToByteArray(CloudBlobContainer container, string targetFileName)
        //{
        //    CloudBlockBlob blob = container.GetBlockBlobReference(targetFileName);
        //    //you have to fetch the attributes to read the length
        //    blob.FetchAttributes();
        //    long fileByteLength = blob.Properties.Length;
        //    Byte[] myByteArray = new Byte[fileByteLength];
        //    blob.DownloadToByteArray(myByteArray, 0);
        //    return myByteArray;
        //}

        //public string DownloadToStream(CloudBlobContainer container, string sourceBlobName, Stream stream)
        //{
        //    string status = string.Empty;
        //    CloudBlockBlob blob = container.GetBlockBlobReference(sourceBlobName);
        //    blob.DownloadToStream(stream);
        //    status = "Downloaded successfully";
        //    return status;
        //}

        //internal string RenameBlob(CloudBlobContainer container, string blobName, string newBlobName)
        //{
        //    string status = string.Empty;

        //    CloudBlockBlob blobSource = container.GetBlockBlobReference(blobName);
        //    if (blobSource.Exists())
        //    {
        //        CloudBlockBlob blobTarget = container.GetBlockBlobReference(newBlobName);
        //        blobTarget.StartCopy(blobSource);
        //        blobSource.Delete();
        //    }

        //    status = "Finished renaming the blob.";
        //    return status;
        //}

        ////if the blob is there, delete it 
        ////check returning value to see if it was there or not
        //internal string DeleteBlob(CloudBlobContainer container, string blobName)
        //{
        //    string status = string.Empty;
        //    CloudBlockBlob blobSource = container.GetBlockBlobReference(blobName);
        //    bool blobExisted = blobSource.DeleteIfExists();
        //    if (blobExisted)
        //    {
        //        status = "Blob existed; deleted.";
        //    }
        //    else
        //    {
        //        status = "Blob did not exist.";
        //    }
        //    return status;
        //}

        ///// <summary>
        ///// parse the blob URI to get just the file name of the blob 
        ///// after the container. So this will give you /directory1/directory2/filename if it's in a "subfolder"
        ///// </summary>
        ///// <param name="theUri"></param>
        ///// <returns>name of the blob including subfolders (but not container)</returns>
        //private string GetFileNameFromBlobURI(Uri theUri, string containerName)
        //{
        //    string theFile = theUri.ToString();
        //    int dirIndex = theFile.IndexOf(containerName);
        //    string oneFile = theFile.Substring(dirIndex + containerName.Length + 1,
        //        theFile.Length - (dirIndex + containerName.Length + 1));
        //    return oneFile;
        //}

        //internal List<string> GetBlobList(CloudBlobContainer container)
        //{
        //    List<string> listOBlobs = new List<string>();
        //    foreach (IListBlobItem blobItem in container.ListBlobs(null, true, BlobListingDetails.All))
        //    {
        //        string oneFile = GetFileNameFromBlobURI(blobItem.Uri, container.Name);
        //        listOBlobs.Add(oneFile);
        //    }
        //    return listOBlobs;
        //}

        //internal List<string> GetBlobListForRelPath(CloudBlobContainer container, string relativePath)
        //{
        //    //first, check the slashes and change them if necessary
        //    //second, remove leading slash if it's there
        //    relativePath = relativePath.Replace(@"\", @"/");
        //    if (relativePath.Substring(0, 1) == @"/")
        //    relativePath = relativePath.Substring(1, relativePath.Length - 1);

        //    List<string> listOBlobs = new List<string>();
        //    foreach (IListBlobItem blobItem in 
        //    container.ListBlobs(relativePath, true, BlobListingDetails.All))
        //    {
        //        string oneFile = GetFileNameFromBlobURI(blobItem.Uri, container.Name);
        //        listOBlobs.Add(oneFile);
        //    }
        //    return listOBlobs;
        //}

        static void WriteToAppendBlob(CloudBlobContainer container)
        {
            //Get a reference to an append blob.
            CloudAppendBlob appendBlob = container.GetAppendBlobReference("append-blob.log");

            //Create the append blob. Note that if the blob already exists, the CreateOrReplace() method will overwrite it.
            //You can check whether the blob exists first to avoid overwriting it using CloudAppendBlob.Exists().
            appendBlob.CreateOrReplace();

            int numBlocks = 10;

            //Generate an array of random bytes.
            Random rnd = new Random();
            byte[] bytes = new byte[numBlocks];
            rnd.NextBytes(bytes);

            //Simulate a logging operation by writing text data and byte data to the end of the append blob.
            for (int i = 0; i < numBlocks; i++)
            {
                appendBlob.AppendText(String.Format("Timestamp: {0} \tLog Entry: {1}{2}",
                    DateTime.UtcNow.ToString(), bytes[i], Environment.NewLine));
            }

            //Read the append blob to the console window.
            Console.WriteLine(appendBlob.DownloadText());
        }

        static string GetRandomBlobName()
        {
            return string.Format("sample-blob-{0}{1}", DateTime.Now.Ticks.ToString(), ".txt");
        }

    
    }
}
