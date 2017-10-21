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
        /// <summary>
        /// <see cref="CoreAPIMethod.BaseEntityMethod"/>. 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="request"></param>
        [APIMethod(
            Description = "Sets -" + nameof(CoreP.Context) + "- for the current user to -" + nameof(CoreP.Context) + "- for report as identified by {QueryId}.",
            S1 = nameof(Use), S2 = "DUMMY", // TODO: REMOVE "DUMMY". Added Summer 2017 because of bug in routing mechanism.
            AccessLevelUse = AccessLevel.Relation,
            ShowDetailedResult = true)]
        public object Use(BaseDatabase db, ValidRequest request) {
            var properties = new List<(PropertyKeyWithIndex, object)>();
            if (!Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var _new)) { // Note how we assume that Context will always contain at least one property once defined.k
                return request.GetErrorResponse(ResultCode.data_error, "No " + nameof(CoreP.Context) + " properties found for this report");
            }
            if (request.CurrentUser.Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var old)) {
                old.Properties.ForEach(p => db.OperateOnProperty(request.CurrentUser.Id, p.Value, PropertyOperation.SetInvalid, null));
            }

            // Naïve approach. Will only set one context-value.
            //c.Properties.ForEach(p => db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, 
            //    // NOTE: No need for specifying PropertyKeyWithIndex here. Most probably it would only result in an obscure exception anyway.
            //    // p.Value.Key.PropertyKeyWithIndex,
            //    p.Value.Key,
            //    p.Value.V<Context>()));

            db.UpdateProperty(request.CurrentUser.Id, request.CurrentUser, CoreP.Context.A(), _new.V<List<Context>>());

            request.Result.ResultCode = ResultCode.ok;
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), request.API.CreateAPIUrl(CoreAPIMethod.Context));
            // request.Result.AddProperty(CoreP.Message.A(), "xxx"); // Probably unnecessary
            return request.GetResponse();
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

        [PropertyKey(PriorityOrder = (PriorityOrder.Important - 1), Size = InputFieldSize.Medium, Group = typeof(ReportPropertiesDescriber))]
        ReportName,

        [PropertyKey(PriorityOrder = PriorityOrder.Important, Size = InputFieldSize.MultilineMedium, Group = typeof(ReportPropertiesDescriber))]
        ReportDescription,

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