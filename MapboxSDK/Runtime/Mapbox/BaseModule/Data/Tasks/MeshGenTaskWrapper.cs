using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mapbox.BaseModule.Data.Tasks
{
    public class MeshGenTaskWrapper<T> : TaskWrapper where T : MeshGenTaskWrapperResult, new()
    {
        public T DataResult;
        public Func<T> DataAction;
        public Action<Task, T> DataCompleted;

        public override void Action()
        {
            DataResult = DataAction();
        }

        public override void Completed(Task task)
        {
            if (task != null && !task.IsFaulted)
            {
                DataCompleted(Task.CompletedTask, DataResult);
                IsCompleted = true;
                return;
            }

            if (task == null)
            {
                DataResult ??= new T();
                DataResult.ResultType = TaskResultType.Cancelled;
                DataCompleted(null, DataResult);
            }
            else
            {
                DataResult ??= new T();
                DataResult.ResultType = TaskResultType.MeshGenerationFailure;
                DataCompleted(task, DataResult);
            }
            IsCompleted = true;
        }
    }
    
    public class MeshGenTaskWrapperResult : TaskResult
    {
        public TaskResultType ResultType;
        public Dictionary<string, Dictionary<int, HashSet<MeshData>>> Data;
		
		
        public MeshGenTaskWrapperResult()
        {
            Data = new Dictionary<string, Dictionary<int, HashSet<MeshData>>>();
        }
    }
	
    public enum TaskResultType
    {
        DataProcessingFailure,
        MeshGenerationFailure,
        GameObjectFailure,
        Success,
        Cancelled
    }
}