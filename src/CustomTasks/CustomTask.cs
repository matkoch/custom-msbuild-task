using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CustomTasks;

public class CustomTask : ContextAwareTask
{
    [Required] public string StringParameter { get; set; }

    public ITaskItem[] FileItems { get; set; }

    [Output] public string OutputParameter { get; set; }
    [Output] public ITaskItem[] OutputItems { get; set; }

    protected override bool ExecuteInner()
    {
        string GetFile([CallerFilePath] string path = null) => path;

        BuildEngine.LogWarningEvent(
            new BuildWarningEventArgs(
                subcategory: "subcategory",
                code: "code",
                file: GetFile(),
                lineNumber: 100,
                columnNumber: 10,
                endLineNumber: 0, // ignored if 0
                endColumnNumber: 0, // ignored if 0
                message: "message",
                helpKeyword: "helpKeyword",
                senderName: "senderName"));

        OutputParameter = StringParameter;
        OutputItems = new[] { new TaskItem($"{StringParameter}.cs") }.Concat(FileItems).ToArray();

        return true;
    }
}
