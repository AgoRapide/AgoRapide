// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide.Core {

    [Class(Description = "Keeps track of all keys on the form -xxx- and how they may be queried")]
    public static class Documentator {

        private static Dictionary<
            string, // Key is complete key on the form -xxx-
            string  // Value is complete URL for replacement like http://sample.agorapide.com/api/EnumValue/CoreP.Username/HTML
        > KeyReplacementsHTML = new Dictionary<string, string>();

        /// <summary>
        /// TODO: Consider making private
        /// </summary>
        public static Dictionary<
            string,                    // Key is key like -xxx- but without -, that is like xxx
            List<EntityAndAttribute>   // 
        > Keys = new Dictionary<string, List<EntityAndAttribute>>();

        /// <summary>
        /// Note that method is performance intensive and result should therefore be cached
        /// 
        /// TODO: Consider adding bool parameter called "cache" for ourself to implement caching
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Replaces all keys on the form -xxx- like " +
            "-Username- " +
            "with complete links like " +
            "http://sample.agorapide.com/api/EnumValue/CoreP.Username/HTML"
        )]
        public static string ReplaceKeys(string html) {
            KeyReplacementsHTML.ForEach(r => html = html.Replace(r.Key, r.Value));
            return html;
        }

        /// <summary>
        /// TODO: Make more logical choice of methdod names and placing of logic
        /// </summary>
        /// <param name="db"></param>
        public static void IndexKnowEntities(BaseDatabase db) {
            APIMethod.AllMethods.ForEach(m => IndexEntity(m));
            IndexEntity(Util.Configuration); // Is 
            Enum.RegisterAndIndexCoreEnum(db);
            Class.RegisterAndIndexCoreClass(db);
        }

        public static void IndexEntity(ApplicationPart applicationPart) => IndexEntity(applicationPart, applicationPart.A);
        /// <summary>
        /// Complete by calling <see cref="IndexFinalize"/> afterwards. 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        public static void IndexEntity(BaseEntity entity, BaseAttribute attribute) {
            Util.AssertCurrentlyStartingUp();
            var ea = new EntityAndAttribute { Entity = entity, Attribute = attribute };
            attribute.Id.IdDoc.ForEach(id => {
                var list = Keys.TryGetValue(id, out var temp) ? temp : Keys[id] = new List<EntityAndAttribute>();
                list.Add(ea);
            });
        }

        private static APICommandCreator api = APICommandCreator.HTMLInstance;
        /// <summary>
        /// Returns <see cref="KeyReplacementsHTML"/>
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> IndexFinalize() {
            Util.AssertCurrentlyStartingUp();
            KeyReplacementsHTML = new Dictionary<string, string>();
            Keys.ForEach(k => KeyReplacementsHTML["-" + k.Key + "-"] = GetSingleReplacement(k.Key, k.Value));
            return KeyReplacementsHTML;
        }

        public static string GetSingleReplacement(string key, List<EntityAndAttribute> list) {
            var types = list.Select(l => l.Entity.GetType()).Distinct().ToList();
            switch (types.Count) {
                case 0: throw new InvalidCountException(nameof(list) + ". Expected at least 1 item in list");
                case 1: return api.CreateAPILink(CoreAPIMethod.EntityIndex, key, types[0], key);  // Use specific api-method like api/EnumValue for instance
                default: return api.CreateAPILink(CoreAPIMethod.EntityIndex, key, typeof(BaseEntity), key); // Use generic api-method like api/Entity since result will have different types
            }
        }

        /// <summary>
        /// TODO: Consider making private
        /// </summary>
        public class EntityAndAttribute {
            /// <summary>
            /// TODO: Consider replacing with Type and Id instead.
            /// </summary>
            public BaseEntity Entity;
            public BaseAttribute Attribute;
        }
    }
}