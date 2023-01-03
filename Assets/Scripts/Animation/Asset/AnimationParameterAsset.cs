using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Tinder.Animation.Asset
{
    public interface AnimationParameterBlob
    {
        int HashValue { get; }
        string ParameterName { get; }
    }

    public struct BoolParameterBlob : AnimationParameterBlob
    {
        public int HashValue => ParameterName.GetHashCode();
        public string ParameterName => Name.Value;

        public FixedString64Bytes Name;
        public bool DefaultValue;
    }

    public struct IntParameterBlob : AnimationParameterBlob
    {
        public int HashValue => ParameterName.GetHashCode();
        public string ParameterName => Name.Value;

        public FixedString64Bytes Name;
        public int DefaultValue;
    }

    public struct FloatParameterBlob : AnimationParameterBlob
    {
        public int HashValue => ParameterName.GetHashCode();
        public string ParameterName => Name.Value;

        public FixedString64Bytes Name;
        public float DefaultValue;
    }
}
