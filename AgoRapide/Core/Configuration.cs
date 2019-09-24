// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using System.Web.Http;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide.Core {

    [Class(AccessLevelRead = AccessLevel.Admin, AccessLevelWrite = AccessLevel.System)]
    public class Configuration : ApplicationPart {

        public ConfigurationAttribute _c { get; private set; }
        public ConfigurationAttribute C {
            get => _c ?? throw new NullReferenceException(nameof(C));
            set => _c = value ?? throw new NullReferenceException(nameof(value));
        }

        /// <summary>
        /// Dummy constructor for use by <see cref="BaseDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public Configuration() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        /// <summary>
        /// Note special approach for <see cref="Configuration"/>, <see cref="BaseDatabase"/> is not a constructor parameter as with other <see cref="ApplicationPart"/>
        /// </summary>
        /// <param name="configurationAttribute"></param>
        public Configuration(ConfigurationAttribute configurationAttribute) :base(configurationAttribute) => C = configurationAttribute;
        protected override void ConnectWithDatabase(BaseDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
        /// <summary>
        /// Note special approach for <see cref="Configuration"/>, <see cref="BaseDatabase"/> is not a constructor parameter as with other <see cref="ApplicationPart"/>
        /// </summary>
        /// <param name="db"></param>
        public void ConnectWithDatabasePublicAccess(BaseDatabase db) => ConnectWithDatabase(db);
    }
}