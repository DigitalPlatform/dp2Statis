using DigitalPlatform.Text;

namespace dp2StatisServer.Data
{
    public class PerdayTime
    {
        int _hour = 0;

        public int Hour
        {
            get
            {
                return _hour;
            }
            set
            {
                if (value < 0 || value > 23)
                    throw new ArgumentException("Hour 值应在 0-23 之间");
                _hour = value;
            }
        }

        int _minute = 0;

        public int Minute
        {
            get
            {
                return _minute;
            }
            set
            {
                if (value < 0 || value > 59)
                    throw new ArgumentException("Minute 值应在 0-59 之间");

                _minute = value;
            }
        }

        public override string ToString()
        {
            return $"{Hour}:{Minute}";
        }

        public static string ToString(List<PerdayTime> times)
        {
            List<string> results = new List<string>();
            foreach (var time in times)
            {
                results.Add(time.ToString());
            }

            return StringUtil.MakePathList(results, ",");
        }

        public class ParseTimeResult : DigitalPlatform.NormalResult
        {
            public PerdayTime? Time { get; set; }
        }

        public static ParseTimeResult ParseTime(string strStartTime)
        {
            string strHour = "";
            string strMinute = "";

            int nRet = strStartTime.IndexOf(":");
            if (nRet == -1)
            {
                strHour = strStartTime.Trim();
                strMinute = "00";
            }
            else
            {
                strHour = strStartTime.Substring(0, nRet).Trim();
                strMinute = strStartTime.Substring(nRet + 1).Trim();
            }

            PerdayTime time = new PerdayTime();
            try
            {
                time.Hour = Convert.ToInt32(strHour);
                time.Minute = Convert.ToInt32(strMinute);
            }
            catch (Exception ex)
            {
                return new ParseTimeResult
                {
                    Value = -1,
                    ErrorInfo = $"时间值 '{strStartTime}' 格式(hh:mm)不合法: {ex.Message}"
                };
            }

            return new ParseTimeResult
            {
                Time = time,
            };
        }

        public static List<PerdayTime> ParseTimeRange(string range)
        {
            var parts = StringUtil.ParseTwoPart(range, "-");
            if (string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
                throw new ArgumentException($"时间范围字符串 '{range}' 不合法。横杠左右两侧不应为空");
            var result1 = ParseTime(parts[0]);
            var result2 = ParseTime(parts[1]);

            if (result1.Value == -1)
                throw new ArgumentException(result1.ErrorInfo);
            if (result2.Value == -1)
                throw new ArgumentException(result2.ErrorInfo);

            List<PerdayTime> results = new List<PerdayTime>();
            results.Add(result1.Time);
            results.Add(result2.Time);
            return results;
        }

    }

}
