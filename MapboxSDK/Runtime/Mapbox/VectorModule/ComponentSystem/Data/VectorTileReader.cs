using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mapbox.VectorTile.Contants;
using UnityEngine;
using ValueType = Mapbox.VectorTile.Contants.ValueType;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public ref struct VectorTileReader
    {
        private Dictionary<string, Vector2Int> _layerViews;
        private readonly ReadOnlySpan<byte> _data;

        public VectorTileReader(ref ReadOnlySpan<byte> data)
        {
            _data = data;
            _layerViews = new Dictionary<string, Vector2Int>();
            if (data.IsEmpty)
            {
                throw new System.Exception("Tile data cannot be empty");
            }
            if (data[0] == 0x1f && data[1] == 0x8b)
            {
                throw new System.Exception("Tile data is zipped");
            }
            
            layers(data);
        }

        private void layers(ReadOnlySpan<byte> data)
        {
            PbfReader tileReader = new PbfReader(data);
            while (tileReader.NextByte())
            {
                if (tileReader.Tag == (int)TileType.Layers)
                {
                    string name = null;
                    var view = tileReader.View();
                    PbfReader layerView = new PbfReader(_data.Slice(view.x, view.y));
                    while (layerView.NextByte())
                    {
                        if (layerView.Tag == (int)LayerType.Name)
                        {
                            var strLen = layerView.Varint();
                            name = layerView.GetString((int)strLen);
                        }
                        else
                        {
                            layerView.Skip();
                        }
                    }
                    if (name == null)
                    {
                        Debug.LogWarning("Skipping PBF layer with no name tag");
                        continue;
                    }
                    _layerViews.Add(name, view);
                }
                else
                {
                    tileReader.Skip();
                }
            }
        }

        public bool TryGetLayer(string layerName, out VectorTileLayer layer)
        {
            if (_layerViews.TryGetValue(layerName, out var layerData))
            {
                layer = getLayer(_data.Slice(layerData.x, layerData.y));
                return true;
            }
            else
            {
                layer = new VectorTileLayer();
                return false;
            }
        }
        
        private VectorTileLayer getLayer(ReadOnlySpan<byte> layerData)
        {
            var layer = new VectorTileLayer(ref layerData);
            var layerReader = new PbfReader(layer.Data);
            while (layerReader.NextByte())
            {
                int layerType = layerReader.Tag;
                switch ((LayerType)layerType)
                {
                    case LayerType.Version:
                        var version = layerReader.Varint();
                        layer.Version = version;
                        break;
                    case LayerType.Name:
                        var strLength = layerReader.Varint();
                        layer.Name = layerReader.GetString((int)strLength);
                        break;
                    case LayerType.Extent:
                        layer.Extent = (ulong)layerReader.Varint();
                        break;
                    case LayerType.Keys:
                        var keyBuffer = layerReader.View();
                        string key = Encoding.UTF8.GetString(layerData.Slice(keyBuffer.x, keyBuffer.y));
                        layer.Keys.Add(key);
                        break;
                    case LayerType.Values:
                        var valueBuffer = layerReader.View();
                        var valuesBuffer = layerData.Slice(valueBuffer.x, valueBuffer.y);
                        var valReader = new PbfReader(valuesBuffer);
                        while (valReader.NextByte())
                        {
                            switch ((ValueType)valReader.Tag)
                            {
                                case ValueType.String:
                                    var stringBuffer = valReader.View();
                                    string value = Encoding.UTF8.GetString(valuesBuffer.Slice(stringBuffer.x, stringBuffer.y));
                                    layer.Values.Add(value);
                                    break;
                                case ValueType.Float:
                                    float snglVal = valReader.GetFloat();
                                    layer.Values.Add(snglVal);
                                    break;
                                case ValueType.Double:
                                    double dblVal = valReader.GetDouble();
                                    layer.Values.Add(dblVal);
                                    break;
                                case ValueType.Int:
                                    long i64 = valReader.Varint();
                                    layer.Values.Add(i64);
                                    break;
                                case ValueType.UInt:
                                    long u64 = valReader.Varint();
                                    layer.Values.Add(u64);
                                    break;
                                case ValueType.SInt:
                                    long s64 = valReader.Varint();
                                    long decoded = (long)((ulong)s64 >> 1) ^ -(s64 & 1);
                                    layer.Values.Add(decoded);
                                    break;
                                case ValueType.Bool:
                                    long b = valReader.Varint();
                                    layer.Values.Add(b == 1);
                                    break;
                                default:
                                    throw new System.Exception(string.Format(
                                        NumberFormatInfo.InvariantInfo
                                        , "NOT IMPLEMENTED valueReader.Tag:{0} valueReader.WireType:{1}"
                                        , valReader.Tag
                                        , valReader.WireType
                                    ));
                                //uncomment the following lines when not throwing!!
                                //valReader.Skip();
                                //break;
                            }
                        }
                        break;
                    case LayerType.Features:
                        layer.AddFeatureData(layerReader.View());
                        break;
                    default:
                        layerReader.Skip();
                        break;
                }
            }
            return layer;
        }
    }
}