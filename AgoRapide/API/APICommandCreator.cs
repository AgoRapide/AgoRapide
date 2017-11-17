// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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

        /// This has limited value, do not use (call <see cref="CreateAPICommand(CoreAPIMethod, Type, object[])"/> instead
        // public string CreateAPICommand(Type entityType, QueryId queryId) => APIMethod.GetByCoreMethodAndEntityType(CoreAPIMethod.EntityIndex, entityType).GetAPICommand(queryId);

        /// <summary>
        /// Creates API URL for <see cref="CoreAPIMethod.EntityIndex"/> for <paramref name="entityType"/> and <paramref name="id"/>  like "https://AgoRapide.com/api/Person/42/HTML"
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Uri CreateAPIUrl(Type entityType, long id) => CreateAPIUrl(CreateAPICommand(entityType, id));
        public Uri CreateAPIUrl(BaseEntity entity) => CreateAPIUrl(CreateAPICommand(entity));
        public Uri CreateAPIUrl(CoreAPIMethod coreMethod) => CreateAPIUrl(coreMethod, null);
        public Uri CreateAPIUrl(CoreAPIMethod coreMethod, Type type, params object[] parameters) => CreateAPIUrl(CreateAPICommand(coreMethod, type, parameters));
        // public string CreateAPIUrl(string apiCommand) => (!apiCommand.StartsWith(Util.Configuration.BaseUrl) ? Util.Configuration.BaseUrl : "") + apiCommand + (ResponseFormat == ResponseFormat.HTML ? Util.Configuration.HTMLPostfixIndicator : "");
        public Uri CreateAPIUrl(string apiCommand) => CreateAPIUrl(apiCommand, ResponseFormat);
        /// <summary>
        /// </summary>
        /// <param name="apiCommand">If not starts with http:// or https:// then <see cref="ConfigurationAttribute.BaseUrl"/> will be prepended</param>
        /// <param name="responseFormat"></param>
        /// <returns></returns>
        public static Uri CreateAPIUrl(string apiCommand, ResponseFormat responseFormat) => new Uri((apiCommand.StartsWith("http://") || apiCommand.StartsWith("https://") ? "" : 
            Util.Configuration.C.BaseUrl.ToString()) + apiCommand + (responseFormat == ResponseFormat.HTML ? Util.Configuration.C.HTMLPostfixIndicator : (responseFormat == ResponseFormat.CSV ? Util.Configuration.C.CSVPostfixIndicator : "")));

        /// <summary>
        /// Creates HTML API link for <see cref="CoreAPIMethod.EntityIndex"/> for <paramref name="entity"/> like {a href="https://AgoRapide.com/api/Person/42/HTML"}John Smith{/a}
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string CreateAPILink(BaseEntity entity) => CreateAPILink(entity, entity.IdFriendly);
        public string CreateAPILink(BaseEntity entity, string linkText) => CreateAPILink(CoreAPIMethod.EntityIndex, linkText, entity.GetType(), entity.IdString);
        public string CreateAPILink(CoreAPIMethod coreMethod, Type type, params object[] parameters) => CreateAPILink(coreMethod, null, null, type, parameters);
        public string CreateAPILink(CoreAPIMethod coreMethod, string linkText, Type type, params object[] parameters) => CreateAPILink(coreMethod, linkText, null, type, parameters);
        public string CreateAPILink(CoreAPIMethod coreMethod, string linkText, string helpText, Type type, params object[] parameters) {
            var apiCommand = CreateAPICommand(coreMethod, type, parameters);
            return CreateAPILink(apiCommand, linkText, helpText);
        }

        public string CreateAPILink(Uri url) => CreateAPILink(url.ToString(), url.ToString().Replace(Util.Configuration.C.BaseUrl.ToString(), ""), null);
        public string CreateAPILink(Uri url, string linkText) => CreateAPILink(url.ToString(), linkText, null);
        public string CreateAPILink(string apiCommand) => CreateAPILink(apiCommand, apiCommand, null);
        public string CreateAPILink(string apiCommand, string linkText) => CreateAPILink(apiCommand, linkText, null);
        public string CreateAPILink(string apiCommand, string linkText, string helpText) =>
            (string.IsNullOrEmpty(helpText) ? "" : "<span title=\"" + helpText.HTMLEncode() + "\">") +
            "<a href=\"" + CreateAPIUrl(apiCommand) + "\">" + (string.IsNullOrEmpty(linkText) ? apiCommand : linkText).HTMLEncode().Replace(" ","&nbsp") + "</a>" + /// Replacement of space with non-breaking space introduced 17 Nov 2017 in order to make Context-presentation by <see cref="Result.ToHTMLDetailed"/> better.
            (string.IsNullOrEmpty(helpText) ? "" : "</span>");

        public static APICommandCreator JSONInstance = new APICommandCreator(ResponseFormat.JSON);
        public static APICommandCreator HTMLInstance = new APICommandCreator(ResponseFormat.HTML);
        public static APICommandCreator CSVInstance = new APICommandCreator(ResponseFormat.CSV);
    }
}