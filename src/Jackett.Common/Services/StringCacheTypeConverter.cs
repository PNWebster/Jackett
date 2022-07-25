using System;
using Jackett.Common.Utils;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Jackett.Common.Services
{
    internal class StringCacheTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(string);
        public object ReadYaml(IParser parser, Type type) => parser.Consume<Scalar>().Value.ToSystemReferencedString();
        public void WriteYaml(IEmitter emitter, object value, Type type) => throw new NotImplementedException();
    }
}
