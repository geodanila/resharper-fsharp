using JetBrains.Util;
using JetBrains.Util.Logging;

namespace FSharp.ExternalFormatter.Protocol
{
  public static class FantomasProtocolConstants
  {
    public const string PROCESS_FILENAME = "JetBrains.ReSharper.Plugins.FSharp.ExternalFormatter.Host.exe";
    public const string PARENT_PROCESS_PID_ENV_VARIABLE = "FSHARP_EXTERNAL_FORMATTER_PROCESS_PID";
    public static readonly FileSystemPath LogFolder = Logger.LogFolderPath.Combine("ExternalFormatter");
  }
}