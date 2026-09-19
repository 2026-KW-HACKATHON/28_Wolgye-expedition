using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class MockTaskManager : LoggingTaskManager
    {
        public override void AddTask(TaskWrapper taskWrapper, int priorityLevel = 3)
        {
            TotalTaskCreatedCount++;
            if (EnableLogging)
            {
                Logs.Add(string.Format("{0,-10} {2,-10} {1, -30}", Time.frameCount, "added", taskWrapper.GetType().Name));
            }
            taskWrapper.Action();
            taskWrapper.Completed(Task.CompletedTask);
        }
    }
    
    public class LoggingTaskManager : TaskManager, ILogWriter
    {
        public bool EnableLogging = true;
        public int TotalTaskCreatedCount;
        public int TotalCancelledCount;
        public List<string> Logs = new List<string>();
        public List<TaskWrapper> Records = new List<TaskWrapper>();

        public int ActiveTaskCount => _runningTasks.Count;
        public int TaskQueueSize => 0; // _taskQueue.Count; //_taskQueue.Count;
        public int TasksInQueue => 0; //_taskQueue.Count;

        private int _meshGenTaskCount;
        private float _meshGenTaskTotalTime;

        public Dictionary<string, int> TaskType = new Dictionary<string, int>();

        public LoggingTaskManager()
        {
            base.TaskStarting += (t) =>
            {
                //if (EnableLogging)
                {
                    //Logs.Add(Time.frameCount + " - " + t.Info);
                    var taskType = t.GetType().Name;
                    if (!TaskType.ContainsKey(taskType))
                    {
                        TaskType.Add(taskType, 0);
                    }

                    TaskType[taskType]++;
                }
            };
        }

        public override void AddTask(TaskWrapper taskWrapper, int priorityLevel = 3)
        {
            TotalTaskCreatedCount++;
            if (EnableLogging)
            {
                Logs.Add(string.Format("{0,-10} {2,-10} {1, -30}", Time.frameCount, "added", taskWrapper.GetType().Name));
                Records.Add(taskWrapper);
            }
            base.AddTask(taskWrapper, priorityLevel);
        }

        // public override void CancelTask(TaskWrapper task)
        // {
        //     TotalCancelledCount++;
        //     base.CancelTask(task);
			     //
        // }
        //
        // public override void CancelTile(CanonicalTileId cancelledTileId)
        // {
        //     var taskCount = 0;
        //     var tileTypes = "";
        //     if (_tasksByTile.ContainsKey(cancelledTileId))
        //     {
        //         taskCount = _tasksByTile[cancelledTileId].Count;
        //         tileTypes = string.Join(" | ", _tasksByTile[cancelledTileId].Select(x => _allTasks[x].Info));
        //         TotalCancelledCount += taskCount;
        //     }
        //     if (EnableLogging)
        //     {
        //         Logs.Add(string.Format("{0,-10} {1,-15} {2,-30}; ({3}) {4}", Time.frameCount, cancelledTileId, "cancel", taskCount, tileTypes));
        //     }
        //
        //     base.CancelTile(cancelledTileId);
        // }

        public void ClearLogsAndStats()
        {
            _meshGenTaskCount = 0;
            _meshGenTaskTotalTime = 0;
            TotalTaskCreatedCount = 0;
            TotalCancelledCount = 0;
            TaskType.Clear();
            Logs.Clear();
        }

        public void ToggleLogging()
        {
            EnableLogging = !EnableLogging;
        }

        protected override void OnTaskStarting(TaskWrapper task)
        {
            base.OnTaskStarting(task);
            //Debug.Log(string.Format("{0} {1}-{2} | {3}", Time.frameCount, task.TileId, task.TilesetId, task.Info));
            if (EnableLogging)
            {
                Logs.Add(string.Format("{0,-10} {1, -30}", Time.frameCount, task.GetType().Name));
            }
        }

        protected override void OnTaskFinished(TaskWrapper task)
        {
            base.OnTaskFinished(task);
            
            if (task is MeshGenTaskWrapper<MeshGenTaskWrapperResult> meshTask)
            {
                var time = task.FinishedTime - task.StartingTime;
                _meshGenTaskCount++;
                _meshGenTaskTotalTime += time;
            }

            if (EnableLogging)
            {
                Logs.Add(string.Format("{0,-10} {1, -30}", Time.frameCount, task.GetType().Name));
            }
        }

        public void ResetStats()
        {
            ClearLogsAndStats();
        }

        public JObject DumpLogs()
        {
            var dataLog = new JObject();
            var jArray = new JArray();
            foreach (var taskWrapper in Records)
            {
                var recordData = new JObject();
                recordData["Type"] = taskWrapper.GetType().Name;
                recordData["IsCancelled"] = taskWrapper.IsCancelled;
                recordData["IsCompleted"] = taskWrapper.IsCompleted;
                recordData["TileId"] = taskWrapper.TileId.ToString();
                recordData["QueueTime"] = taskWrapper.EnqueueFrame;
                recordData["StartTime"] = taskWrapper.StartingTime;
                recordData["FinishTime"] = taskWrapper.FinishedTime;
                jArray.Add(recordData);
            }

            dataLog["TaskLogs"] = jArray;
            return dataLog;
        }

        public string PrintScreen()
        {
            var str = string.Format("Task Manager | Queued: {0}, Running: {1}, Cancelled: {2}, Total: {3}\r\n" +
                                 "Task Manager | Levels  {4}, {5}, {6}, {7}, {8}\r\n" +
                                 "Mesh Gen Task Average Time: {9}", 
                _taskQueue.Sum(x => x.Count), _runningTasks.Count, TotalCancelledCount, TotalTaskCreatedCount,
                _taskQueue[0].Count, _taskQueue[1].Count, _taskQueue[2].Count, _taskQueue[3].Count, _taskQueue[4].Count,
                _meshGenTaskTotalTime/_meshGenTaskCount);
            str += Environment.NewLine;
            str += string.Join(",", TaskType.Select(x => $"{x.Key}: {x.Value}"));
            return str;
        }
    }
}