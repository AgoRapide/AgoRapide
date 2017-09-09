// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(
        AgoRapideEnumType = EnumType.EnumValue,
        Description =
            "In AgoRapide different categories of objects are treated in the same manner " +
            "as traditional application data objects (here named -" + nameof(APIDataObject) + "-). " +
            "This opens up for using the powerful query and drill down mechanism in AgoRapide also for " +
            "documentation of your application. "
    )]
    public enum EntityTypeCategory {

        None,

        [EnumValue(Description = "Value used when more precise description (like for instance -" + nameof(APIDataObject) + "-) is not possible. ")]
        BaseEntity,

        [EnumValue(Description =
            "Represents a (traditional) basic data object that your API provides like Person, Order, Product. " +
            "All your data object classes should inherit this class in order to get better -" + nameof(ResponseFormat.HTML) + "- functionality. " +
            "Not used by the AgoRapide library itself. " +
            "Corresponds to -" + nameof(AgoRapide.APIDataObject) + "-."
        )]
        APIDataObject,

        [EnumValue(
            Description =
                "Practical term used to designate something more complex than an -" + nameof(APIDataObject) + "- " +
                "with independent business logic (like for instance logic for communication with outside systems). " +
                "Corresponds to -" + nameof(AgoRapide.Agent) + "-.",
            LongDescription =
                "Note the pragmatic approach, combining business logic and data objects"
       )]
        Agent,

        [EnumValue(Description =
            "Represents some internal part of your application. " +
            "Corresponds to -" + nameof(Core.ApplicationPart) + "-.")]
        ApplicationPart
    }
}
