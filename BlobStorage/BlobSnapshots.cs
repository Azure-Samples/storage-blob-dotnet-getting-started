using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataBlobStorageSample
{
    public class BlobSnapshots
    {
        //The snapshot commands can also be run on a page blob, just change the incoming type accordingly.

        /// <summary>
        /// Retrieve all of the snapshots and the base blob, iterate through them
        /// </summary>
        /// <param name="cloudBlobContainer">Container in which the blob resides</param>
        /// <param name="blobName">Name of the blob</param>
        public void ListSnapshotsAndProperties(CloudBlobContainer cloudBlobContainer, string blobName)
        {
            Console.WriteLine("***** List Snapshots and their Properties for a blob *****");

            //get list of blobs and snapshots
            IEnumerable<IListBlobItem> listOfBlobs = GetListOfSnapshots(cloudBlobContainer, blobName);

            foreach (IListBlobItem blobItem in listOfBlobs)
            {
                //you must cast this as a CloudBlockBlob 
                //  because blobItem does not expose all of the properties
                CloudBlockBlob theBlob = blobItem as CloudBlockBlob;

                //Call FetchAttributes so it retrieves the metadata.
                theBlob.FetchAttributes();

                //print the snapshot information
                Console.WriteLine("theBlob IsSnapshot = {0}, SnapshotTime = {1}, snapshotURI = {2}",
                  theBlob.IsSnapshot, theBlob.SnapshotTime, theBlob.SnapshotQualifiedUri);

                //iterate through the metadata and display each key-value pair
                int index = 0;
                foreach (KeyValuePair<string, string> kvPair in theBlob.Metadata)
                {
                    index++;
                    Console.WriteLine(".MetaData ({0}) = {1},{2}", index, kvPair.Key, kvPair.Value);
                }
            }
        }

        /// <summary>
        /// Take a snapshot
        /// </summary>
        /// <param name="cloudBlockBlob">The blob for which to create the snapshot</param>
        /// <returns>The blob snapshot</returns>
        public CloudBlockBlob TakeASnapshot(CloudBlockBlob cloudBlockBlob)
        {           
            //create the snapshot and return a reference to it 
            CloudBlockBlob newBlob = cloudBlockBlob.CreateSnapshot();
            return newBlob;
        }

        /// <summary>
        /// Get a list of IListBlobItems that are snapshots. 
        /// </summary>
        /// <param name="cloudBlobContainer">Container in which the blobs reside</param>
        /// <param name="blobName">Name of the blob</param>
        /// <returns>List of snapshots for the specified blob</returns>
        public IEnumerable<IListBlobItem> GetListOfSnapshots(CloudBlobContainer cloudBlobContainer, string blobName)
        {
            IEnumerable<IListBlobItem> listOfBlobItems = cloudBlobContainer.ListBlobs(blobName, true, BlobListingDetails.Snapshots);
            return listOfBlobItems;
        }

        /// <summary>
        /// Copy a snapshot to a new blob.
        /// </summary>
        /// <param name="sourceBlob">The snapshot to be copied</param>
        /// <param name="cloudBlobContainer">The container to which the snapshot should be copied</param>
        /// <param name="newBlobName">The name of the new blob</param>
        public void CopySnapshotToBlob(CloudBlockBlob sourceBlob, CloudBlobContainer cloudBlobContainer, string newBlobName)
        {
            CloudBlockBlob blobTarget = cloudBlobContainer.GetBlockBlobReference(newBlobName);
            blobTarget.StartCopyFromBlob(sourceBlob);
        }

        /// <summary>
        /// Delete all snapshots for a blob.
        /// </summary>
        /// <param name="cloudBlockBlob">The blob whose snapshots are to be deleted</param>
        public void DeleteAllSnapshots(CloudBlockBlob cloudBlockBlob)
        {
            cloudBlockBlob.Delete(DeleteSnapshotsOption.DeleteSnapshotsOnly);
        }

        /// <summary>
        /// Delete the snapshots and the blob.
        /// </summary>
        /// <param name="cloudBlockBlob">The blob to be fully removed</param>
        public void DeleteSnapshotsAndBlob(CloudBlockBlob cloudBlockBlob)
        {
            cloudBlockBlob.Delete(DeleteSnapshotsOption.IncludeSnapshots);
        }

        /// <summary>
        /// Delete the blob, but only if it has no snapshots.
        /// </summary>
        /// <param name="cloudBlockBlob">The blob to be deleted</param>
        public void DeleteBlobIfItHasNoSnapshots(CloudBlockBlob cloudBlockBlob)
        {
            cloudBlockBlob.Delete(DeleteSnapshotsOption.None);
        }
    }
}
