using System;
using System.Diagnostics;

namespace com.csutil.logging {

    public abstract class LogDefaultImpl : ILog {

        /// <summary> LineBreak </summary>
        public const string LB = "\r\n";

        public void LogDebug(string msg, params object[] args) {
            PrintDebugMessage("> " + msg + Log.ToArgsStr(args, ArgToString) + LB
                + "  * at " + Log.CallingMethodStr(args) + LB + LB);
        }

        public void LogWarning(string warning, params object[] args) {
            PrintWarningMessage("> WARNING: " + warning + Log.ToArgsStr(args, ArgToString) + LB
                + "  * at " + Log.CallingMethodStr(args) + LB + LB);
        }

        public Exception LogError(string error, params object[] args) {
            PrintErrorString(">>> ERROR: " + error, args);
            return new Exception(error);
        }

        public Exception LogExeption(Exception e, params object[] args) {
            PrintException(e, args);
            return e;
        }

        private void PrintErrorString(string e, object[] args) {
            PrintErrorMessage(e + Log.ToArgsStr(args, ArgToString) + LB
                + "    * at " + Log.CallingMethodStr(args, count: 4) + LB + LB);
        }

        protected abstract void PrintDebugMessage(string debugLogMsg, params object[] args);
        protected abstract void PrintWarningMessage(string warningMsg, params object[] args);
        protected abstract void PrintErrorMessage(string errorMsg, params object[] args);

        protected virtual void PrintException(Exception e, object[] args) {
            // The default implementation prints exceptions the same as errors:
            PrintErrorString(">>> EXCEPTION: " + e, args);
        }

        protected virtual string ArgToString(object arg) {
            if (arg is StackFrame) { return null; }
            if (arg is StackTrace) { return null; }
            return "" + arg;
        }

        public virtual StopwatchV2 LogMethodEntered(string methodName, object[] args) {
#if DEBUG
            args = new StackFrame(2, true).AddTo(args);
#endif
            Log.d(" --> " + methodName, args);
            if (!methodName.IsNullOrEmpty()) { AppFlow.TrackEvent(EventConsts.catMethod, methodName, args); }
            return AssertV2.TrackTiming(methodName);
        }

        public virtual void LogMethodDone(Stopwatch timing, int maxAllowedTimeInMs, string sourceMemberName, string sourceFilePath, int sourceLineNumber) {
            var timingV2 = timing as StopwatchV2;
            string methodName = sourceMemberName;
            if (timingV2 != null) {
                timingV2.StopV2();
                methodName = timingV2.methodName;
            } else { timing.Stop(); }
            AppFlow.TrackEvent(EventConsts.catMethod, methodName + " DONE", timing);
            var text = "    <-- " + methodName + " finished after " + timing.ElapsedMilliseconds + " ms";
            if (timingV2 != null) { text += ", " + timingV2.GetAllocatedMemBetweenStartAndStop(); }
            text = $"{text} \n at {sourceFilePath}: line {sourceLineNumber}";
            Log.d(text, new StackFrame(1, true).AddTo(null));
            if (maxAllowedTimeInMs > 0) { timing.AssertUnderXms(maxAllowedTimeInMs); }
        }
    }

}