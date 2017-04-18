﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide {

    /// <summary>
    /// Note how we DO NOT set any <see cref="AgoRapideAttribute.Description"/> for <see cref="CoreP.CoreMethod"/> 
    /// but instead rely on the <see cref="AgoRapideAttribute.Description"/> set here. 
    /// This comment describes the recommended approach to setting attributes when the type given (<see cref="AgoRapideAttribute.Type"/>) 
    /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
    /// </summary>
    [AgoRapide(
        Description = "Represents core AgoRapide API methods that must be available in the client application.",
        EnumType = EnumType.DataEnum
    )]
    public enum CoreMethod {
        None,

        /// <summary>
        /// TODO: LINK THIS SOMEWHERE
        /// </summary>
        [AgoRapide(Description = "\"Starting point\" for API. Will usually return HTML-format regardless of -" + nameof(ResponseFormat) + "- requested by client")]
        RootIndex,

        [AgoRapide(Description ="Access to -" + nameof(Configuration) + "-.")]
        Configuration,

        /// <summary>
        /// Describes methods like api/APIMethod/{id}, api/Person/{id} and so on. Also used for api/Property/{id}
        /// See <see cref="BaseController.HandleCoreMethodEntityIndex"/>
        /// </summary>
        [AgoRapide(Description = "Basic access to entities.")]
        EntityIndex,

        //[AgoRapide(Description = "Documentation about API methods")]
        //MethodIndex,

        //[AgoRapide(Description = "Basic access to properties")]
        //PropertyIndex,
            
        [AgoRapide(Description = "Adding of entities. Corresponding -" + nameof(APIMethodOrigin.Autogenerated) + "- -" +nameof(APIMethod) + "- will get parameters according to -" + nameof(Extensions.GetObligatoryChildProperties) + "-")]
        AddEntity,

        [AgoRapide(Description = 
            "Calls -" + nameof(IDatabase.UpdateProperty) + "-. " +
            "Will create a new property if it did not exist already or if the old values was different.\r\n" +
            "If property existed with same value then its -" + nameof(DBField.valid) + "- and -" + nameof(DBField.vid) + "- will be updated")]        
        UpdateProperty,

        [AgoRapide(Description = 
            "Calls -" + nameof(IDatabase.GetEntityHistory) + "- for the given property. " +
            "If the property is an entity root-property then all history information for that entity is returned.")]
        History,

        [AgoRapide(Description = 
            "Executes either -" + nameof(AgoRapide.PropertyOperation.SetValid) + "- or -" + nameof(AgoRapide.PropertyOperation.SetValid) + "-.")]
        PropertyOperation,

        [AgoRapide(Description = 
            "The application specific general query when looking up a context. " +
            "Typical example could be somebody in a support department looking up a customer based on whatever identification is available. " +
            "Result is communicated through -" + nameof(GeneralQueryResult) + "-")]
        GeneralQuery,

        /// <summary>
        /// See <see cref="BaseController.AgoRapideGenericMethod"/>
        /// </summary>
        [AgoRapide(Description =
            "Maps to the {*url} route, " +
            "in other words it is the method handling all API calls not recognized by the ASP .NET routing mechanism.\r\n" +
            "The implementation of this method calls -" + nameof(BaseController) + "." + nameof(BaseController.AgoRapideGenericMethod) + "- " +
            "(see that method again for further documentation).")]
        GenericMethod,

        /// <summary>
        /// See <see cref="BaseController.HandleCoreMethodExceptionDetails"/>
        /// </summary>
        [AgoRapide(Description = 
            "Offers details about the last exception that occurred.\r\n" +
            "The implementation of this method calls -" + nameof(BaseController) + "." + nameof(BaseController.HandleCoreMethodExceptionDetails) + "- " +
            "(see that method again for further documentation).")]            
        ExceptionDetails,
    }
}
