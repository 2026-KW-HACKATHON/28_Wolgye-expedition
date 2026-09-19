using System.Runtime.CompilerServices;
using Mapbox.VectorTile.Contants;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public static class DecodeGeometry
    {
        public static FeatureVertexData GetGeometry(uint[] geometryCommands, Vector3 scale)
        {
            var vertexData = new FeatureVertexData();
            vertexData.Vertices = new Vector3[geometryCommands.Length / 2]; //ArrayPool<Vector3>.Shared.Rent(geometryCommands.Length / 2);
			
            int geomCmdCnt = geometryCommands.Length;
            float cursorX = 0;
            float cursorY = 0;
            var index = 0;
            for (int i = 0; i < geomCmdCnt; i++)
            {
                uint g = geometryCommands[i];
                Commands cmd = (Commands)(g & 0x7);
                uint cmdCount = g >> 3;

                if (cmd == Commands.MoveTo || cmd == Commands.LineTo)
                {
                    for (int j = 0; j < cmdCount; j++)
                    {
                        // ZigzagDecode(geometryCommands[i + 1], geometryCommands[i + 2], out var dx, out var dy);
                        // cursorX += (dx / scale.x);
                        // cursorY += (dy / scale.z);
                        //small micro optimization to push changes into method below to prevent new allocation
                        ModifyWithZigzagDecode(geometryCommands[i + 1], geometryCommands[i + 2], ref scale.x, ref scale.z, ref cursorX, ref cursorY);
                        
                        i += 2;
                        //end of part of multipart feature
                        if (cmd == Commands.MoveTo)
                        {
                            vertexData.Submeshes.Add(index);
                        }

                        var pntTmp = new Vector3(cursorX, 0, cursorY);
                        // {
                        //     x = cursorX * scaleOffset[0] + scaleOffset[2],
                        //     y = 0,
                        //     z = cursorY * scaleOffset[1] - scaleOffset[3]
                        // };
                        vertexData.Vertices[index++] = pntTmp;
                    }
                }
            }

            vertexData.Submeshes.Add(index);
            vertexData.VertexCount = index;
            return vertexData;
        }
        
        public static FeatureVertexData GetGeometry(uint[] geometryCommands, Vector3 scale, Vector4 scaleOffset)
        {
            var vertexData = new FeatureVertexData();
            vertexData.Vertices = new Vector3[geometryCommands.Length / 2]; //ArrayPool<Vector3>.Shared.Rent(geometryCommands.Length / 2);
			
            int geomCmdCnt = geometryCommands.Length;
            float cursorX = 0;
            float cursorY = 0;
            var index = 0;
            for (int i = 0; i < geomCmdCnt; i++)
            {
                uint g = geometryCommands[i];
                Commands cmd = (Commands)(g & 0x7);
                uint cmdCount = g >> 3;

                if (cmd == Commands.MoveTo || cmd == Commands.LineTo)
                {
                    for (int j = 0; j < cmdCount; j++)
                    {
                        // ZigzagDecode(geometryCommands[i + 1], geometryCommands[i + 2], out var dx, out var dy);
                        // cursorX += (dx / scale.x);
                        // cursorY += (dy / scale.z);
                        //small micro optimization to push changes into method below to prevent new allocation
                        ModifyWithZigzagDecode(geometryCommands[i + 1], geometryCommands[i + 2], ref scale.x, ref scale.z, ref cursorX, ref cursorY);
                        i += 2;
                        //end of part of multipart feature
                        if (cmd == Commands.MoveTo)
                        {
                            vertexData.Submeshes.Add(index);
                        }

                        var pntTmp = new Vector3()
                        {
                            x = cursorX * scaleOffset[0] + scaleOffset[2],
                            y = 0,
                            z = cursorY * scaleOffset[1] - scaleOffset[3]
                        };
                        vertexData.Vertices[index++] = pntTmp;
                    }
                }
            }

            vertexData.Submeshes.Add(index);
            vertexData.VertexCount = index;
            return vertexData;
        }
		
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZigzagDecode(uint x, uint y, out float dx, out float dy)
        {
            dx = (x >> 1) ^ (-(int)(x & 1));
            dy = (y >> 1) ^ (-(int)(y & 1));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ModifyWithZigzagDecode(uint x, uint y, ref float scaleX, ref float scaleZ, ref float cursorX, ref float cursorY)
        {
            cursorX += (((x >> 1) ^ (-(int)(x & 1))) / scaleX);
            cursorY += (((y >> 1) ^ (-(int)(y & 1))) / scaleZ);
        }
        
        private static Vector2 ZigzagDecode(uint x, uint y)
        {

            //TODO: verify speed improvements using
            // new Point2d(){X=x, Y=y} instead of
            // new Point3d(x, y);

            //return new Point2d(
            //    ((x >> 1) ^ (-(x & 1))),
            //    ((y >> 1) ^ (-(y & 1)))
            //);
            return new Vector2
            {
                x = ((x >> 1) ^ (-(x & 1))),
                y = ((y >> 1) ^ (-(y & 1)))
            };
        }
    }
}