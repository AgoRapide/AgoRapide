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

        public static void IndexKnowEntities(IDatabase db) {
            APIMethod.AllMethods.ForEach(m => IndexEntity(m, m.A));
            IndexEntity(Util.Configuration, Util.Configuration.A);
            // EnumMapper.AllCoreP.ForEach(p => IndexEntity(ApplicationPart.Get))
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
                list.Add(ea);
            });
        }

        [ClassMember(Description = "Not thread safe. Should be called single threaded at application startup only.")]
        public static void IndexFinalize() {
            KeyReplacementsHTML = new Dictionary<string, string>();
            var api = APICommandCreator.HTMLInstance;
            Keys.ForEach(k => {
                var list = k.Value;
                switch (list.Count) {
                    case 0: throw new InvalidCountException(nameof(list) + ". Expected at least 1 item in list");
                    case 1: KeyReplacementsHTML[k.Key] = api.CreateAPILink(CoreAPIMethod.EntityIndex, list[0].Entity.GetType(), k.Key); break;
                    default:
                        throw new NotImplementedException(); // TODO: Check for different types of entity
                }
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