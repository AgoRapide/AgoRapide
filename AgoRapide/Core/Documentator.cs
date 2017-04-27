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

        private static Dictionary<
            string,                    // Key is key like -xxx- but without -, that is like xxx
            List<EntityAndAttribute>   // 
        > Keys = new Dictionary<string, List<EntityAndAttribute>>();

        /// <summary>
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Replaces all keys on the form -xxx- like " +
            "-Username- " +
            "with complete links like " +
            "http://sample.agorapide.com/api/EnumValue/CoreP.Username/HTML")]
        public static string ReplaceKeys(string html) {
            KeyReplacementsHTML.ForEach(r => html = html.Replace(r.Key, r.Value));
            return html;
        }

        /// <summary>
        /// Complete by calling <see cref="IndexFinalize"/> afterwards. 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        [ClassMember(Description = "Not thread safe. Should be called single threaded at application startup only.")]
        public static void IndexEntity(BaseEntity entity, BaseAttribute attribute) {
            var ea = new EntityAndAttribute { Entity = entity, Attribute = attribute };
            attribute.Id.IdDoc.ForEach(id => {
                var list = Keys.TryGetValue(id, out var temp) ? temp : Keys[id] = new List<EntityAndAttribute>();
            });
        }

        public static void IndexFinalize() {
            var prefix = Util.Configuration.C.BaseUrl;
            Keys.ForEach(k => {

            });
        }

        private class EntityAndAttribute {
            /// <summary>
            /// TODO: Consider replacing with Type and Id instead.
            /// </summary>
            public BaseEntity Entity;
            public BaseAttribute Attribute;
        }
    }
}