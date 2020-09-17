---
page_type: sample
languages:
- csharp
products:
- azure
description: "Demonstrates how to use the Blob Storage service."
urlFragment: storage-blob-dotnet-getting-started
---

# Azure Blob Storage Samples for .NET

Demonstrates how to use the Blob Storage service.
Blob storage stores unstructured data such as text, binary data, documents or media files.
Blobs can be accessed from anywhere in the world via HTTP or HTTPS.

Note: This sample uses the .NET 4.7.2 asynchronous programming model to demonstrate how to call Azure Storage using asynchronous API calls. When used in real applications, this approach enables you to improve the
responsiveness of your application. Calls to Azure Storage are prefixed by the `await` keyword. For more information about asynchronous programming using the Async/Await pattern, see [Asynchronous Programming with Async and Await (C# and Visual Basic)](https://msdn.microsoft.com/library/hh191443.aspx).

If you don't already have a Microsoft Azure subscription, you can
get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).

## Running this sample

This sample can be run using either the Azurite that installs as part of this SDK - or by
updating the App.Config file with your AccountName and Key.
To run the sample using the Azurite (default option):

1. Download and Install the mkcert [here](https://github.com/FiloSottile/mkcert#installation).
2. Clone Azurite to local [here](https://github.com/Azure/Azurite).
3. After cloning source code, execute following commands to install Azurite V3.

```bash
npm ci
npm run build
npm install -g
```

4. After intalling Azurite, execute following commands to start Azurite V3.

```bash
mkcert -install
mkcert certname
azurite --oauth basic --cert certname.pem --key certname-key.pem
```
5. Set breakpoints and run the project using F10.

To run the sample using the Storage Service

1. Open the app.config file and comment out the connection string and account for the Azurite and uncomment the connection string and account for the storage service (AccountName=[]...)
2. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in the App.Config file.
3. Set breakpoints and run the project using F10.

## More information
- [How to create, manage, or delete a storage account in the Azure Portal](https://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/)
- [Get started with Azure Blob storage (object storage) using .NET](https://azure.microsoft.com/documentation/articles/storage-dotnet-how-to-use-blobs/)
- [Blob Service Concepts](http://msdn.microsoft.com/en-us/library/dd179376.aspx)
- [Blob Service REST API](http://msdn.microsoft.com/en-us/library/dd135733.aspx)
- [Blob Service C# API](http://go.microsoft.com/fwlink/?LinkID=398944)
- [Delegating Access with Shared Access Signatures](http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-shared-access-signature-part-1/)
- [Azurite](https://github.com/Azure/Azurite)
- [Asynchronous Programming with Async and Await](http://msdn.microsoft.com/en-us/library/hh191443.aspx)
