# Light.TemporaryStreams 🌊

[![NuGet Badge](https://img.shields.io/nuget/v/Light.TemporaryStreams.svg)](https://www.nuget.org/packages/Light.TemporaryStreams/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/feO2x/Light.TemporaryStreams/blob/main/LICENSE)

## Overview 🔍

Light.TemporaryStreams is a .NET library that helps you convert non-seekable streams into seekable temporary streams.
This is particularly useful for backend services that receive streams from multipart/form-data requests or download
files from storage systems for further processing.

## Key Features ✨

- 🚀 Easy conversion of non-seekable streams to seekable temporary streams
- 💾 Automatic management of temporary files (creation and deletion)
- 🔄 Smart switching between memory-based and file-based streams based on size
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

```csharp
using Light.TemporaryStreams;
using System.IO;
using System.Threading.Tasks;

// Example: Convert a non-seekable stream to a seekable temporary stream
public async Task<Stream> MakeStreamSeekable(Stream nonSeekableStream)
{
    // Create the temporary stream service
    var temporaryStreamService = new TemporaryStreamService();

    // Copy the non-seekable stream to a temporary stream
    var temporaryStream = await temporaryStreamService.CopyToTemporaryStreamAsync(nonSeekableStream);

    // The temporary stream is seekable and positioned at the beginning
    temporaryStream.Position = 0;

    // When you're done, dispose the temporary stream
    // If it's backed by a file, the file will be automatically deleted
    return temporaryStream; // Remember to dispose this when done
}
```

## How It Works 🛠️

### TemporaryStream

A `TemporaryStream` is a wrapper around either:

- 🧠 A `MemoryStream` (for smaller files, less than 80 KB by default)
- 📄 A `FileStream` to a temporary file (for larger files)

This approach is similar to how `IFormFile` works in ASP.NET Core.

### Automatic Cleanup

When a `TemporaryStream` instance is disposed:

- If the underlying stream is a `FileStream`, the temporary file is automatically deleted
- You don't need to worry about cleaning up temporary files manually

## DI Integration 🔌

```csharp
using Light.TemporaryStreams;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Register the temporary stream service with default options
    services.AddTemporaryStreamService();

    // Or with custom options
    services.AddTemporaryStreamService(options =>
    {
        options.MemoryStreamThreshold = 1024 * 512; // Use memory stream for files less than 512 KB
        options.FileOptions = FileOptions.Asynchronous | FileOptions.DeleteOnClose;
    });
}
```

## Using Plugins 🧩

### Hash Calculation During Stream Copy

```csharp
using Light.TemporaryStreams;
using Light.TemporaryStreams.Hashing;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading.Tasks;

public async Task<(TemporaryStream Stream, string Md5Hash, string Sha256Hash)> CopyStreamWithHashing(Stream source)
{
    var temporaryStreamService = new TemporaryStreamService();

    // Create hash calculators for MD5 and SHA256
    var md5Calculator = new CopyToHashCalculator(MD5.Create(), "MD5");
    var sha256Calculator = new CopyToHashCalculator(SHA256.Create(), "SHA256");

    // Create the hashing plugin with both calculators
    var hashingPlugin = new HashingPlugin(
        ImmutableArray.Create(md5Calculator, sha256Calculator)
    );

    // Copy the stream and calculate hashes in one go
    var temporaryStream = await temporaryStreamService.CopyToTemporaryStreamAsync(
        source,
        ImmutableArray.Create<ICopyToTemporaryStreamPlugin>(hashingPlugin)
    );

    // Get the calculated hashes
    string md5Hash = hashingPlugin.GetHash("MD5");
    string sha256Hash = hashingPlugin.GetHash("SHA256");

    return (temporaryStream, md5Hash, sha256Hash);
}
```

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

Contributions are welcome! Feel free to submit issues and pull requests.

## License 📜

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
