namespace NSLogger.Tests;

[SupportedOSPlatform("macos")]
public class NSLoggerTests
{
    [Fact]
    public async Task TestLogs()
    {
        var imageData = await File.ReadAllBytesAsync("/System/Library/Frameworks/Quartz.framework/Versions/A/Frameworks/ImageKit.framework/Versions/A/Resources/PreviewImage.jpg");

        using var logger = new Logger();

        logger.LogMessage("👋 Hello, world!", level: 0);
        logger.LogMessage($"First line{Environment.NewLine}Second line", level: 1);
        logger.LogMessage("With a domain", level: 2, domain: "Domain 📗 with emoji");
        var frame = new StackFrame(skipFrames: 0, needFileInfo: true);
        logger.LogMessage("With file and function", level: 3, fileName: frame.GetFileName(), lineNumber: frame.GetFileLineNumber() + 1, functionName: frame.GetMethod()?.Name);
        logger.LogMessage("With a thread name", level: 4, threadName: "Custom 🧵 name");
        await Task.Run(() => logger.LogData(imageData, level: 5));
        logger.LogImage(imageData, level: 6);
    }
}