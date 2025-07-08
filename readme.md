# Light.TemporaryStreams 🌊

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.TemporaryStreams/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-1.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.TemporaryStreams/1.0.0/)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.GuardClauses/releases)

## Overview 🔍

Light.TemporaryStreams is a lightweight .NET library that helps you convert non-seekable streams into seekable temporary
streams. A temporary stream is either backed by a memory stream (for input smaller than 80 KB) or a file stream to a
temporary file. This is particularly useful for backend services that receive streams from HTTP requests (e.g.,
`application/octet-stream`, custom-parsed `multipart/form-data`) or download files from storage systems for further
processing.

## Key Features ✨

- 🚀 Easy conversion of non-seekable streams to seekable temporary streams
- 💾 Automatic management of temporary files (creation and deletion)
- 🔄 Smart switching between memory-based and file-based streams based on size (similar behavior to ASP.NET Core's
  `IFormFile`)
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

First, register the `ITemporaryStreamService` in your dependency injection container:

```csharp
services.AddTemporaryStreamService();
```

Then, inject the `ITemporaryStreamService` into any class that needs to convert non-seekable streams to seekable
temporary streams:

```csharp
using Light.TemporaryStreams;
using System.IO;
using System.Threading.Tasks;

public class SomeService
{
    private readonly ITemporaryStreamService _temporaryStreamService;
    private readonly IS3UploadClient _s3UploadClient;

    public SomeService(ITemporaryStreamService temporaryStreamService, IS3UploadClient s3UploadClient)
    {
        _temporaryStreamService = temporaryStreamService;
        _s3UploadClient = s3UploadClient;
    }

    public async Task<TempooraryStream> ProcessStreamAsync(
        Stream nonSeekableStream,
        CancellationToken cancellationToken = default
    )
    {
        // A temporary stream is either backed by a memory stream or a file stream
        // and thus seekable.
        await using var temporaryStream =
            await _temporaryStreamService.CopyToTemporaryStreamAsync(nonSeekableStream, cancellationToken);

        // Do something here with the temporary stream (analysis, processing, etc.)
        using (var pdf = new PdfProcessor(temporaryStream, leaveOpen: true))
        {
            var emptyOrIrrelevantPages = pdf.DetermineEmptyOrIrrelevantPages();
            pdf.RemovePages(emptyOrIrrelevantPages);
        }

        // Once you are done with processing, you can easily reset the stream to Position 0.
        // You can also use resilience patterns here and always reset the stream
        // for each upload attempt.
        temporaryStream.ResetStreamPosition();
        await _s3UploadClient.UploadAsync(temporaryStream);

        // When the temporary stream is disposed, it will automatically delete the
        // underlying file if necessary. No need to worry about manual cleanup.
        // This is also great when a temporary stream is returned in an
        // MVC Controller action or in Minimal API endpoint.
    }
}
```

## How It Works 🛠️

### Smart Memory Usage

A `TemporaryStream` is a wrapper around either:

- 🧠 A `MemoryStream` (for smaller files, less than 80 KB by default)
- 📄 A `FileStream` to a temporary file (for larger files)

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
`options` argument of aforementioned methods.

## Plugins 🧩

`CopyToTemporaryStreamAsync` supports a plugin system that allows you to extend the behavior of the stream copying
process. Light.TemporaryStreams comes with a `HashingPlugin` to calculate hashes. And, you can create your own plugins
by implementing the `ICopyToTemporaryStreamPlugin` interface.

### Basic Usage of HashingPlugin

```csharp
// You can simply pass any instance of System.Security.Cryptography.HashAlgorithm
// to the hashing plugin constructor. They will be disposed of when the hashingplugin is disposed of.
await using var hashingPlugin = new HashingPlugin([SHA1.Create(), MD5.Create()]);
await using var temporaryStream = await _temporaryStreamService
    .CopyToTemporaryStreamAsync(stream, [hashingPlugin], cancellationToken: cancellationToken);

// After copying is done, you can call GetHash to obtain the hash as a base64 string
// or GetHashArray to obtain the hash in its raw byte array form.
// Calling these methods before `CopyToTemporaryStreamAsync` has completed will result
// in an InvalidOperationException.
string sha1Base64Hash = hashingPlugin.GetHash(nameof(SHA1));
byte[] md5HashArray = hashingPlugin.GetHashArray(nameof(MD5));
```

