using GameServerManager.Server.Database;
using GameServerManager.Server.Helpers;
using GameServerManager.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using System.Net.Sockets;
using System.Web;

namespace GameServerManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ServerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _dbContext;

        public ServerController(IConfiguration configuration, DatabaseContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<ServerInfo>>> List(CancellationToken ct)
        {
            return await _dbContext.Server.Select(x => new ServerInfo { Id = x.Id, Name = x.Name }).ToListAsync(ct);
        }

        [HttpGet("{serverId}/details")]
        public async Task<ActionResult<ServerDetails>> Details(Guid serverId, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var response = new ServerDetails
            {
                Id = server.Id,
                Name = server.Name,
            };

            var process = ProcessHelper.FindProcessByPort(server.Port);
            response.ProcessFound = process is not null;

            try
            {
                using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Blocking = false;
                await s.ConnectAsync("88.99.82.253", server.Port);
                response.Responding = true;
            }
            catch (SocketException)
            {
                response.Responding = false;
            }

            return response;
        }

        [HttpPost("start")]
        public async Task<ActionResult> Start(ServerIdRequest request, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == request.ServerId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var serverProc = ProcessHelper.FindProcessByPort(server.Port);

            if (serverProc is not null)
            {
                return Problem("The server is already running.");
            }

            if (!ProcessHelper.StartServerInTmux(server.Path, server.Filename))
            {
                return Problem("Could not start server.");
            }

            return Ok();
        }

        [HttpPost("restart")]
        public async Task<ActionResult> Restart(ServerIdRequest request, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == request.ServerId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var process = ProcessHelper.FindProcessByPort(server.Port);

            if (process is null)
            {
                return Problem("Could not find process.");
            }

            process.Kill();

            if (!ProcessHelper.StartServerInTmux(server.Path, server.Filename))
            {
                return Problem("Could not start server.");
            }

            return Ok();
        }

        [HttpPost("stop")]
        public async Task<ActionResult> Stop(ServerIdRequest request, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == request.ServerId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var process = ProcessHelper.FindProcessByPort(server.Port);

            if (process is null)
            {
                return Problem("Could not find process.");
            }

            process.Kill();

            return Ok();
        }

        [HttpPost("executeCommand")]
        public async Task<ActionResult> ExecuteCommand(ExecuteCommandRequest request, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == request.ServerId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var process = ProcessHelper.FindProcessByPort(server.Port);

            if (process is null)
            {
                return Problem("Could not find process.");
            }

            if (!ProcessHelper.SendToTmux(process.Id, request.Command))
            {
                return Problem("Could not execute command.");
            }

            return Ok();
        }

        [HttpGet("{serverId}/files/{*path}")]
        public async Task<ActionResult<List<FileDetails>>> GetFiles(Guid serverId, string? path, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            path = HttpUtility.UrlDecode(path);

            var serverFullPath = Path.GetFullPath(server.Path);
            var requestFullPath = Path.GetFullPath(Path.Combine(serverFullPath, path ?? ""));

            if (!requestFullPath.StartsWith(serverFullPath) || !Directory.Exists(requestFullPath))
            {
                return BadRequest("The requested path is not valid.");
            }

            return await FileHelper.GetAllDirectoryFileDetails(requestFullPath, ct);
        }

        [HttpPut("renameFile")]
        public async Task<ActionResult> RenameFile(RenameFileRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                return BadRequest("The new name cannot be empty.");
            }

            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == request.ServerId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var serverFullPath = Path.GetFullPath(server.Path);
            var requestFullPath = Path.GetFullPath(Path.Combine(serverFullPath, request.Path ?? ""));

            if (!requestFullPath.StartsWith(serverFullPath) || (!Directory.Exists(requestFullPath) && !System.IO.File.Exists(requestFullPath)))
            {
                return BadRequest("The requested path is not valid.");
            }

            FileHelper.RenameFileOrDirectory(requestFullPath, request.NewName);

            return Ok();
        }

        [HttpDelete("{serverId}/files/{*path}")]
        public async Task<ActionResult> DeleteFile(Guid serverId, string? path, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            path = HttpUtility.UrlDecode(path);

            var serverFullPath = Path.GetFullPath(server.Path);
            var requestFullPath = Path.GetFullPath(Path.Combine(serverFullPath, path ?? ""));

            if (!requestFullPath.StartsWith(serverFullPath) || (!Directory.Exists(requestFullPath) && !System.IO.File.Exists(requestFullPath)))
            {
                return BadRequest("The requested path is not valid.");
            }

            FileHelper.DeleteFileOrDirectory(requestFullPath);

            return Ok();
        }

        [HttpPost("{serverId}/files/{*path}")]
        public async Task<ActionResult> UploadFile(Guid serverId, string? path, IEnumerable<IFormFile>? files, CancellationToken ct)
        {
            if (files?.Any() != true)
            {
                return BadRequest("No files.");
            }

            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            path = HttpUtility.UrlDecode(path);

            var serverFullPath = Path.GetFullPath(server.Path);
            var requestFullPath = Path.GetFullPath(Path.Combine(serverFullPath, path ?? ""));

            if (!requestFullPath.StartsWith(serverFullPath) || !Directory.Exists(requestFullPath))
            {
                return BadRequest("The requested path is not valid.");
            }

            if (files.Any(x => System.IO.File.Exists(Path.Combine(requestFullPath, x.FileName))))
            {
                return Problem("The files must not already exist.");
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFullPath(Path.Combine(requestFullPath, file.FileName));

                if (!fileName.StartsWith(requestFullPath))
                {
                    return BadRequest($"The path '{file.FileName}' is not valid.");
                }

                var directory = Path.GetDirectoryName(fileName);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                using var stream = System.IO.File.Create(fileName);

                if (stream is null)
                {
                    continue;
                }

                await file.CopyToAsync(stream, ct);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("{serverId}/files/download/{token}/{*path}")]
        public async Task<ActionResult> DownloadFile(Guid serverId, string token, string? path, CancellationToken ct)
        {
            if (!TokenHelper.ValidateToken(token, _configuration.GetSection("AppSettings:Token").Value))
            {
                return Unauthorized();
            }

            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            path = HttpUtility.UrlDecode(path);

            var serverFullPath = Path.GetFullPath(server.Path);
            var requestFullPath = Path.GetFullPath(Path.Combine(serverFullPath, path ?? ""));

            if (!requestFullPath.StartsWith(serverFullPath) || (!Directory.Exists(requestFullPath) && !System.IO.File.Exists(requestFullPath)))
            {
                return BadRequest("The requested path is not valid.");
            }

            var isDirectory = FileHelper.IsDirectory(requestFullPath);
            var fileName = Path.GetFileName(requestFullPath);
            var downloadName = isDirectory ? fileName + ".zip" : fileName;

            var stream = FileHelper.GetFileOrZippedDirectory(requestFullPath, isDirectory);

            return File(stream, isDirectory ? MediaTypeNames.Application.Zip : MediaTypeNames.Application.Octet, downloadName);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateServerRequest request, CancellationToken ct)
        {
            await _dbContext.Server.AddAsync(new Database.Server
            {
                Id = Guid.NewGuid(),
                Arguments = request.Arguments,
                Filename = request.Filename,
                Name = request.Name,
                Path = request.Path,
                Port = request.Port
            }, ct);

            await _dbContext.SaveChangesAsync(ct);

            return Ok();
        }

        [HttpDelete("{serverId}")]
        public async Task<ActionResult> Delete(Guid serverId, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            _dbContext.Server.Remove(server);

            await _dbContext.SaveChangesAsync(ct);

            return Ok();
        }
    }
}
