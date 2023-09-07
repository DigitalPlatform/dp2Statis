using System.Xml;

using DigitalPlatform;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.Xml;

namespace dp2StatisServer.Data
{
    public interface IInstanceRepository
    {
        GlobalInfo? GetGlobalInfo();

        Instance? CreateInstance(string name,
            string description,
            string replicateTime,
            string appServerUrl,
            string appServerUserName,
            string appServerPassword,
    string adminPassword,
    string instanceUserPassword);

        IEnumerable<Instance> GetInstances();

        Instance? FindInstance(string name);

        void SetInstance(Instance instance);

        void DeleteInstance(string name, string? adminPassword);
    }


    public class GlobalInfo
    {
        // 数据目录根目录
        public string DataDirRoot { get; set; }

        public GlobalInfo(string dataDir)
        {
            DataDirRoot = dataDir;
        }
    }
}