### Hexadecimal Hashes via CopyToHashCalculator

## When To Use Light.TemporaryStreams 🤔

### When Processing Files in ASP.NET Core Without IFormFile

You might need to use Light.TemporaryStreams when:

- You need to manually parse multipart/form-data requests
- Your endpoint accepts both JSON and binary data
- You're processing files in a custom middleware
- You need to work with raw streams but still need seeking capability

See [Andrew Lock's blog post](https://andrewlock.net/reading-json-and-binary-data-from-multipart-form-data-sections-in-aspnetcore/)
for an example of when this might be needed.

### Example: Processing a File Upload with JSON Metadata

```csharp
using Light.TemporaryStreams;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ITemporaryStreamService _temporaryStreamService;

    public UploadController(ITemporaryStreamService temporaryStreamService)
    {
        _temporaryStreamService = temporaryStreamService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload()
    {
        if (!Request.HasFormContentType)
            return BadRequest("Multipart form data expected");

        var form = await Request.ReadFormAsync();

        // Get metadata from the form
        if (!form.TryGetValue("metadata", out var metadata))
            return BadRequest("Metadata is required");

        var metadataObj = JsonSerializer.Deserialize<FileMetadata>(metadata);

        // Get the file stream
        if (!form.Files.TryGetValue("file", out var formFile))
            return BadRequest("File is required");

        // Create a seekable temporary stream from the file stream
        using var fileStream = formFile.OpenReadStream();
        using var temporaryStream = await _temporaryStreamService.CopyToTemporaryStreamAsync(fileStream);

        // Now you can seek and process the file as needed
        temporaryStream.Position = 0;

        // Process the file...

        return Ok(new { message = "File processed successfully" });
    }

    public class FileMetadata
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
    }
}
```

## Configuring the TemporaryStreamService ⚙️

You can customize the behavior of `TemporaryStreamService` through `TemporaryStreamServiceOptions`:

```csharp
var options = new TemporaryStreamServiceOptions
{
    // Use memory stream for files smaller than 1 MB
    MemoryStreamThreshold = 1024 * 1024,

    // Custom buffer size for file operations
    BufferSize = 81920,

    // Custom file options
    FileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan,

    // Custom file mode
    FileMode = FileMode.Create,

    // Custom file access
    FileAccess = FileAccess.ReadWrite
};

var service = new TemporaryStreamService(options);
```

## Light.TemporaryStreams.Core vs. Light.TemporaryStreams 🧰

### Light.TemporaryStreams.Core

This package contains the core implementation including:

- `ITemporaryStreamService` interface
- `TemporaryStreamService` implementation
- `TemporaryStream` class
- `TemporaryStreamServiceOptions` for configuration
- Extension methods like `CopyToTemporaryStreamAsync`
- Plugin system and existing plugins (like `HashingPlugin`)

### Light.TemporaryStreams

This package builds on Core and adds integration with:

- Microsoft.Extensions.DependencyInjection for registering services
- Microsoft.Extensions.Logging for logging events
- Extension methods for `IServiceCollection` to register the service

Use Light.TemporaryStreams.Core if you're working in a non-DI environment or have your own DI container.
Use Light.TemporaryStreams if you're working in an ASP.NET Core application or any other application using
Microsoft.Extensions.DependencyInjection.

## Contributing 🤝

Contributions are welcome! First, create an issue Feel free to submit issues and pull requests.

## License 📜

This project is licensed under the MIT License - see
the [LICENSE](https://github.com/feO2x/Light.TemporaryStreams/blob/main/LICENSE) file for details.
