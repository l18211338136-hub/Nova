using System.Text;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nova.Contracts.Constants;
using Nova.Contracts.Security;
using Nova.Contracts.Storage;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Web.Responses;
using Nova.Modules.Storage.Application.Attachments.Commands;
using Nova.Modules.Storage.Application.Attachments.Queries;
using Nova.Modules.Storage.Application.Files.Commands;
using Nova.Modules.Storage.Domain.Attachments;
using Nova.Modules.Storage.Domain.Files;
using Nova.Modules.Storage.Infrastructure.Persistence;
using Nova.Modules.Storage.Infrastructure.Providers;
using Nova.UnitTests.Handlers;
using NSubstitute;
using Mapster;

namespace Nova.UnitTests;

public class StorageModuleTests
{
    private static ITenantInfo CreateDummyTenant()
    {
        return new NovaTenantInfo
        {
            Id = TenantConstants.RootTenantId,
            Identifier = TenantConstants.RootTenantId,
            Name = "Root Tenant"
        };
    }

    [Fact]
    public void StorageOptions_ShouldHaveDefaultValue()
    {
        var options = new StorageOptions();
        Assert.Equal("MinIO", options.ActiveProvider);
        Assert.Equal("http://localhost:9000", options.S3.ServiceUrl);
        Assert.True(options.S3.ForcePathStyle);
    }

    [Fact]
    public async Task LocalStorageProvider_GetPreSignedUploadUrlsAsync_ShouldReturnBatchResponse()
    {
        // Arrange
        var options = Substitute.For<IOptions<StorageOptions>>();
        options.Value.Returns(new StorageOptions
        {
            ActiveProvider = "Local",
            LocalStorage = new LocalStorageOptions
            {
                RootPath = Path.Combine(Path.GetTempPath(), "Nova_Storage_Test_" + Guid.NewGuid()),
                BaseUrl = "/uploads"
            }
        });

        var provider = new LocalStorageProvider(options);

        var items = new List<PreSignedUrlRequestItem>
        {
            new() { FileName = "avatar.png", ContentType = "image/png", AttachmentType = AttachmentType.Avatar },
            new() { FileName = "product1.jpg", ContentType = "image/jpeg", AttachmentType = AttachmentType.Gallery },
            new() { FileName = "product2.jpg", ContentType = "image/jpeg", AttachmentType = AttachmentType.Gallery }
        };

        // Act
        var results = await provider.GetPreSignedUploadUrlsAsync(items, TimeSpan.FromMinutes(15));

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);

        Assert.Equal("avatar.png", results[0].FileName);
        Assert.StartsWith("/uploads/", results[0].AccessUrl);
        Assert.Contains("/api/v1/storage/upload", results[0].UploadUrl);

