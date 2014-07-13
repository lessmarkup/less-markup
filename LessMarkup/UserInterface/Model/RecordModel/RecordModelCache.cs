/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.RecordModel
{
    public class RecordModelCache : ICacheHandler
    {
        private readonly Dictionary<Type, RecordModelDefinition> _definitions = new Dictionary<Type, RecordModelDefinition>();
        private readonly Dictionary<string, RecordModelDefinition> _idToDefinition = new Dictionary<string, RecordModelDefinition>();
        private readonly Dictionary<Type, string> _typeToId = new Dictionary<Type, string>();
        private readonly IModuleProvider _moduleProvider;

        public RecordModelCache(IModuleProvider moduleProvider)
        {
            _moduleProvider = moduleProvider;
        }

        public RecordModelDefinition GetDefinition<T>()
        {
            return GetDefinition(typeof (T));
        }

        public RecordModelDefinition GetDefinition(Type type)
        {
            RecordModelDefinition ret;
            if (!_definitions.TryGetValue(type, out ret))
            {
                return null;
            }
            return ret;
        }

        public RecordModelDefinition GetDefinition(string id)
        {
            RecordModelDefinition ret;
            if (!_idToDefinition.TryGetValue(id, out ret))
            {
                return null;
            }
            return ret;
        }

        public bool HasDefinition(Type type)
        {
            return _definitions.ContainsKey(type);
        }

        private static string CreateDefinitionId(RecordModelDefinition definition, int index, HashAlgorithm hashAlgorithm)
        {
            var idString = definition.DataType.AssemblyQualifiedName + index;

            var bytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(idString));

            idString = Convert.ToBase64String(bytes, 0, 7, Base64FormattingOptions.None);

            return idString;
        }

        public void Initialize(long? siteId, out DateTime? expirationTime, long? objectId = null)
        {
            using (var hashAlgorithm = HashAlgorithm.Create("SHA512"))
            {
                foreach (var module in _moduleProvider.Modules.Where(m => m.Initializer != null))
                {
                    var modelTypes = module.Initializer.ModelTypes;
                    if (modelTypes == null)
                    {
                        continue;
                    }
                    foreach (var type in modelTypes)
                    {
                        var recordModelAttribute = type.GetCustomAttribute<RecordModelAttribute>();
                        if (recordModelAttribute == null)
                        {
                            continue;
                        }

                        var definition = new RecordModelDefinition();
                        definition.Initialize(type, recordModelAttribute, module.ModuleType);

                        _definitions.Add(type, definition);

                        string definitionId;

                        for (int i = 0;; i++)
                        {
                            definitionId = CreateDefinitionId(definition, i, hashAlgorithm);
                            if (!_idToDefinition.ContainsKey(definitionId))
                            {
                                break;
                            }
                        }

                        definition.Id = definitionId;
                        _idToDefinition.Add(definitionId, definition);
                        _typeToId.Add(type, definitionId);
                    }
                }
            }

            expirationTime = null;
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return false;
        }

        private readonly EntityType[] _entities = new EntityType[0];

        public EntityType[] HandledTypes { get { return _entities; } }
    }
}
