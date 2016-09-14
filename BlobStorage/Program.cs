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

    /// <summary>
    /// Azure Blob Storage Samples - Demonstrate how to use Blob storage. 
    /// Blob storage stores unstructured data such as text, binary data, documents or media files. 
    /// Blobs can be accessed from anywhere in the world via HTTP or HTTPS.
    ///
    /// Note: These samples use the .NET 4.5 asynchronous programming model to demonstrate how to call the Blob storage using the 
    /// asynchronous APIs available in the .NET storage client library. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to Azure Storage are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Blobs - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/
    /// - Blob Service Concepts - http://msdn.microsoft.com/en-us/library/dd179376.aspx 
    /// - Blob Service REST API - http://msdn.microsoft.com/en-us/library/dd135733.aspx
    /// - Blob Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// - Delegating Access with Shared Access Signatures - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-shared-access-signature-part-1/
    /// - Storage Emulator - http://msdn.microsoft.com/en-us/library/azure/hh403989.aspx
    /// - Configure a Connection String - https://azure.microsoft.com/documentation/articles/storage-configure-connection-string/
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run using either the Azure Storage Emulator that installs as part of this SDK - or by
        // updating the App.Config file with your AccountName and Key.
        //
        // To run the sample using the Storage Emulator (default option):
        //      1. Start the Azure Storage Emulator (once only) by pressing the Start button or the Windows key and searching for it
        //         by typing "Azure Storage Emulator". Select it from the list of applications to start it.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // To run the sample using the Storage Service:
        //      1. Open the app.config file and comment out the connection string for the emulator (UseDevelopmentStorage=True) and
        //         uncomment the connection string for the storage service (AccountName=[]...)
        //      2. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information.
        //      3. Set breakpoints and run the project using F10. 
        // 
        // If possible, run the samples with a storage account that is not used for production data. You can create a new storage 
        // account from the Azure portal if desired.
        // 
        // *************************************************************************************************************************

        static void Main(string[] args)
        {
            Console.WriteLine("Azure Blob Storage - Getting Started Samples\n");
            GettingStarted.CallBlobGettingStartedSamples();

            Console.WriteLine("Azure Blob Storage - Advanced Samples\n ");
            Advanced.CallBlobAdvancedSamples().Wait();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
