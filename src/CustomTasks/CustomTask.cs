using System;
using Microsoft.Build.Framework;

namespace CustomTasks
{
    public class CustomTask : ContextAwareTask
    {
        [Required] public string StringParameter { get; set; }

        public ITaskItem[] FilesParameter { get; set; }

        protected override bool ExecuteInner()
        {
            throw new NotImplementedException();
        }
    }
}
