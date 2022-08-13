using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dp2Statis.Reporting
{
    public static class DataUtility
    {
        public static DateTime GetMinValue()
        {
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }
    }
}
