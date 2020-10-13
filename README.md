
```diff
- NOTE: AgoRapide 2017 has been replaced with AgoRapide 2020, 
- NOTE: a library for building data-oriented backend applications.
```

NOTE: AgoRapide 2020 code is [here](https://bitbucket.org/BjornErlingFloetten/arcore) 

NOTE: AgoRapide 2020 documentation is [here](http://bef.no/agorapidedoc/)

.


"The shape of a program should reflect only the problem it needs to solve" - [Paul Graham - Revenge of the Nerds](http://www.paulgraham.com/icad.html).

Use AgoRapide and this goal comes within your reach!


# AgoRapide 2017
Lightweight pragmatic integrated library for creation of .NET REST APIs offering JSON, HTML, PDF and CSV views. Key-value single table database storage with data-driven entity model. Written in C# 7.0 using Visual Studio 2017.

&#x1F53A; PLEASE NOTE: AgoRapide is currently in the beta-stage of development. Do not use it for any production purposes yet. Only use it if you are curious and / or want to help us with contributions. One welcome contribution would for instance be attending to TODO's in this document! 

Please feel free to contact the author for more information.

# What is AgoRapide?
(Note: For code examples please scroll further down)

AgoRapide is a .NET library for creation of REST APIs offering JSON, HTML, PDF and CSV views.

AgoRapide is open sourced and MIT-licensed 

AgoRapide is a lightweight library with untraditional ideas for specially interested people. In some areas we reinvent the wheel with happy abandon, in other we are more traditional. We hope everything is easily understood. 

AgoRapide is about avoiding repetition. By repetition we mean all kinds of tasks that are similar to each other. Like creating tables, API-methods, objects, properties for objects, populating objects, validating properties, naming of properties. Every time that we see a pattern of repetition we try to factor out the common component. See further down for more details about repetition.

AgoRapide is an integrated solution encompassing API, documentation, unit-testing and an administrative HTML5-based interface all in one single integrated package.

Through an ingenious URL-mechanism the API is discoverable both through JSON queries, HTML5, PDF and CSV (Comma Separated Values). A small cost administrative interface integrates easily, eliminating the need for expensive development of separate systems. Documentation and unit-testing is tightly coupled with the code facilitating efficient updates of both.

AgoRapide is also (optionally) a model for backend storage in a single table based on the key-value principle (you do not need to implement this in order to use AgoRapide but may chose a more traditional database approach). AgoRapide "as is" supports PostgreSQL / PostGIS. The backend storage implements automatic history record of all changes to the database.

AgoRapide supports Windows, .NET and IIS. If you also want the backend storage then Npgsql and PostgreSQL / PostGIS is supported. We welcome contributors who would like to expand on this. The approach is usable for lots of different platforms. 

AgoRapide is based on a pragmatic development philosophy acquired through 30 years of experience developing all kinds of small to medium scale applications with a focus on data and control systems. The keyword is pragmatic, we strive not to dogmatically stick to some ideology. Instead we prefer to test out what works in practice.

AgoRapide is open for contributions and new ideas. As of June 2017 it is mostly a one-man project (by Bjørn Erling Fløtten). We would especially welcome ideas for turning AgoRapide into a more complete modelling tool (with the possibility of generating traditional SQL schemas for instance. See [panSL.org](http://panSL.org) for some history). We are also considering automatically creating native app-code, based on the property information given in AgoRapide. There are of course also a lot of mundane improvements to be made regarding annotations and validating of properties.

AgoRapide consists of two main areas of functionality, the API and the key-value data storage. The current sample application (AgoRapideSample) demonstrates use of both but there is nothing inherent in the API forcing you to use a key-value data storage.

1) API: The REST API functionality with documentation, unit-testing and HTML administrative interface. There is also a small AgoRapide-0.1.js to include in your web-project. This in turn depends on jquery-3.1.1.min.js. 

2) Data storage: (Optional to use) Data access towards a database with a single table based on the key-value principle (only supports PostgreSQL at the moment, some abstraction is built in, in order to prepare for other database engines). 
The objects in the entity model consist of a general collection of properties, together with functionality for strongly tagging these properties with meta data. 
You could use AgoRapideSample as a super light framework for getting started with the AgoRapide library.

AgoRapide is written all-new in C# 7.0 targettng .NET framework 4.7 (using Visual Studio 2017). 

The compiled DLLs should be possible to reference in older environments (like Visual Studio 2015 
and C# 6.0 for instance, or something even older, but it has not been tested yet)

# Code example, implement a simple API method

## Annotate a special enum (if needed)

```C#
public enum P {
    None,
	...
    [PropertyKey(
        Description = "Used for demo-method in -" + nameof(SomeController.DemoDoubler) + "-",
        SampleValues = new string[] { "42", "1968", "2001" },
        InvalidValues = new string[] { "1.0" },
        Type = typeof(long))]
    SomeNumber,
	...
}
```

## Create API method

(Please note that AgoRapide automatically implements for you all the simple CRUD (Create, Read, Update, Delete) API methods)

```C#
public class SomeController : BaseController {
    [HttpGet]
    [Method(
        Description = "Doubles the number given (every time)",
        S1 = nameof(DemoDoubler), S2 = P.SomeNumber
    )]
    public object DemoDoubler(string SomeNumber) {
        try {
            if (!TryGetRequest(SomeNumber, out var request, out var completeErrorResponse)) return completeErrorResponse;
            var answer = checked(request.Parameters.PV<long>(P.SomeNumber) * 2);
            return request.GetOKResponseAsText(answer.ToString(), "Your number doubled is: " + answer);
        } catch (Exception ex) {
            return HandleExceptionAndGenerateResponse(ex);
        } finally {
            DBDispose();
        }
    }
}
```

## Test your method

[All methods](http://sample.agorapide.com/)

Note how methods are data just like everything else in your API (illustrating our idea of avoiding repetition, we just reuse the general API-mechanism for documentation purposes). 

Note how anonymous access to basic API method documentatation is allowed. Use username 'a' and password 'a' when credentials are needed for the more restricted operations.

[This method](http://sample.agorapide.com/api/APIMethod/APIMethod_DemoDoubler__SomeNumber_/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/APIMethod/APIMethod_DemoDoubler__SomeNumber_/)

Note suggested URLs for immediate testing.

Test friendliness of API:

[Call method with missing parameter](http://sample.agorapide.com/api/DemoDoubler/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/DemoDoubler)

Note suggestions of complete URL for valid parameter.

[Call method with invalid parameter](http://sample.agorapide.com/api/DemoDoubler/1.0/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/DemoDoubler/1.0)

Note suggestion of sample values.

[Call method normally](http://sample.agorapide.com/api/DemoDoubler/42/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/DemoDoubler/42)

[Call method provocate exception](http://sample.agorapide.com/api/DemoDoubler/7777777777777777777/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/DemoDoubler/7777777777777777777)

Note balance between security (holding back information) and user friendliness (giving out information). Some exception information is given but not everything.

[Ask for exception details](http://sample.agorapide.com/api/ExceptionDetails/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/ExceptionDetails)

(use credentials 'a' / 'a' for above link)

Note logging of parameter values (although exception message only says OverflowException the log itself has kept the actual data value (like 7777777777777777777) facilitating easier debugging)

# Code example, implement a new entity

## Construct your class

```C#
    [PropertyKey(
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.Anonymous
    )]
    public class Car : APIDataObject {        
    }
```
## Annotate an enum

```C#
public enum CarP {
    None,
	...
    [PropertyKey(
        Type = typeof(Colour), 
        Parents = new Type[] { typeof(Car) }, 
        IsObligatory = true, 
        AccessLevelRead = AccessLevel.Anonymous, 
        AccessLevelWrite = AccessLevel.Anonymous)]
    Colour,
	...
}
```

## Test it

[All methods](http://sample.agorapide.com/)

Note how methods like [Car/Add/{Colour}](http://sample.agorapide.com/api/APIMethod/APIMethod_Car_Add__Colour_/HTML) are created automatically with suggested URLs like http://sample.agorapide.com/api/Car/Add/Red/HTML

(TODO: Link will probably break in the near future)

You also have methods like [Car/{QueryId}](http://sample.agorapide.com/api/APIMethod/APIMethod_Car__QueryId_/HTML) with suggested URL like http://sample.agorapide.com/api/Car/All/HTML.

Note how the HTML administrative interface automatically suggests drill down URLs like http://sample.agorapide.com/api/Car/WHERE%20Colour%20=%20Blue/HTML 

# How AgoRapide avoids repetition

AgoRapide has been crafted slowly and carefully. 

Code that comes easy to mind has been looked upon with suspicion because "easy" code usually implies that some repetition is taking place. We have constantly strived to avoid any repetition and in that process also kept the number of code lines to a minimum and thereby also kept the corollary flexibility in the code-base to a maximum.

(The inavoidable higher level of abstraction that comes as a consequence of all this has been somewhat mitigated by extensive use of linked comments and very detailed exception messages.)

Some forms of repetition that we have identified and attempted to avoid in AgoRapide are:

## Creating API-methods is a repetition
API-methods of the form GetSomeObjectTypeBySomePropertyValue have been generalised in AgoRapide into one single idea: 

Api/{SomeObjectType}/{QueryExpression} like Api/Person/WHERE FirstName LIKE 'John%'

This means that you do not have to repeatedly create API-methods that in essence are copies of each other. 

## Creating properties of objects is a repetition
Properties of objects like Person.FirstName are usually implemented as public string FirstName { get; set; }. This is just another kind of repetition. 

These have instead been generalised by AgoRapide and are accessed like 

person.PV&lt;string&gt;(P.FirstName) (where P is the enum used throughout your application)

or more static strongly typed as

person.PVM&lt;Colour&gt;() or 

person.PV&lt;int&gt;(P.Age). 

Of course, if you see that you have to repeat person.PV&lt;int&gt;(P.Age) all over your codebase then you just implement a traditional property getter int Age =&gt; PV&lt;int&gt;(P.Age). But please note that you still do not have to implement the setter (and validator) as that is done from a general properties collection.

Another advantage of AgoRapide's approach is inbuilt casting or conversion. For instance you can do person.PV&lt;string&gt;(P.DateOfBirth) directly with the conversion from DateTime to string being done in a consistent manner according to the original enum annotation.

## Handling of unknown values is a repetition
Sometimes values are not set. AgoRapides gives you three possibilities for handling this:

For instance, if you are unsure about FirstName being set or not, you can do one of the following:

1) person.PV&lt;string&gt;(P.FirstName) which will immediately throw a (detailed) exception message (that is, you demand / assert the value to always be set).
2) person.PV(P.FirstName, "") which will return the empty string as a default value. 

and lastly, you can use the TryGet-method:
3) person.TryGetPV(P.FirstName, out var firstName). 

This consistency is made possible by the fact that traditional getters are not used.

## Traversing entity relations is a repetition
Entity relations have to be specified only once. 

For instance for a Car:
```C#
public enum CarP {
    None,
	Model, 
	RegistrationNumber,
	Colour, 
	...
    [PropertyKey(ForeignKeyOf = typeof(Person)]
    CarOwner,
	...
}
```

Some examples of where AgoRapide utilizes this information:

1) The HTML administrative interface automatically creates links between entities. 
2) The "context" concept automatically creates drilldown functionality based on unions and intersections between different sets of data. Example: By choosing a set of persons you automatically get functionality for drilling down to their related cars.
3) The tagging mechanism automatically adds information like "number of cars owned by person" (which in turn helps with drill-down operation, that is, operations like api/Person/WHERE CountOfCars > 1 becomes immediately available, greatly lessening query complexities).

## Creating tables is a repetition
(optional for you to apply. 

You may use a traditional database and still leverage all of the API functionality of AgoRapide).

AgoRapide's default data storage uses the Entity-Attribute-Value (EAV) table concept, meaning everything is stored as a single table. It still supports relations and some relational integrity through the database layer though, but there are of course obvious tradeoffs to consider. 

## Populating object properties is a repetition
AgoRapide populates object properties through a general Property collection eliminating the need for specific setters and getters in C# for each and every property. 

## Validating properties is a repetition
Validation (and cleaning up user input) in AgoRapide is data driven and may be be injected at any stage like:
1) When parsing user input, 
2) When communicating with other systems

and
3) When reading from database. 

In general the validating logic is given as [System.Attribute](https://msdn.microsoft.com/en-us/library/system.attribute(v=vs.110).aspx)-properties for the corresponding enum value describing a property meaning it is easily "within reach" everywhere in the code.

## Writing unit tests, examples, tool tips and documentation is a repetition
AgoRapide combines all these tasks into one. Once you have documented your enums then everything else follows automatically. 

NOTE: As of the time of writing unit tests are not yet implemented. This is due to there being very little to test because an AgoRapide-based application becomes data driven to a high degree. We have not forgotten about unit tests however and we strongly want to add it in future releases (some of the unit tests should be possible to generate 100% automatically since the basic annotation model is very rich in its ability to explain about valid and invalid data). 

## Branches is a repetition
(optional for you to apply). 

Instead of multiple branches in your source code repository you may mark different properties in AgoRapide as "development", "test", "production" and the system will automatically hide features in test or development environment as needed. 

Note that this may be done even for individual entities, that is, you may have just some of your customers test functionality in your production environment for instance. 

## Environments is a repetition
(optional for you to apply). 

Different runtime environments like development, test and production is a repetition. 

With AgoRapide we strive to at least make these environments as identical as possible in the sense that the same source code can run in all environments. This means less work in administering environments.

## Creating database, API and client UI is a repetition
With a single-table database (optional) your database schema will in practice never change. 

For the client user interface (UI) there will (in the near future) be some auto-generated code libraries available (in Java and ObjectiveC). We are looking into the possibility of auto-generated code for app views also (not currently supported). 

(even this section of course repeats the phrase "is a repetition" multiple times)

# When is AgoRapide useful for my projects?

AgoRapide is useful when you desire:

## Lightweight reports
With AgoRapide you get easy manual querying and updates (lightweight reports) through REST API in web browser (instead of SQL statements). 

You can for instance give your users a controlled SQL-like access to data without having to write special reports. 

Example is ad-hoc reports like: 

api/Customer/WHERE first_name LIKE 'John%'/Property/phone_number. This would correspond to SELECT phone_number FROM customers WHERE first_name LIKE 'John%' in an traditional system.

With AgoRapide anybody can "write" such reports and yet still the access is controlled through the APIs general access control mechanism.

With the concept of "Contexts" you can create cuts and unions between different sets like:

Context1: Intersect;Person;WHERE country = 'France'
Context2: Intersect;Car;WHERE color = 'Red'
Assuming there being a relationship between Person and Car queries like api/Person/CurrentContext or api/Car/CurrentContext will only show the relevant entities (that is French persons who own red cars, or red cars with French owners).

This "Context" concept can also be wrapped up into Reports with basic aggregates being suggested automatically.

In general AgoRapide is quite suitable as a basis for a lightweight business intelligence / reporting system. There is a lot more functionality inbuilt like fast synchronization against other data sources. This can again be combined with in-memory tagging of data which makes for a very fast efficient drill-down interface.

## Easy change of entity model
The entity model can be changed very rapidly. The database schema will usually not change since you change the entity model by using simple UPDATE statements instead of the more complex CREATE TABLE / ALTER TABLE.

Example: UPDATE p SET key = 'colour' WHERE key = 'color' together with a simple rename of enum P.color to P.colour in the C# code. 

You do not need to implement specific accessors in your objects. Properties are accessed in a strongly static typed fashion like car.PV&lt;Colour&gt; (PV is shorthand for "property value"). 

Changes in relations can likewise be adjusted through UPDATE statements. 

## Small budgets / short time to market
When using AgoRapide you get sooner to marked with a more solid product. 

You do not have to budget separately for administrative tools / support department tools or similar. Also the cost of documentation is greatly reduced with AgoRapide.

## Extensive logging (with actual data), exception handling and helpful _relevant_ links to documentation in error messages

Excerpt of typical exception message:

Exception: OverflowException 
Message: Arithmetic operation resulted in an overflow. 
Source : AgoRapideSample 
Stacktrace: 
at AgoRapideSample.AnotherController.DemoDoubler 
in C:\git\AgoRapide\AgoRapideSample\Controllers\AnotherController.cs:line 35 
2017-03-22 15:25:01.839: 13: AnotherController.TryGetRequest: Parameter SomeNumber: 7777777777777777777 
...
(more log-data would usually follow here)
...

[Or ask for last exception](http://sample.agorapide.com/api/ExceptionDetails/HTML)&nbsp;&nbsp;[(as JSON)](http://sample.agorapide.com/api/ExceptionDetails)

(use credentials 'a' / 'a' for above link)

Note how actual data in both exception message and log greatly helps in pinpointing problem.

(See also more links above under Code examples, for how missing parameters and invalid parameters are handled with helpful error messages). 

## A history of all changes to database
AgoRapide does not even support the concept of deletion. 

Properties are only added to the database, with older properties marked as invalid. 

The entity (the user) doing the change is also logged telling you who changed what data. 

See methods like [Property/{QueryIdInteger}/History](http://sample.agorapide.com/api/APIMethod/APIMethod_Property__QueryIdInteger__History/HTML) and [example](http://sample.agorapide.com/api/Property/5614/History/HTML) (Note: Last link may be broken).

## A C# centric approach
AgoRapide is more C# centric than database centric in the sense that the database schema is in reality stored as C# code. Auto generated libraries are then used as basis for access through other programming environment (like Java, Swift, ObjectiveC and so on). 

The database engine, although still a relational one, is thereby utilized in quite a simple manner.

# Where is AgoRapide in use now?
AgoRapide as a business intelligence system is being used now with great success within companies owned by main developer Bjørn Erling Fløtten.

A philosophy similar to AgoRapide's REST API architecture has also been used with success in multiple other projects where:

The entity model is relatively simple (10-20 types of entities). 

Each entity has lots of field associated with it and the fields change over time (since there is no need to implement accessors for each and every field there is little code to keep updated). 

Entity fields rapidly change name or character as you develop / specifications changes (changing names is extremely simple for instance). 

The total number of fields is around one thousand. 

The database contains from thousands up to tens of millions of rows. 

Data is more often read from database than written. 

Most of the data fits inside the servers RAM making the aggresive cache mechanism very effective. 

# When should I not use AgoRapide?
AgoRapide is probably less useful if:

You prefer traditional ways of thinking (but you would probably not have read all the way down to this paragraph anyway in such a case).

Your datamodel is very stable from the beginning.

Your organisation has sufficient resources for separate departments for development, testing, documentation and administration.

The datamodel has lots of relations (it might or it might not be practical to use AgoRapide, we just have not tested it yet). [panSL.org](http://panSL.org) may help with advanced data modelling, this is a try-as-you-go data modelling tool developed by the same author as AgoRapide originally. 

There is a need for storing hundreds of millions of rows in your database (again, this has just not been tested as of the time of writing. Tens of millions has been tested and is quite OK).

# How do I start with AgoRapide?
Clone our AgoRapideSample project and link to AgoRapide library in order to get started.

TODO: As of the time of writing we have not worked out how to automate this yet. Contributions are welcome!

# Future development

(we have a thousand other ideas than those listed below, please give us a call if you are interested in contributing)

AgoRapide and AgoRapideSample as Nuget packages.

More security functionality (like OAuth 2.0 for instance, at the moment HTTPS with Basic authentication is implemented).

Autogenerated client libraries (we are holding a little back here though because often an app / client will be best served by a specialized "AppView" tailored specifically by the API for that specific app / client. In other words, moving as much code as possible onto your server side).

More closer adherence to REST API standards (currently only HTTP GET with response code 200 is used. POST is supported, but not other HTTP methods. There is pragmatic reasons for the current design however).

Support for concepts like vnd.error.

Support for documentation systems like Postman.

Unit testing (data driven based on existing annotations). 

Support for other database engines than PostgreSQL.

Inbuilt support for "traditional" database tables (multiple tables instead of the current single table approach). 

# FAQ

## What do you  mean by HTML administrative view?
A rudimentary interface useful for administrering your backend, API, and doing support for your customers. 

Example: [HTML interface](http://sample.agorapide.com/api/Car/5109/HTML)&nbsp;&nbsp;[(correspondig data as JSON)](http://sample.agorapide.com/api/Car/5109)

(use credentials 'a' / 'a' for these links)

As you can see the HTML interface is like a clone of the JSON interface with the addition of useful links and documentation. 
(the current functionality is quite limited but we assume that you get the idea. Similar philosophies has been used to a great extent with success in other applications)

In practice the HTML view often eliminates completely the need for building a separate application for doing customer support (or correspondingly eliminating the need for adding support department related functionality into your customer application).

## Is AgoRapide a replacement of WebAPI?
No, it is built on top of WebAPI. We use as much as possible of the standard functionality in WebAPI, especially the routing mechanism (not attribute based but classic routing). Also, the signature of your controller methods will be familiar. We are Not that fANCY.

## Is AgoRapide technically complicated? 
We strive to keep the code as simple as possible. 

For instance there is almost no use of reflection in the code. Use of generics is also kept to a minimum.

## Is AgoRapide easy to get started with?
Yes. 

You need very little boilerplate code to get started. We have also striven to make all error messages VERY detailed, always pinpointing the exact cause of any problems. 

Use AgoRapideSample as a starting point and you should be up and running with your first API in minutes. Please feel free to email bef at bef.no for guidance.

## Other interesting aspects about AgoRapide?
Yes, of course! 

A particular useful feature is impersonating users through the "entity-to-represent" and "represented-by-entity" concept. With this concept the API can give the view of one API client (user) based on the credentials of another API-client (administrative user). 

In practice this means that your support department may see exactly the same data as your customer sees in your application, without the customer having to give away his / her / its password (this also goes for logging into apps built on top of an AgoRapide API).

## What is AgoRapide.com?
Old [AgoRapide.com](http://AgoRapide.com) is our earlier implementation of the same idea as our new AgoRapide C# library. 

AgoRapide.com uses a schema language (see [panSL.org](http://panSL.org)) from which a dynamic HTML interface is generated. 

AgoRapide.com is able to generate C# code. The properties as given in the schema language corresponds closely to the "P"-enums in AgoRapide in the sense that as much as possible of the application logic is stored there. The generated C# however is very old-fashioned with none of the elegance of AgoRapide library.

AgoRapide.com is also much more limited since you would totally depend on our server and infrastructure. Neither can the autogenerated C# code from AgoRapide.com be modified and fed back into the schema generator. 

With the new AgoRapide C# library you write and control your own code. In addition the MIT license of AgoRapide gives you great flexibility in how to deploy your application.

## How is the performance of the AgoRapide library? 
AgoRapide delivers very good performance. 

Apart from initialization there is very little code actually being executed when you run an application based on the AgoRapide library. 

The call stack imposed by AgoRapide is very short. 

Great care has been taken in order to avoid repetitive calculations within the code. There is for instance a lot of use of ConcurrentDictionary in order to cache frequent calculations.

Entity properties however are read from a dictionary, instad of directly. This could be an issue if you intensively use certain properties repeatedly. In such cases you can always implement traditional (cached) property getters as needed.

## What security features does AgoRapide have?
AgoRapide has a built in Basic Authentication mechanism. If your needed security level is very basic and you desire a focus on user friendliness then you may use this. Use with caution however!

For more strict needs you are supposed to supply a more thorough mechanism like OAuth or similar. 

Apart from that there is of course a basic eye to security throughout the library. Passwords are salted and hashed for instance, not shown by the API mechanism. SQL injection / Javascript injection attacks should also in principle not be possible.

But, we would of course be happy for any input from experts in the field of security.

## How does AgoRapide relate to Entity Framework and Razor?
AgoRapide does not use any of these concepts. 

AgoRapide could be considered a lightweight, more data-driven variant of Entity Framework in the sense that classes are defined as annotated enums (instead of traditional properties) and those annotations again are used as the basis for building the database and the user interface. (In reality this means that if you find that Entity Framework works well for you then you will probably not have any use of AgoRapide.)

## Why does AgoRapide not use .NET's inbuilt attribute routing?
It is on our todo-list. We are concerned though about how to implement client-friendly error messages for invalid parameter values.
