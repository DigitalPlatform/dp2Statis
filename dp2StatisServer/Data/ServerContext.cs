using FluentScheduler;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform;

namespace dp2StatisServer.Data
{
    public static class ServerContext
    {
        static InstanceRepository? _instanceRepository = null;

        public static InstanceRepository Instances
        {
            get
            {
                if (_initialized == false)
                {
                    Initialize();
                }
                if (_instanceRepository == null)
                    throw new Exception("_instanceRepository 尚未正确初始化(理应在 Initialize() 中已经初始化)");
                return _instanceRepository;
            }
        }

        static bool _initialized = false;

        // parameters:
        //      force   是否强制初始化。false 表示不强制初始化，也就是说只会初始化一次，后面再调用不会初始化
        public static void Initialize(bool force = false)
        {
            if (_initialized == false || force == true)
            {
                if (_instanceRepository != null)
                {
                    _instanceRepository.Dispose();
                    _instanceRepository = null;
                }

                _instanceRepository = new InstanceRepository();

                _initialized = true;
                InitialJobs();
            }
        }

        // 初始化所有实例的同步任务
        public static void InitialJobs()
        {
            JobManager.Initialize();
            // var names = JobManager.AllSchedules.Select(o => o.Name).ToList();
            JobManager.RemoveAllJobs();
            foreach (var instance in Instances.GetInstances())
            {
                RefreshJob(instance);
#if REMOVED
                string jobName = "replicate" + instance.Name;
                JobManager.RemoveJob(jobName);

                if (string.IsNullOrEmpty(instance.ReplicateTime))
                    continue;

                var ret = PerdayTime.ParseTime(instance.ReplicateTime);
                if (ret.Value == -1)
                {
                    // TODO: 写入错误日志
                    continue;
                }
                JobManager.AddJob(
                    () =>
                    {
                        instance.BeginReplication(!instance.HasTaskDom, $"每日 {instance.ReplicateTime} 定时触发同步:");
                    },
                    s => s.WithName(jobName).ToRunEvery(1).Days().At(ret.Time.Hour, ret.Time.Minute)
                );
#endif
            }
        }

        public static NormalResult RefreshJob(Instance instance)
        {
            string jobName = "replicate" + instance.Name;
            JobManager.RemoveJob(jobName);

            if (string.IsNullOrEmpty(instance.ReplicateTime))
                return new NormalResult
                {
                    Value = 0,
                    ErrorCode = "emptyTime",
                    ErrorInfo = "同步时间为空"
                };

            var ret = PerdayTime.ParseTime(instance.ReplicateTime);
            if (ret.Value == -1)
            {
                // TODO: 写入错误日志
                return new NormalResult
                {
                    Value = -1,
                    ErrorCode = "invalidTime",
                    ErrorInfo = $"同步时间字符串 '{instance.ReplicateTime}' 格式错误: {ret.ErrorInfo}"
                };
            }
            JobManager.AddJob(
                () =>
                {
                    instance.BeginReplication(!instance.HasTaskDom, $"每日 {instance.ReplicateTime} 定时触发同步:");
                },
                s => s.WithName(jobName).ToRunEvery(1).Days().At(ret.Time.Hour, ret.Time.Minute)
            );

            return new NormalResult();
        }
    }
}
