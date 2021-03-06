﻿// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(
        Description = "Characterizes the runtime environment that the application runs in.",
        LongDescription = "Environment in AgoRapide are used in different conceptual understandings:\r\n" +
            "\r\n" +
            "1) To characterize a single property. See -" + nameof(PropertyKeyAttribute) + "-.\r\n" +
            "   The current -" + nameof(Environment) + "- has to be equivalent or lower in order for the property to be shown / accepted\r\n" +
            "\r\n" +
            "2) To characterize an API method. See -" + nameof(PropertyKeyAttribute) + "-.\r\n" +
            "    The current -" + nameof(Environment) + "- has to be equivalent or lower in order for the method to be included in the API routing\r\n" +
            "\r\n" +
            "3) To characterize an individual instance of an entity. See -" + nameof(BaseEntity) + "-\r\n" +
            "    In this manner you may switch on functionality for specific customers only in for instance your production environment.\r\n" +
            "\r\n" +
            "Use cases 1), 2) and 3) give the possibility of using the same code base in development, test and production.\r\n" +
            "This again reduces the need for having branches in your source code repository\r\n" +
            "\r\n" +
            "And last, you may use Environment for\r\n" +
            "4) To (the traditional understanding) characterize the runtime environment the application runs in\r\n" +
            "    (see -" + nameof(ConfigurationAttribute.Environment) + "-)\r\n",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum Environment {
        None,
        Development,
        Test,
        Production
    }
}
