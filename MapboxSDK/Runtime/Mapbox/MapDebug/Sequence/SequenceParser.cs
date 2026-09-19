#if UNITY_RECORDER && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Sequence
{
    public class SequenceParser
    {
        private Dictionary<string, Type> _typeParsers = new Dictionary<string, Type>()
        {
            {"wait", typeof(WaitSequenceCommand)},
            {"SetCamera", typeof(SetCameraSequenceCommand)}
        };
    
        public List<SequenceCommand> ParseSequence(JObject parsedJSON)
        {
            var commands = new List<SequenceCommand>();
            var version = parsedJSON["version"];
            var sequence = parsedJSON["sequence"];
            var packedCommand = new SetCameraSequenceCommand();
            foreach (var commandToken in sequence)
            {
                var commandName = commandToken["Name"].Value<string>();
                if (_typeParsers.TryGetValue(commandName, out var type))
                {
                    if (commandName == "wait")
                    {
                        commands.Add(packedCommand);
                        packedCommand = new SetCameraSequenceCommand();

                        var command = commandToken.ToObject<WaitSequenceCommand>();
                        commands.Add(command);
                    }
                    else if(commandName == "SetCamera")
                    {
                        var command = commandToken.ToObject<SetCameraSequenceCommand>();
                        if (command.center.HasValue)
                            packedCommand.center = command.center.Value;
                        if (command.bearing.HasValue)
                            packedCommand.bearing = command.bearing.Value;
                        if (command.zoom.HasValue)
                            packedCommand.zoom = command.zoom.Value;
                        if (command.pitch.HasValue)
                            packedCommand.pitch = command.pitch.Value;
                    }
                }
            }
        
            commands.Add(packedCommand);
        
            return commands;
        }
    }

    public abstract class SequenceCommand
    {
        public abstract string Name { get; }
        public abstract IEnumerator Run(MapboxMap map, float speed);
    }

    public class WaitSequenceCommand : SequenceCommand
    {
        public override string Name => "wait";
        public float duration;

        public override IEnumerator Run(MapboxMap map, float speed)
        {
            yield return new WaitForSeconds(duration * speed);
        }
    }

    public class SetCameraSequenceCommand : SequenceCommand
    {
        public override string Name => "SetCamera";
        public LatitudeLongitude? center;
        public float? zoom;
        public float? pitch;
        public float? bearing;
        public float? scale;

        public override IEnumerator Run(MapboxMap map, float speed)
        {
            map.ChangeView(center, zoom, pitch, bearing);
            yield break;
        }
    }
}
#endif