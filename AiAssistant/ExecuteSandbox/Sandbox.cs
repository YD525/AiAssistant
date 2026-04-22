using System;
using System.Collections.Generic;

namespace AiAssistant.ExecuteSandbox
{
    // Safe execution result
    public class SafeResult
    {
        public bool Allowed;
        public string Reason;

        public static SafeResult Ok() => new SafeResult { Allowed = true };
        public static SafeResult Deny(string Reason) => new SafeResult { Allowed = false, Reason = Reason };
    }

  

    public class Sandbox
    {
        // Safe check delegate (permission gate)
        public delegate SafeResult CheckSafe(string FuncName, List<object> Args);

        public static CheckSafe CheckSafeFunc = null;

        public static SafeResult Check(string Func, params object[] Args)
        {
            if (Sandbox.CheckSafeFunc == null)
                return SafeResult.Ok();

            return Sandbox.CheckSafeFunc(Func, new List<object>(Args));
        }

        // Execute function with return value
        public static T Exec<T>(string Func, Func<T> Action, params object[] Args)
        {
            var Result = Check(Func, Args);

            if (!Result.Allowed)
                throw new UnauthorizedAccessException(
                    $"Blocked by SafeGate: {Func}, Reason: {Result.Reason}"
                );

            return Action();
        }

        // Execute function without return value
        public static void Exec(string Func, Action Action, params object[] Args)
        {
            var Result = Check(Func, Args);

            if (!Result.Allowed)
                throw new UnauthorizedAccessException(
                    $"Blocked by SafeGate: {Func}, Reason: {Result.Reason}"
                );

            Action();
        }
    }
}
