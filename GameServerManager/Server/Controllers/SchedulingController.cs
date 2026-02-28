using GameServerManager.Server.Database;
using GameServerManager.Server.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace GameServerManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulingController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly ISchedulerFactory _schedulerFactory;

        public SchedulingController(DatabaseContext dbContext, ISchedulerFactory schedulerFactory)
        {
            _dbContext = dbContext;
            _schedulerFactory = schedulerFactory;
        }

        [HttpPost("{serverId}/enableBackups")]
        public async Task<ActionResult> EnableBackups(Guid serverId, CancellationToken ct)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId, ct);

            if (server is null)
            {
                return BadRequest("Server was not found.");
            }

            var job = JobBuilder.Create<BackupJob>()
                .WithIdentity(BackupJob.Key)
                .UsingJobData(BackupJob.ServerIdKey, serverId.ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("minutely")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(5)
                    .WithRepeatCount(5)
                ).Build();

            var scheduler = await _schedulerFactory.GetScheduler(ct);

            await scheduler.ScheduleJob(job, trigger, ct);

            return Ok();
        }
    }
}
