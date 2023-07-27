// See https://aka.ms/new-console-template for more information
using Microsoft.AspNetCore.StaticFiles;
using Minio;
using Minio.Exceptions;
using System.Net.Mime;
using System.Threading;

Console.WriteLine("Hello, World!");
var endpoint = "127.0.0.1:9000";
var accessKey = "mMAHHfIGjYVNxFM6pxDu";
var secretKey = "tSE2i6rq9fMbF852YsNikrsmDw81j0x0Et2wO3Z4";
try
{
    var minio = new MinioClient()
                        .WithEndpoint(endpoint)
                        .WithCredentials(accessKey, secretKey)
                        //.WithSSL()
                        .Build();
    Run(minio).Wait();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
Console.ReadLine();

// File uploader task.
async static Task Run(MinioClient minioClient)
{
    var bucketName = "daily-notes";
    var objectName = "abcde/230727-1.txt";
    var filePath = "C:\\Users\\FikriFirmansyahAkbar\\Documents\\Daily Notes\\230727-1.txt";
    // try object versioning by putting different file
    var anotherFilePath = "C:\\Users\\FikriFirmansyahAkbar\\Documents\\Daily Notes\\230726-1.txt";
    var contentType = "text/plain";

    try
    {
        // Make a bucket on the server, if not already present.
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        bool found = await minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
        if (!found)
        {
            var mbArgs = new MakeBucketArgs()
                .WithBucket(bucketName);
            await minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            var svArgs = new SetVersioningArgs()
                .WithBucket(bucketName)
                .WithVersioningEnabled();
            await minioClient.SetVersioningAsync(svArgs).ConfigureAwait(false);
        }

        // Upload a file to bucket.
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithFileName(filePath)
            .WithContentType(contentType);
        await minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

        // Upload another file to bucket.
        using var memoryStream = new MemoryStream(File.ReadAllBytes(anotherFilePath));
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(anotherFilePath, out var contentType2))
        {
            contentType = "application/octet-stream";
        }
        memoryStream.Position = 0;
        var putAnotherObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(memoryStream)
            .WithObjectSize(memoryStream.Length)
            .WithContentType(contentType);
        await minioClient.PutObjectAsync(putAnotherObjectArgs).ConfigureAwait(false);
        Console.WriteLine("Successfully uploaded " + objectName);

        // Download file
        // Check whether the object exists using statObject().
        // If the object is not found, statObject() throws an exception,
        // else it means that the object exists.
        // Execution is successful.
        StatObjectArgs statObjectArgs = new StatObjectArgs()
                                            .WithBucket(bucketName)
                                            .WithObject(objectName);
        await minioClient.StatObjectAsync(statObjectArgs);

        // Get input stream to have content of objectName from bucketName, print to stdout
        GetObjectArgs getObjectArgs = new GetObjectArgs()
                                          .WithBucket(bucketName)
                                          .WithObject(objectName)
                                          .WithCallbackStream((stream) =>
                                          {
                                              stream.CopyTo(Console.OpenStandardOutput());
                                          });
        await minioClient.GetObjectAsync(getObjectArgs);

        // Gets the object's data and stores it in objectName
        GetObjectArgs getDownloadedObjectArgs = new GetObjectArgs()
                                          .WithBucket(bucketName)
                                          .WithObject(objectName)
                                          .WithFile($"C:\\Users\\FikriFirmansyahAkbar\\Downloads\\");
        await minioClient.GetObjectAsync(getDownloadedObjectArgs);

        RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                  .WithBucket(bucketName)
                                  .WithObject(objectName);
        await minioClient.RemoveObjectAsync(rmArgs);
    }
    catch (MinioException e)
    {
        Console.WriteLine("File Upload Error: {0}", e.Message);
    }
}