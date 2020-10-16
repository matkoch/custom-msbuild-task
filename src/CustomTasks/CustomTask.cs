using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CustomTasks
{
    public class CustomTask : ContextAwareTask
    {
        [Required] public string StringParameter { get; set; }

        public ITaskItem[] FileItems { get; set; }

        [Output] public string OutputParameter { get; set; }
        [Output] public ITaskItem[] OutputItems { get; set; }

        protected override bool ExecuteInner()
        {
            OutputParameter = StringParameter;
            OutputItems = new[] {new TaskItem($"{StringParameter}.cs")}.Concat(FileItems).ToArray();
            return true;
        }
    }
}
