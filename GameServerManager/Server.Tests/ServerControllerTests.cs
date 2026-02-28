using Microsoft.Extensions.Configuration;

namespace GameServerManager.Server.Tests
{
    public class ServerControllerTests
    {
        private const string TestServerFilesPath = "TestServerFiles";
        private readonly ServerController _sut;
        private readonly Mock<IConfiguration> _configurationMock = new();
        private readonly Mock<DatabaseContext> _contextMock = new(new DbContextOptions<DatabaseContext>());

        public ServerControllerTests()
        {
            _sut = new ServerController(_configurationMock.Object, _contextMock.Object);
        }

        [Fact]
        public async Task GetList_ShouldReturnServers_WhenServersExist()
        {
            // Arrange
            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            // Act
            var serversList = await _sut.List(default);

            // Assert
            serversList.Value.Should().Contain(x => x.Id == serverId);
        }

        [Fact]
        public async Task GetFiles_ShouldReturnFiles_WhenPathHasFiles()
        {
            // Arrange
            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath)
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            // Act
            var files = await _sut.GetFiles(serverId, null, default);

            // Assert
            files.Value.Should().Contain(x => x.Name == "TextFile.txt");
        }

        [Fact]
        public async Task GetFiles_ShouldNotReturnFiles_WhenPathHasNoFiles()
        {
            // Arrange
            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath + "/NotExisting")
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            // Act
            var files = await _sut.GetFiles(serverId, null, default);

            // Assert
            files.Value.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task RenameFile_ShouldRenameFile_WhenFileExists()
        {
            // Arrange
            var testFileName = "RenameMe";
            var testFilePath = TestServerFilesPath + "/" + testFileName;

            var testFileNameRenamed = "RenamedFile";
            var testFileRenamedPath = TestServerFilesPath + "/" + testFileNameRenamed;

            File.Delete(testFileRenamedPath);
            File.Create(testFilePath).Dispose();

            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath)
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            // Act
            await _sut.RenameFile(new RenameFileRequest
            {
                ServerId = serverId,
                Path = testFileName,
                NewName = testFileNameRenamed
            }, default);

            // Assert
            File.Exists(testFilePath).Should().BeFalse();
            File.Exists(testFileRenamedPath).Should().BeTrue();
            File.Delete(testFileRenamedPath);
        }

        [Fact]
        public async Task DeleteFile_ShouldDeleteFile_WhenFileExists()
        {
            // Arrange
            var testFileName = "RenameMe";
            var testFilePath = TestServerFilesPath + "/" + testFileName;

            File.Create(testFilePath).Dispose();

            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath)
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            // Act
            await _sut.DeleteFile(serverId, testFileName, default);

            // Assert
            File.Exists(testFilePath).Should().BeFalse();
            File.Delete(testFilePath);
        }

        [Fact]
        public async Task UploadFile_ShouldCreateFile_WhenFileDoesNotExist()
        {
            // Arrange
            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath)
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            var testFileName = "NewFile";
            var testFilePath = TestServerFilesPath + "/" + testFileName;
            var fileContent = "Blah blah blah";
            using var fileStream = new MemoryStream(Encoding.Default.GetBytes(fileContent));

            File.Delete(testFilePath);

            var formFiles = new FormFile[]
            {
                new FormFile(fileStream, 0, fileContent.Length, testFileName, testFileName)
            };

            // Act
            await _sut.UploadFile(serverId, null, formFiles, default);

            // Assert
            File.Exists(testFilePath).Should().BeTrue();
            File.ReadAllText(testFilePath).Should().Be(fileContent);
            File.Delete(testFilePath);
        }

        [Fact]
        public async Task UploadFile_ShouldFail_WhenFileExists()
        {
            // Arrange
            var serverId = Guid.NewGuid();

            var servers = new Database.Server[]
            {
                new Database.Server
                {
                    Id = serverId,
                    Path = Path.GetFullPath(TestServerFilesPath)
                }
            };

            _contextMock.Setup(x => x.Server).ReturnsDbSet(servers);

            var testFileName = "NewFile";
            var testFilePath = TestServerFilesPath + "/" + testFileName;
            using var fileStream = new MemoryStream();

            File.Create(testFilePath).Dispose();

            var formFiles = new FormFile[]
            {
                new FormFile(fileStream, 0, 0, testFileName, testFileName)
            };

            // Act
            var res = await _sut.UploadFile(serverId, null, formFiles, default);

            // Assert
            (res as OkResult)?.Should().BeNull();
            File.Delete(testFilePath);
        }
    }
}