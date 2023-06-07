using System.Text;
using NLog;
using NLog.Targets;

namespace Jackett.Common.Utils.Logging
{
    public class CleanseFileTarget : FileTarget
    {
        protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
        {
            var result = CleanseLogMessage.Cleanse(Layout.Render(logEvent));
            target.Append(result);
        }
    }
}
