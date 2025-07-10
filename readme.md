# Light.TemporaryStreams 🌊

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.TemporaryStreams/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-1.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.TemporaryStreams/1.0.0/)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.GuardClauses/releases)

## Overview 🔍

Light.TemporaryStreams is a lightweight .NET library that helps you convert non-seekable streams to seekable temporary streams. A temporary stream is either backed by a memory stream (for input smaller than 80 KB) or a file stream. This is particularly useful for backend services that receive streams from HTTP requests or download files from storage systems for further processing.

## Key Features ✨

- 🚀 Easy conversion of non-seekable streams to seekable temporary streams
- 💾 Automatic management of temporary files (creation and deletion)
- 🔄 Smart switching between memory-based and file-based streams depending on size (similar behavior to ASP.NET Core's `IFormFile`)
- 🧩 Plugin system for extending functionality (e.g., calculating hashes during stream copying)
- 🔌 Integration with Microsoft.Extensions.DependencyInjection and Microsoft.Extensions.Logging

## Installation 📦

```bash
dotnet add package Light.TemporaryStreams
```

For just the core functionality without DI and logging integration:

```bash
dotnet add package Light.TemporaryStreams.Core
```

## Basic Usage 🚀

First, register the `ITemporaryStreamService` and other dependencies of Light.TemporaryStreams with your dependency injection container:

```csharp
services.AddTemporaryStreamService();
```

Then, inject the `ITemporaryStreamService` into any class that needs to convert non-seekable streams to seekable
temporary streams:

```csharp
public class SomeService
{
    private readonly ITemporaryStreamService _temporaryStreamService;
    private readonly IS3UploadClient _s3UploadClient;

    public SomeService(
        ITemporaryStreamService temporaryStreamService,
        IS3UploadClient s3UploadClient
    )
    {
        _temporaryStreamService = temporaryStreamService;
        _s3UploadClient = s3UploadClient;
    }

    public async Task ProcessStreamAsync(
        Stream nonSeekableStream,
        CancellationToken cancellationToken = default
    )
    {
        // A temporary stream is either backed by a memory stream or a file stream and thus seekable.
        await using TemporaryStream temporaryStream =
            await _temporaryStreamService.CopyToTemporaryStreamAsync(
                nonSeekableStream,
                cancellationToken: cancellationToken
            );

        // Do something here with the temporary stream (analysis, processing, etc.).
        // For example, your code base might have a PdfProcessor that requires a seekable stream.
        using (var pdf = new PdfProcessor(temporaryStream, leaveOpen: true))
        {
            var emptyOrIrrelevantPages =
                await pdf.DetermineEmptyOrIrrelevantPagesAsync(cancellationToken);
            pdf.RemovePages(emptyOrIrrelevantPages);
        }

        // Once you are done with processing, you can easily reset the stream to Position 0.
        // You can also use resilience patterns here and always reset the stream
        // for each upload attempt.
        temporaryStream.ResetStreamPosition();
        await _s3UploadClient.UploadAsync(temporaryStream, cancellationToken);

        // When the temporary stream is disposed of (because of the await using at
        // the beginning of the method), it will automatically delete the
        // underlying file if necessary. No need to worry about manual cleanup.
        // This also works when a temporary stream is returned in an
        // MVC Controller action or in a Minimal API endpoint.
    }
}
```

## How It Works 🛠️

### Smart Memory Usage

A `TemporaryStream` is a wrapper around either:

- 🧠 A `MemoryStream` (for smaller files, less than 80 KB by default)
- 📄 A `FileStream` to a temporary file (for 80 KB or larger files)

This approach is similar to how `IFormFile` works in ASP.NET Core. You can adjust the threshold for using file streams
using the `TemporaryStreamServiceOptions.FileThresholdInBytes` property.

Use the `TemporaryStream.IsFileBased` property to check if the stream is backed by a file or a memory stream. Use
`TemporaryStream.TryGetUnderlyingFilePath` or `TemporaryStream.GetUnderlyingFilePath` to get the absolute file path.

### Automatic Cleanup

When a `TemporaryStream` instance is disposed:

- If the underlying stream is a `FileStream`, the temporary file is automatically deleted
- You don't need to worry about cleaning up temporary files manually

You can adjust this behavior using the `TemporaryStreamServiceOptions.DisposeBehavior` property.

### Temporary File Management

By default, temporary files are created using `Path.GetTempFileName()`. You can pass your own file path by providing a
value to the optional `filePath` argument of `ITemporaryStreamService.CreateTemporaryStream` or the
`CopyToTemporaryStreamAsync` extension methods.

By default, Light.TemporaryStreams uses `FileMode.Create`, thus files are either created or overwritten. You can adjust
this behavior using the `TemporaryStreamServiceOptions.FileStreamOptions` property.

### Temporary Stream Service Options

When you call `services.AddTemporaryStreamService()`, a singleton instance of `TemporaryStreamServiceOptions` is
registered with the DI container. This default instance is used when you do not explicitly pass a reference to
`ITemporaryStreamService.CreateTemporaryStream` or `CopyToTemporaryStreamAsync`.

However, if you want to deviate from the defaults in certain use cases, simply instantiate your own and pass them to the
`options` argument of aforementioned methods. The `TemporaryStreamServiceOptions` class is an immutable record.

## Plugins 🧩

`CopyToTemporaryStreamAsync` supports a plugin system that allows you to extend the behavior of the stream copying
process. Light.TemporaryStreams comes with a `HashingPlugin` to calculate hashes. You can also create your own plugins
by implementing the `ICopyToTemporaryStreamPlugin` interface.

### Basic Usage of HashingPlugin

```csharp
// You can simply pass any instance of System.Security.Cryptography.HashAlgorithm
// to the hashing plugin constructor. They will be disposed of when the
// hashingPlugin is disposed of.
await using var hashingPlugin = new HashingPlugin([SHA1.Create(), MD5.Create()]);
await using var temporaryStream =
    await _temporaryStreamService.CopyToTemporaryStreamAsync(
        stream,
        [hashingPlugin],
        cancellationToken: cancellationToken
    );

// After copying is done, you can call GetHash to obtain the hash as a base64 string
// or GetHashArray to obtain the hash in its raw byte array form.
// Calling these methods before `CopyToTemporaryStreamAsync` has completed will result
// in an InvalidOperationException.
string sha1Base64Hash = hashingPlugin.GetHash(nameof(SHA1));
byte[] md5HashArray = hashingPlugin.GetHashArray(nameof(MD5));
```

### More Control via CopyToHashCalculator

The `HashAlgorithm` instances passed to the `HashingPlugin` constructor in the previous example are actually converted to instances of `CopyToHashCalculator` via an implicit conversion operator. You can instantiate this class yourself to have more control over the conversion method that converts a hash byte array into a string as well as the name used to identify the hash calculator.

```csharp
// You can explicitly create instances of CopyToHashCalculator to have more control over the
// conversion method and the name that identifies the hash calculator within the HashingPlugin.
var sha1Calculator = new CopyToHashCalculator(
    SHA1.Create(),
    HashConversionMethod.UpperHexadecimal,
    "SHA1"
);
var md5Calculator = new CopyToHashCalculator(
    MD5.Create(),
    HashConversionMethod.None,
    "MD5"
);
await using var hashingPlugin = new HashingPlugin([sha1Calculator, md5Calculator]);

await using var temporaryStream =
    await _temporaryStreamService.CopyToTemporaryStreamAsync(
        stream,
        [hashingPlugin],
        cancellationToken: cancellationToken
    );

string sha1HexadecimalHash = hashingPlugin.GetHash("SHA1");
byte[] md5HashArray = hashingPlugin.GetHashArray("MD5");
```

## When To Use Light.TemporaryStreams 🤔

- Your service implements endpoints that receive `application/octet-stream` requests and you need to process the incoming stream in a seekable way.
- Your service implements endpoints that receive `multipart/form-data` requests and you cannot use `IFormFile`, for example because the request has both JSON and binary data. See [this blog post by Andrew Lock](https://andrewlock.net/reading-json-and-binary-data-from-multipart-form-data-sections-in-aspnetcore/) for an example.
- Your service downloads files from storage systems like Amazon S3 or Azure Storage Accounts and processes them further.
- Your endpoint wants to return a stream to the caller and the file should be gone after the request finishes.

## Light.TemporaryStreams.Core vs. Light.TemporaryStreams 🧰

### Light.TemporaryStreams.Core

This package contains the core implementation including:

- `ITemporaryStreamService` interface
- `TemporaryStreamService` implementation
- `TemporaryStream` class
- `TemporaryStreamServiceOptions` for configuration
- Extension method `CopyToTemporaryStreamAsync`
- Plugin system `ICopyToTemporaryStreamPlugin` and existing plugin `HashingPlugin`

### Light.TemporaryStreams

This package builds on Core and adds integration with:

- Microsoft.Extensions.DependencyInjection for registering services
- Microsoft.Extensions.Logging for logging when a temporary stream cannot be properly deleted

Use Light.TemporaryStreams.Core if you're working in a non-DI environment or have your own DI container.
Use Light.TemporaryStreams if you're working in an ASP.NET Core application or any other application supporting
Microsoft.Extensions.DependencyInjection and Microsoft.Extensions.Logging.

## Contributing 🤝

Contributions are welcome! First, create an issue to discuss your idea. After that, you can submit a pull request.

## License 📜

This project is licensed under the MIT License - see
the [LICENSE](https://github.com/feO2x/Light.TemporaryStreams/blob/main/LICENSE) file for details.

## Let there be... Light 💡

![Light Libraries Logo](https://raw.githubusercontent.com/feO2x/Light.GuardClauses/main/Images/light_logo.png)
