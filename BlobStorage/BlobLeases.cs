using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBlobStorageSample
{
    public class BlobLeases
    {


        //**********Tamra -- what about page blobs?  Can we genericize this?

        /// <summary>
        /// Acquire a lease on the blob for the requested time. 
        /// </summary>
        /// <param name="TimeInSeconds">Amount of time to hold the lease. This can be 15 to 60 seconds. If set to 0, the lease is infinite.</param>
        /// <param name="cloudBlockBlob">The blob on which to take out the lease.</param>
        /// <returns>Lease ID</returns>
        public string AcquireLease(int TimeInSeconds, CloudBlockBlob cloudBlockBlob)
        {
            
            if (TimeInSeconds > 0 && (TimeInSeconds < 15 || TimeInSeconds > 60))
            {
                throw new ApplicationException("Time for the lease must be 0 (infinite) or between 15 and 60 seconds.");
            }

            //set the lease time -- it's a timespan object
            TimeSpan? leaseTime = null;
            if (TimeInSeconds > 0)
            {
                //set the lease time to the number of seconds passed in -- it's a timespan object
                leaseTime = TimeSpan.FromSeconds(TimeInSeconds);
            }

            //acquire the actual lease and return the lease ID
            string leaseID = cloudBlockBlob.AcquireLease(leaseTime, null);

            //after running this code, you should get these results:
            // LeaseDuration = Fixed; LeaseState = Leased; LeaseStatus = Locked
            //after the lease expires, you should get these results:
            //  LeaseDuration = Unspecified, LeaseState = Expired, LeaseStatus = Unlocked

            return leaseID;
        }

        /// <summary>
        /// You must have the LeaseId of the blob in order to renew the lease.
        /// You can renew the lease even if it has expired, as long as the blob has not been modified or leased again since it expired.
        /// You can't change the duration when you renew the lease; it will use the previous lease duration.
        /// If you want to change the duration of the lease, use AcquireLease instead.
        /// </summary>
        /// <param name="cloudBlockBlob">Blob with the lease to be renewed</param>
        /// <param name="leaseId">current Lease Id</param>
        public void RenewLease(CloudBlockBlob cloudBlockBlob, string leaseId)
        {
            AccessCondition acc = new AccessCondition();
            acc.LeaseId = leaseId;
            cloudBlockBlob.RenewLease(acc);
        }

        /// <summary>
        /// Change the leaseId of the current active lease.
        /// Must provide current leaseId and new leaseId.
        /// An example where you might use this is where the lease ownership needs to be transferred from one component of your system to another. 
        /// For example, the first component has a lease on a blob, but needs to allow another component to operate on it. 
        /// The first component could pass the Lease Id to the second component. 
        /// The second component could change the Lease Id, do what it needs to do, then change it back to the original Lease Id. 
        /// This allows the second component to temporarily have exclusive access to the blob, 
        /// prevents the first component from modifying it while it has exclusive access, 
        /// and then returns access to the first component.
        /// </summary>
        /// <param name="cloudBlockBlob">Blob with the lease</param>
        /// <param name="leaseId">The current lease Id</param>
        /// <returns>The new lease Id</returns>
        public string ChangeLease(CloudBlockBlob cloudBlockBlob, string leaseId)
        {
            AccessCondition acc = new AccessCondition();
            acc.LeaseId = leaseId;
            string proposedLeaseId = System.Guid.NewGuid().ToString();
            string newLeaseId = cloudBlockBlob.ChangeLease(proposedLeaseId, acc);            
            return newLeaseId;
        }


        /// <summary>
        /// To release the lease on a blob, you can either let it expire or call 
        /// ReleaseLease to make it available immediately available for another client to lease it.
        /// You must have the current lease Id to release the lease.
        /// </summary>
        /// <param name="cloudBlockBlob">Blob with the lease on it</param>
        /// <param name="leaseId">Lease Id of the current lease that you want to release</param>
        public void ReleaseLease(CloudBlockBlob cloudBlockBlob, string leaseId)
        {
            AccessCondition acc = new AccessCondition();
            acc.LeaseId = leaseId;
            cloudBlockBlob.ReleaseLease(acc);
        }

        /// <summary>
        /// Break the lease; does not require a lease Id.
        /// If you do this, the lease on the blob cannot be renewed. 
        /// When you break a lease, you have to specify a timespan called the lease break period.
        /// During this period, no lease methods except for Break or Release can be performed.
        /// When you break the lease on a blob successfully, the response indicates the time interval
        /// in seconds until a new lease can be acquired.
        /// If one process breaks the lease on a blob and the original process that had the lease on the blob releases it,  
        /// another client can immediately acquire a new lease, rather than waiting for the lease break period time to elapse.
        /// </summary>
        /// <param name="cloudBlockBlob"></param>
        public void BreakLease(CloudBlockBlob cloudBlockBlob, int breakReleaseTimeInSeconds)
        {
            //set the break release time 
            TimeSpan? breakReleaseTime = TimeSpan.FromSeconds(breakReleaseTimeInSeconds);
            cloudBlockBlob.BreakLease(breakReleaseTime);
        }


        /// <summary>
        /// The blob's Properties properties has information about the lease.
        /// Fetch the attributes to populate the properties and then display them.
        /// </summary>
        /// <param name="cloudBlockBlob">Blob for which to display the information.</param>
        public void DisplayLeaseProperties(CloudBlockBlob cloudBlockBlob, string leaseId)
        {
            //fetch attributes to populate the cloudBlockBlob.Properties properties
            //  and display the properties we're interested in
            cloudBlockBlob.FetchAttributes();

            //if a lease Id is supplied, print it 
            if (!string.IsNullOrWhiteSpace(leaseId))
            {
                Console.WriteLine("     LeaseId = {0}", leaseId);
            }
            //display the rest of the lease properties
            Console.WriteLine("     LeaseDuration = {0}", cloudBlockBlob.Properties.LeaseDuration);
            Console.WriteLine("     LeaseState = {0}", cloudBlockBlob.Properties.LeaseState);
            Console.WriteLine("     LeaseStatus = {0}", cloudBlockBlob.Properties.LeaseStatus);
            Console.WriteLine(string.Empty);
        }

    }
}
