using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rougamo;
using Rougamo.Context;

namespace DigitalPlatform.LibraryClientOpenApi
{
#if REMOVED
    // https://github.com/inversionhourglass/Rougamo
    public class LoginAttribute : MoAttribute
    {
        /*
        public override void OnEntry(MethodContext context)
        {
            // 从context对象中能取到包括入参、类实例、方法描述等信息
            Log.Info("方法执行前");
        }

        public override void OnException(MethodContext context)
        {
            Log.Error("方法执行异常", context.Exception);
        }
        */

        public override void OnSuccess(MethodContext context)
        {
            var result_info = context.RealReturnType.GetMembers().Where(o => o.Name.EndsWith("Result")).FirstOrDefault();

            var result = context.ReturnValue;

            context.Method.Invoke(context.Target, context.Arguments);
        }

        /*
        public override void OnExit(MethodContext context)
        {
            Log.Info("方法退出时，不论方法执行成功还是异常，都会执行");
        }
        */
    }
#endif
}
