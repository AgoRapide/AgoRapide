using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide;

namespace AgoRapide.API {

    public class APICommandCreator {

        public ResponseFormat ResponseFormat { get; private set; }
        private APICommandCreator(ResponseFormat responseFormat) => ResponseFormat = responseFormat;
        
        /// <summary>
        /// Creates API command for <see cref="CoreAPIMethod.EntityIndex"/> for <paramref name="entityType"/> and <paramref name="id"/> like "Person/42"
        /// 
        /// Note that could in principle be made static
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public string CreateAPICommand(Type entityType, long id) => CreateAPICommand(CoreAPIMethod.EntityIndex, entityType, new QueryIdInteger(id));
        /// <summary>
        /// Note that could in principle be made static
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string CreateAPICommand(BaseEntity entity) => CreateAPICommand(entity.GetType(), entity.Id);
        /// <summary>
        /// Note that could in principle be made static
        /// </summary>
        /// <param name="coreMethod"></param>
        /// <param name="type">May be null, for instance for <see cref="CoreAPIMethod.ExceptionDetails"/></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public string CreateAPICommand(CoreAPIMethod coreMethod, Type type, params object[] parameters) => APIMethod.GetByCoreMethodAndEntityType(coreMethod, type).GetAPICommand(parameters);

        /// <summary>
        /// Creates API URL for <see cref="CoreAPIMethod.EntityIndex"/> for <paramref name="entityType"/> and <paramref name="id"/>  like "https://AgoRapide.com/api/Person/42/HTML"
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public string CreateAPIUrl(Type entityType, long id) => CreateAPIUrl(CreateAPICommand(entityType, id));
        public string CreateAPIUrl(BaseEntity entity) => CreateAPIUrl(CreateAPICommand(entity));
        public string CreateAPIUrl(CoreAPIMethod coreMethod) => CreateAPIUrl(coreMethod, null);
        public string CreateAPIUrl(CoreAPIMethod coreMethod, Type type, params object[] parameters) => CreateAPIUrl(CreateAPICommand(coreMethod, type, parameters));
        // public string CreateAPIUrl(string apiCommand) => (!apiCommand.StartsWith(Util.Configuration.BaseUrl) ? Util.Configuration.BaseUrl : "") + apiCommand + (ResponseFormat == ResponseFormat.HTML ? Util.Configuration.HTMLPostfixIndicator : "");
        public string CreateAPIUrl(string apiCommand) => CreateAPIUrl(apiCommand, ResponseFormat);
        public static string CreateAPIUrl(string apiCommand, ResponseFormat responseFormat) => Util.Configuration.C.BaseUrl + apiCommand + (responseFormat == ResponseFormat.HTML ? Util.Configuration.C.HTMLPostfixIndicator : "");

        /// <summary>
        /// Creates API link for <see cref="CoreAPIMethod.EntityIndex"/> for <paramref name="entity"/> like {a href="https://AgoRapide.com/api/Person/42/HTML"}John Smith{/a}
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string CreateAPILink(BaseEntity entity) => CreateAPILink(entity, entity.IdFriendly);
        public string CreateAPILink(BaseEntity entity, string linkText) =>
            CreateAPILink(CoreAPIMethod.EntityIndex, linkText, entity.GetType(), entity.IdString);
            //    (entity.Properties != null && entity.Properties.TryGetValue(CoreP.IdString, out var p) ?
            //        (QueryId)new QueryIdString(p.V<string>()) : /// Using identifier looks much better in links. Especially good for documentation where names stay the same but id's may change  (like "Property/{QueryId}" to identify an <see cref="APIMethod"/> for instance)
            //        (QueryId)new QueryIdInteger(entity.Id)
            //    )
            //);

        public string CreateAPILink(CoreAPIMethod coreMethod, Type type, params object[] parameters) => CreateAPILink(coreMethod, null, null, type, parameters);
        public string CreateAPILink(CoreAPIMethod coreMethod, string linkText, Type type, params object[] parameters) => CreateAPILink(coreMethod, linkText, null, type, parameters);
        public string CreateAPILink(CoreAPIMethod coreMethod, string linkText, string helpText, Type type, params object[] parameters) {
            var apiCommand = CreateAPICommand(coreMethod, type, parameters);
            return CreateAPILink(apiCommand, linkText, helpText);
        }

        public string CreateAPILink(string apiCommand) => CreateAPILink(apiCommand, apiCommand, null);
        public string CreateAPILink(string apiCommand, string linkText) => CreateAPILink(apiCommand, linkText, null);
        public string CreateAPILink(string apiCommand, string linkText, string helpText) =>
            (string.IsNullOrEmpty(helpText) ? "" : "<span title=\"" + helpText.HTMLEncode() + "\">") +
            "<a href=\"" + CreateAPIUrl(apiCommand) + "\">" + (string.IsNullOrEmpty(linkText) ? apiCommand : linkText).HTMLEncode() + "</a>" +
            (string.IsNullOrEmpty(helpText) ? "" : "</span>");

        public static APICommandCreator JSONInstance = new APICommandCreator(ResponseFormat.JSON);
        public static APICommandCreator HTMLInstance = new APICommandCreator(ResponseFormat.HTML);
    }
}