﻿using Quartz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Surging.Core.Quartz.Schedule
{
    public interface ISurgingScheduleJobManager
    {
        Task ScheduleAsync(JobEntity jobEntity);
           
        Task ScheduleAsync(IJob job, ITrigger trigger, string jobId, string jobGroup = "default", string describe = null, IDictionary<string, object> jobData = null);

        Task PauseJob(string jobId, string groupName = null);

        Task ResumeJob(string jobId, string groupName = null);

        Task RomoveJob(string jobId, string groupName = null);

        Task Start();

        Task Stop();
    }
}
