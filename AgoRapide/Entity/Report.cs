﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide {

    [Class(Description =
        "Report is a continuation of the -" + nameof(Context) + "-concept.\r\n" +
        "The class enables storing of -" + nameof(CoreP.Context) + "- for a user, with further detailed specification for exactly what kind of data to retrieve.",
        AccessLevelRead = AccessLevel.Relation,
        AccessLevelWrite = AccessLevel.Relation
    )]
    public class Report : APIDataObject {

        /// Merged into <see cref="ShowResult"/> 29 Nov 2017.
        ///// <summary>
        ///// <see cref="CoreAPIMethod.BaseEntityMethod"/>. 
        ///// </summary>
        ///// <param name="db"></param>
        ///// <param name="request"></param>
        //[APIMethod(
        //    Description = "Sets -" + nameof(CoreP.Context) + "- for the current user to -" + nameof(CoreP.Context) + "- for report as identified by {QueryId}.",
        //    S1 = nameof(Use), S2 = "DUMMY", // TODO: REMOVE "DUMMY". Added Summer 2017 because of bug in routing mechanism.
        //    AccessLevelUse = AccessLevel.Relation,
        //    ShowDetailedResult = true)]
        //public object Use(BaseDatabase db, ValidRequest request) {
        //    var properties = new List<(PropertyKeyWithIndex, object)>();
        //    if (!Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var _new)) { // Note how we assume that Context will always contain at least one property once defined.k
        //        return request.GetErrorResponse(ResultCode.data_error, "No " + nameof(CoreP.Context) + " properties found for this report");
        //    }
        //    if (request.CurrentUser.Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var old)) {
        //        old.Properties.ForEach(p => db.OperateOnProperty(request.CurrentUser.Id, p.Value, PropertyOperation.SetInvalid, null));
        //    }

        //    // Naïve approach. Will only set one context-value.
        //    //c.Properties.ForEach(p => db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, 
        //    //    // NOTE: No need for specifying PropertyKeyWithIndex here. Most probably it would only result in an obscure exception anyway.
        //    //    // p.Value.Key.PropertyKeyWithIndex,
        //    //    p.Value.Key,
        //    //    p.Value.V<Context>()));

        //    db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, CoreP.Context.A(), _new.V<List<Context>>());

        //    request.Result.ResultCode = ResultCode.ok;
        //    request.Result.AddProperty(CoreP.SuggestedUrl.A(), request.API.CreateAPIUrl(CoreAPIMethod.Context));
        //    // request.Result.AddProperty(CoreP.Message.A(), "xxx"); // Probably unnecessary
        //    return request.GetResponse();
        //}

        /// <summary>
        /// <see cref="CoreAPIMethod.BaseEntityMethod"/>. 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="request"></param>
        [APIMethod(
            Description = "Show result of report as identified by {QueryId}. Depends on -" + nameof(ReportP.ReportEntityType) + "- (and optionally -" + nameof(ReportP.ReportQueryIdFieldIterator) + "-).",
            S1 = nameof(ShowResult), S2 = "DUMMY", // TODO: REMOVE "DUMMY". Added Summer 2017 because of bug in routing mechanism.
            AccessLevelUse = AccessLevel.Relation,
            ShowDetailedResult = true)]
        public object ShowResult(BaseDatabase db, ValidRequest request) {

            /// ====================================================
            /// Step 1, set context to the context of the report
            /// ====================================================
            /// (this will simplify understanding of system)
            /// Note that step 1 and step 2 are really independent of each other.
            var properties = new List<(PropertyKeyWithIndex, object)>();
            if (!Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var _new)) { // Note how we assume that Context will always contain at least one property once defined.k
                return request.GetErrorResponse(ResultCode.data_error, "No " + nameof(CoreP.Context) + " properties found for this report");
            }
            if (request.CurrentUser.Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var old)) {
                old.Properties.ForEach(p => db.OperateOnProperty(request.CurrentUser.Id, p.Value, PropertyOperation.SetInvalid, null));
                request.CurrentUser.Properties.Remove(CoreP.Context.A().Key.CoreP); //( Hack because OperateOnProperty above will not remove property, resulting in UpdateProperty below not doing any change.
            }
            // Naïve approach. Will only set one context-value.
            //c.Properties.ForEach(p => db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, 
            //    // NOTE: No need for specifying PropertyKeyWithIndex here. Most probably it would only result in an obscure exception anyway.
            //    // p.Value.Key.PropertyKeyWithIndex,
            //    p.Value.Key,
            //    p.Value.V<Context>()));
            db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, CoreP.Context.A(), _new.V<List<Context>>());

            /// ====================================================
            /// Step 2, show actual result of report
            /// ====================================================
            /// Note that step 1 and step 2 are really independent of each other.
            if (!TryGetPV<Type>(ReportP.ReportEntityType.A(), out var requiredType)) {
                request.Result.ResultCode = ResultCode.ok;
                request.Result.AddProperty(CoreP.SuggestedUrl.A(), request.API.CreateAPIUrl(CoreAPIMethod.Context));
                request.Result.AddProperty(CoreP.Message.A(), 
                    "Context has been set to Context of report, " +
                    "but no actual result is shown because Property " + ReportP.ReportEntityType + " is not set for this report (" + IdFriendly + "). " +
                    "Set this Property if you want some actual results shown");
                return request.GetResponse();
                // return request.GetErrorResponse(ResultCode.data_error, "Property " + ReportP.ReportEntityType + " not set for this report (" + IdFriendly + "). Must be set in order for this method to execute.");
            }
            QueryId queryId;
            if (TryGetPV<QueryIdFieldIterator>(ReportP.ReportQueryIdFieldIterator.A(), out var temp)) {
                queryId = temp;
            } else {
                queryId = new QueryIdContext();
            }

            if (!db.TryGetContext(request.CurrentUser, queryId, requiredType, PV(CoreP.Context.A(), new List<Context>()), out var entities, out var errorResponse)) {
                return request.GetErrorResponse(errorResponse);
            }

            if (queryId is QueryIdContext) request.PriorityOrderLimit = PriorityOrder.Neutral; /// Include more data since default is only <see cref="PriorityOrder.Important"/>
            return request.GetOKResponseAsSingleEntityOrMultipleEntities(queryId, entities, PV(ReportP.ReportName.A(),"") + "\r\n" + PV(ReportP.ReportDescription.A(),""));
        }
    }

    /// <summary>
    /// Note that Report is also a parent of <see cref="CoreP.Context"/>
    /// </summary>
    [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum ReportP {
        None,

        [PropertyKey(
            ForeignKeyOf = typeof(Person), /// Change this at application startup if you want to use another {TPerson} class than Person. Must be done before call to <see cref="PropertyKeyMapper.MapEnumFinalize"/>
            PriorityOrder = PriorityOrder.Important,
            Parents = new Type[] { typeof(Report) },
            AccessLevelRead = AccessLevel.Relation
        )]
        ReportAuthor,

        [PropertyKey(PriorityOrder = (PriorityOrder.Important - 1), Size = InputFieldSize.Big, Group = typeof(ReportPropertiesDescriber))]
        ReportName,

        [PropertyKey(PriorityOrder = PriorityOrder.Important, Size = InputFieldSize.MultilineMedium, Group = typeof(ReportPropertiesDescriber))]
        ReportDescription,

        [PropertyKey( // TODO: IMPROVE ON DOCUMENTATION
            Description = "Specifies for which type API-method -" + nameof(Report.ShowResult) + "- should show data (may be combined with -" + nameof(ReportQueryIdFieldIterator) + "-).",
            PriorityOrder = PriorityOrder.Important,
            Type = typeof(Type), Group = typeof(ReportPropertiesDescriber))]
        ReportEntityType,

        [PropertyKey( // TODO: IMPROVE ON DOCUMENTATION
            Description = "Specifies result of API-method -" + nameof(Report.ShowResult) + "- (must be combined with -" + nameof(ReportEntityType) + "-).",
            Type = typeof(QueryIdFieldIterator), Size = InputFieldSize.Big, Group = typeof(ReportPropertiesDescriber))]
        ReportQueryIdFieldIterator,

        [PropertyKey(
            Description = "Fields to be excluded from presentation (can not be combined with -" + nameof(ReportIncludeFields) + "-).",
            IsMany = true,
            Group = typeof(ReportPropertiesDescriber)
        )]
        ReportExcludeFields,

        [PropertyKey(
            Description = "Fields to be included in presentation (can not be combined with -" + nameof(ReportExcludeFields) + "-).",
            IsMany = true,
            Group = typeof(ReportPropertiesDescriber))]
        ReportIncludeFields,
    }

    public class ReportPropertiesDescriber : IGroupDescriber {
        public void EnrichKey(PropertyKeyAttributeEnriched key) {
            key.AddParent(typeof(Report));
            key.A.AccessLevelRead = AccessLevel.Relation;
            key.A.AccessLevelWrite = AccessLevel.Relation;
        }
    }

    public static class ReportPExtensions {
        public static PropertyKey A(this ReportP p) => PropertyKeyMapper.GetA(p);
    }
}