        Assert.Equal("product1.jpg", results[1].FileName);
        Assert.Equal("product2.jpg", results[2].FileName);
    }

    [Fact]
    public async Task LocalStorageProvider_UploadDownloadAndDelete_ShouldWork()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "Nova_Storage_IO_" + Guid.NewGuid());
        var options = Substitute.For<IOptions<StorageOptions>>();
        options.Value.Returns(new StorageOptions
        {
            ActiveProvider = "Local",
            LocalStorage = new LocalStorageOptions
            {
                RootPath = tempFolder,
                BaseUrl = "/uploads"
            }
        });

        var provider = new LocalStorageProvider(options);
        var content = "Hello Nova Storage File Stream";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var uploadReq = new StorageUploadRequest
        {
            FileName = "test_document.txt",
            ContentType = "text/plain",
            FileStream = stream
        };

        // Act 1: Upload
        var uploadRes = await provider.UploadAsync(uploadReq);

        // Assert 1
        Assert.NotNull(uploadRes);
        Assert.False(string.IsNullOrEmpty(uploadRes.FileKey));
        Assert.True(await provider.ExistsAsync(uploadRes.FileKey));

        // Act 2: Download & Read
        string readText;
        using (var downloadedStream = await provider.DownloadAsync(uploadRes.FileKey))
        {
            Assert.NotNull(downloadedStream);
            using var reader = new StreamReader(downloadedStream!);
            readText = await reader.ReadToEndAsync();
        }
        Assert.Equal(content, readText);

        // Act 3: Delete
        var deleteRes = await provider.DeleteAsync(uploadRes.FileKey);
        Assert.True(deleteRes);
        Assert.False(await provider.ExistsAsync(uploadRes.FileKey));

        // Cleanup
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
    }

    [Fact]
    public async Task InstantUpload_ExistingFileHash_ShouldReturnInstantSuccess()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<StorageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new StorageDbContext(CreateDummyTenant(), dbOptions);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid());

        var fileHash = "e10adc3949ba59abbe56e057f20f883e";
        var existingFile = FileObject.Create(
            "existing.png",
            "2026/08/07/exist.png",
            "nova-storage",
            "image/png",
            1024,
            StorageProviderType.MinIO,
            fileHash: fileHash,
            accessUrl: "http://localhost:9000/nova-storage/2026/08/07/exist.png"
        );
        db.FileObjects.Add(existingFile);
        await db.SaveChangesAsync();

        var handler = new InstantUploadCommandHandler(db, currentUser);

        // Act
        var context = HandlerTestHarness.CreateConsumeContext(new InstantUploadCommand
        {
            FileHash = fileHash,
            FileSize = 1024,
            FileName = "duplicate_user_avatar.png",
            ContentType = "image/png"
        });
        await handler.Consume(context);

        // Assert
        var response = IdentityIntegrationHarness.GetResponded<ApiResponse<InstantUploadResult>>(context);
        Assert.NotNull(response);
        Assert.True(response!.Data!.IsInstant);
        Assert.Equal(existingFile.FileKey, response.Data.FileKey);
    }

    [Fact]
    public async Task BindAttachment_SingleAvatar_ShouldReplacePreviousAvatar()
    {
        // Arrange InMemory DbContext
        var dbOptions = new DbContextOptionsBuilder<StorageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new StorageDbContext(CreateDummyTenant(), dbOptions);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid());

        var provider = Substitute.For<IStorageProvider>();
        provider.GetPreSignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("http://localhost:9000/nova-storage/avatar.png");

        var file1 = FileObject.Create("old_avatar.png", "2026/08/07/old.png", "nova-storage", "image/png", 1024, StorageProviderType.MinIO);
        var file2 = FileObject.Create("new_avatar.png", "2026/08/07/new.png", "nova-storage", "image/png", 2048, StorageProviderType.MinIO);
        db.FileObjects.AddRange(file1, file2);
        await db.SaveChangesAsync();

        var userId = "user_1001";
        var handler = new BindAttachmentCommandHandler(db, currentUser, provider);

        // Act 1: Bind first avatar
        var context1 = HandlerTestHarness.CreateConsumeContext(new BindAttachmentCommand
        {
            FileId = file1.Id,
            TargetType = "User",
            TargetId = userId,
            AttachmentType = AttachmentType.Avatar
        });
        await handler.Consume(context1);

        // Act 2: Bind second avatar (should replace first avatar)
        var context2 = HandlerTestHarness.CreateConsumeContext(new BindAttachmentCommand
        {
            FileId = file2.Id,
            TargetType = "User",
            TargetId = userId,
            AttachmentType = AttachmentType.Avatar
        });
        await handler.Consume(context2);

        // Assert: Only 1 Avatar attachment exists, and its FileId is file2
        var userAttachments = await db.Attachments
            .Where(x => x.TargetType == "User" && x.TargetId == userId && x.AttachmentType == AttachmentType.Avatar)
            .ToListAsync();

        Assert.Single(userAttachments);
        Assert.Equal(file2.Id, userAttachments[0].FileId);
    }

    [Fact]
    public async Task GetAttachmentsQuery_ShouldReturnOrderedAttachmentList()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<StorageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new StorageDbContext(CreateDummyTenant(), dbOptions);

        var provider = Substitute.For<IStorageProvider>();
        provider.GetPreSignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("http://localhost:9000/nova-storage/gallery.png");

        var file1 = FileObject.Create("img1.png", "2026/08/07/img1.png", "nova-storage", "image/png", 1024, StorageProviderType.MinIO);
        var file2 = FileObject.Create("img2.png", "2026/08/07/img2.png", "nova-storage", "image/png", 2048, StorageProviderType.MinIO);
        db.FileObjects.AddRange(file1, file2);

        var productId = "prod_999";
        var att1 = Attachment.Create(file1.Id, "Product", productId, AttachmentType.Gallery, sort: 1);
        var att2 = Attachment.Create(file2.Id, "Product", productId, AttachmentType.Gallery, sort: 0); // Sort = 0 should be first
        db.Attachments.AddRange(att1, att2);
        await db.SaveChangesAsync();

        var queryHandler = new GetAttachmentsQueryHandler(db, provider);
        var context = HandlerTestHarness.CreateConsumeContext(new GetAttachmentsQuery
        {
            TargetType = "Product",
            TargetId = productId,
            AttachmentType = AttachmentType.Gallery
        });

        // Act
        await queryHandler.Consume(context);

        // Assert
        var response = IdentityIntegrationHarness.GetResponded<ApiResponse<List<AttachmentDto>>>(context);
        Assert.NotNull(response);
        var list = response!.Data;
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Equal(file2.Id, list[0].FileId); // sort: 0 comes first
        Assert.Equal(file1.Id, list[1].FileId); // sort: 1 comes second
    }

    [Fact]
    public async Task UnbindAttachment_ShouldRemoveAttachment()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<StorageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new StorageDbContext(CreateDummyTenant(), dbOptions);

        var file = FileObject.Create("doc.pdf", "2026/08/07/doc.pdf", "nova-storage", "application/pdf", 5000, StorageProviderType.MinIO);
        var attachment = Attachment.Create(file.Id, "Order", "order_1", AttachmentType.Document);
        db.FileObjects.Add(file);
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();

        var handler = new UnbindAttachmentCommandHandler(db);
        var context = HandlerTestHarness.CreateConsumeContext(new UnbindAttachmentCommand { Id = attachment.Id });

        // Act
        await handler.Consume(context);

        // Assert
        var exists = await db.Attachments.AnyAsync(x => x.Id == attachment.Id);
        Assert.False(exists);
    }

    [Fact]
    public void StorageFileDto_MapsterAdapt_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var fileObj = FileObject.Create(
            "test_mapster.png",
            "2026/08/07/mapster.png",
            "nova-storage",
            "image/png",
            2048,
            StorageProviderType.MinIO,
            accessUrl: "http://localhost:9000/nova-storage/mapster.png"
        );

        // Act
        var dto = fileObj.Adapt<StorageFileDto>();

        // Assert
        Assert.Equal(fileObj.Id, dto.Id);
        Assert.Equal(fileObj.FileName, dto.FileName);
        Assert.Equal(fileObj.FileKey, dto.FileKey);
        Assert.Equal(fileObj.FileSize, dto.FileSize);
        Assert.Equal(fileObj.ContentType, dto.ContentType);
        Assert.Equal(fileObj.AccessUrl, dto.AccessUrl);
    }
}
