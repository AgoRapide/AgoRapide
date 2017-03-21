# AgoRapide
Lightweight pragmatic integrated .NET REST API offering JSON and HTML views. Key-value single table database storage with data-driven entity model. Written in C# 7.0 using Visual Studio 2017.

PLEASE NOTE: AgoRapide is currently in the alpha-stage of development. Only use it if you are curious and / or want to help us with contributions. One welcome contribution would for instance be attending to TODO's in this document!

# What is AgoRapide?
AgoRapide is a REST API library based on .NET offering JSON and HTML views.

AgoRapide is open sourced and MIT-licensed 

AgoRapide is a lightweight library with untraditional ideas for specially interested people. In some areas we reinvent the wheel with happy abandon, in other we are more traditional. We hope everything is easily understood. 

AgoRapide is about avoiding repetition. By repetition we mean all kinds of tasks that are similar to each other. Like creating tables, API-methods, objects, properties for objects, populating objects, validating properties. Every time that we see a pattern of repetition we try factor out the common component. See further down for more details about repetition.

AgoRapide is an integrated solution encompassing API, documentation, unit-testing and an administrative HTML5-based interface all in one single integrated package.

Through an ingenious URL-mechanism the API is discoverable both through JSON queries and HTML5. A small cost administrative interface integrates easily, eliminating the need for expensive development of separate systems. Documentation and unit-testing is tightly coupled with the code facilitating efficient updates of both.

AgoRapide is also (optionally) a model for backend storage in a single table based on the key-value principle (you do not need to implement this in order to use AgoRapide but may chose a more traditional database approach). AgoRapide "as is" supports PostgreSQL / PostGIS. The backend storage implements automatic history record of all changes to the database.

AgoRapide supports Windows, .NET and IIS. If you also want the backend storage then Npgsql and PostgreSQL / PostGIS is supported. We welcome contributors who would like to expand on this. The approach is usable for lots of different platforms. 

AgoRapide is based on a pragmatic development philosophy acquired through 30 years of experience developing all kinds of small to medium scale applications with a focus on data and control systems. The keyword is pragmatic, we strive not to dogmatically stick to some ideology. Instead we prefer to test out what works in practise.

AgoRapide is open for contributions and new ideas. As of March 2017 it is mostly a one-man project of Bjørn Erling Fløtten. We would especially welcome ideas for turning AgoRapide into a more complete modelling tool (with the possibility of generating traditional SQL schemas for instance. See [panSL.org] (http://panSL.org) for some history). We are also considering automatically creating native app-code, based on the property information given in AgoRapide. There are of course also a lot of mundane improvements to be made regarding annotations and validating of properties.

AgoRapide consists of two main areas of functionality, the API and the key-value data storage. The current sample application (AgoRapideSample) demonstrates use of both but there is nothing inherent in the API forcing you to use a key-value data storage.
1) API: The REST API functionality with documentation, unit-testing and HTML administrative interface. There is also a smalle AgoRapide-0.1.js to include in your web-project. This in turn depends on jquery-3.1.1.min.js. 
2) Data storage: (Optional to use) Data access towards a database with a single table based on the key-value principle (only supports PostgreSQL at the moment, some abstraction is built in, in order to prepare for other database engines). 
The objects in the entity model consist of a general collection of properties, together with functionality for strongly tagging these properties with meta data. 
You could use AgoRapideSample as a super light framework for getting started with the AgoRapide library.

AgoRapide is written all-new in C# 7.0 targettng .NET framework 4.5.2 (using Visual Studio 2017). 
The compiled DLLs should be possible to reference in older environments (like Visual Studio 2015 
and C# 6.0 for instance, or something even older, but it has not been tested yet)

# How AgoRapide avoids repetition:
## Creating API-methods is a repetition:
API-methods of the form GetSomeObjectTypeBySomePropertyValue have been generalised into one single idea, Api/SomeObjectType/WHERE property = 'value', meaning you do not have to repeatedly create API-methods that in essence are copies of each other. And since the general mechanism supports all kinds of operators like "greater than", "less than" and so on, you get all that flexibility for all your object classes, not only those that you write specific API-methods for.

## Creating properties of objects is a repetition:
Properties of objects like Person.FirstName are usually implemented as public string FirstName { get; set; }. This is just another kind of repetition. These have been generalised by AgoRapide into Person.Get(P.FirstName) where P is an enum. Or more strongly typed as Car.Get<Colour>() or Person.GetInt(P.Age). Of course, if you see that you have to repeat Person.GetInt(P.Age) all over your codebase then you just implement a traditional property getter int Age => GetInt(P.Age). But please note that you still do not have to implement the setter (and validator) as that is done from a general properties collection.

## Creating tables is a repetition:
(optional for you to apply. You may use a traditional database and still leverage almost all the API functionality of AgoRapide).
AgoRapide's data storeage uses the Entity-Attribute-Value (EAV) table concept, meaning everything is stored as a single table. It still supports relations and some relational integrity through the database layer though, but there are of course obvious tradeoffs to consider. 

## Populating object properties is a repetition:
AgoRapide populates object properties through a general Property collection eliminating the need for specific setters and getters for each and every property. 

## Validating properties is a repetition:
Validation (and cleaning up of user input) in AgoRapide is data driven and may be be injected at any stage, when parsing user input, when communicating with other systems or when reading from database. In general the validating logic is given as System.Attribute-properties for the corresponding enum describing a property meaning it is easily "within reach" everywhere in the code.

## Doing unit tests, showing example methods, providing bulk API-interface and writing documentation and tool tips separately is a repetition:
AgoRapide combines all these tasks into one, meaning that once you have written the unit test you have also documented the system and given real world examples for use. In addition, once a property is defined, its corresponding tool tip automatically shows up in the administrative HTML view.

## Branches is a repetition:
(optional for you to apply). Instead of multiple branches in your source code repository you may mark different properties in AgoRapide as "development", "test", "production" and the system will automatically hide features in test or development environment as needed. Note that this may be done even for individual entities, that is, you may have some of your customers test functionality in your production environment for instance. 

## Environments is a repetition:
(optional for you to apply). Different runtime environments like development, test and production is a repetition. With AgoRapide we strive to at least make these environments as identical as possible in the sense that the same source code can run in all environments. This means less work in administering environments.

## Creating database, API and client UI is a repetition:
With a single-table database (optional) your database schema will in practise never change. For the client user interface (UI) there are currently some auto-generated code libraries available (in Java and ObjectiveC). We are looking into the possibility of auto-generated code for views also (not currently supported). 

(even this section of course repeats the phrase "is a repetition" multiple times)

#When is AgoRapide useful for your projects?

AgoRapide is useful when you desire:

## Lightweight reports.
With Easy manual querying and updates (light)
 through REST API in web browser (instead of SQL statements). You can for instance give your users a controlled SQL-like access to data without having to write special reports. 
Example is ad-hoc reports like: api/Customer/WHERE first_name LIKE 'John%'/Property/phone_number. This would correspond to SELECT phone_number FROM customers WHERE first_name LIKE 'John%' in an traditional system (but with AgoRapide anybody can "write" such reports and yet still the access is controlled through the APIs general access control mechanism). 

## Easy change of entity model. 
The entity model can be changed very rapidly. The database schema will usually not change as you change by using SELECT UPDATE instead of CREATE TABLE / ALTER TABLE.
Example: UPDATE p SET name = 'colour' WHERE name = 'color' together with a simple rename of enum P.color to P.colour in the C# code. You do not need to implement specific accessors in your objects. Properties are accessed in a strongly static typed fasion like car.PAs<P_colour> (PAs is shorthand for "property as..."). Changes in relations can likewise be adjusted through UPDATE statements. 

## Small budgets / short time to market.
You do not have to budget separately for administrative tools / support department tools or similar.
Unit tests, documentation, examples and bulk updates through API are all treated as a single concept and kept close to the actual C# code.

## Extensive logging (with actual data), exception handling and helpful _relevant_ links to documentation in error messages.
Excerpt of typical exception message:
TODO: INSERT LIVE URL HERE
Note how actual data in both exception message and log greatly helps in pinpointing problem.

## A history of all changes to database
AgoRapide does not even support the concept of deletion. Properties are only added to the database, with older properties marked as invalid. The entity (the user) doing the change is also logged telling you who change what data.

## A C# centric approach. 
AgoRapide is more C# centric than database centric in the sense that the database schema is in reality stored as C# code. Auto generated libraries are then used as basis for access through other programming environment (like Java, Swift, ObjectiveC and so on). 
The database engine, although still a relational one, is thereby utilized in quite a simple manner.

#Has AgoRapide been used for real?
A philosophy similar to AgoRapide has been used with success in projects where:
The entity model is relatively simple (10-20 types of entities). 
Each entity has lots of field associated with it (no need to implement accessors for each and every field). 
Entity fields rapidly change name or character as you develop (changing names is extremely simple for instance). 
The total number of fields is around one thousand. 
The database contains from thousands up to tens of millions of rows. 
Data is more often read from database than written. 
Most of the data fits inside the servers RAM making the aggresive cache mechanism very effective. 

# When should I not use AgoRapide?
AgoRapide is probably less useful if:
You prefer traditional ways of thinking (but you would probably not have read all the way down to this paragraph anyway in such a case)
Your datamodel is very stable from the beginning.
Your organisation has sufficient resources for separate departments for development, testing, documentation and administration.
The datamodel has lots of relations (it might or it might not be practical to use AgoRapide, we just have not tested it yet). http://pansl.org may help with advanced data modelling, this is a try-as-you-go data modelling tool developed by the same author as AgoRapide originally. 
There is a need for storing hundreds of millions of rows in your database (again, this has just not been tested as of Dec 2016. Tens of millions has been tested and is quite OK).

# How do I start with AgoRapide?
Clone our AgoRapideSample project and link to AgoRapide library in order to get started.
TODO: As of March 2017 we have not worked out how to automate this yet. Contributions are welcome!

# Future development.

AgoRapide as Nuget package
Support for OAuth 2.0
Support for other databases

# FAQ
## What do you  mean by HTML administrative view?
A rudimentary interface useful for administrering your backend, API, and doing support. The HTML interface is like a clone of the JSON interface with the addition of useful links and documentation. In practise it may eliminate the need for building a separate application for doing customer support (or for adding support department related functionality into your customer application).

## Is AgoRapide a replacement of WebAPI?
No, it is built on top of WebAPI. We use as much as possible of the standard functionality in WebAPI, especially the routing mechanism (not attribute based but classic routing). Also, the signature of your controller methods will be familiar. We are Not that fANCY.

## Can I use AgoRapide for other things than a WebAPI?
Yes absolutely. The BCore / BData components may be used in any kind of .NET application, for instance in a kind of listener / always-on application constantly monitoring your WebAPI database or an MQTT server communicating with your app or similar.
 
## Is AgoRapide technically complicated? 
We strive to keep the code as simple as possible. For instance there is no use of reflection in the code. Use of generics is also kept to a minimum, with only a few generic types, like <TProperty> used throughout. 

## Is AgoRapide easy to get started with?
Yes. You need very little boilerplate code to get started. We have also striven to make all error messages VERY detailed, always pinpointing the exact cause of any problems. Use AgoRapideSample as a starting point and you should be up and running with your first API in minutes.

## Other interesting aspects about AgoRapide?
Yes, of course! A particular useful feature is impersonating users through the "entity-to-represent" and "represented-by-entity" concept. With this concept the API can give the view of one API client (user) based on the credentials of another API-client (administrative user). In practise this means that your support department may see exactly the same data as your customer sees in your application, without the customer having to give away his / her / its password. 

## What is AgoRapide.com?
AgoRapide.com is an earlier implementation of the same idea as the AgoRapide C# library. It uses a new schema language (see pansl.com) from which a dynamic HTML interface is generated. AgoRapide.com is also able to generate C# code. The properties as given in the schema language corresponds closely to the recommended <TProperty> implementation in AgoRapide in the sense that as much as possible of the application logic is stored there.

## How is the performance of the AgoRapide library? 
AgoRapide delivers very good performance. Apart from initialization there is very little code actually being executed when you run an application based on the AgoRapide library. The call stack imposes by AgoRapide is very short. Entity properties however are read from a dictionary, instad of directly. This could be an issue if you intensively use certain properties repeatedly. In such cases you can always implement traditional cached properties though as needed.

## What security features does AgoRapide has?
There is a focus on offering user friendliness if you so desire. This means that you have the choice of having only the basic authorization done by an "outside" mechanism like Basic Authentication, OAuth or similar. You may choose to have further authorization like distinguishing between ordinary users and administrative users of your application done within the library. This gives easy administration and friendly error messages. Use with caution however!

## How does AgoRapide relate to Entity Framework and Razor?
AgoRapide does not use any of these concepts. AgoRapide could be considered a lightweight, more data-driven variant of Entity Framework in the sense that classes are defined as annotated enums (instead of traditional properties) and those annotations again are used as the basis for building the database and the user interface. (In reality this means that if you find that Entity Framework works well for you then you will probably not have any use of AgoRapide.)

## Why does AgoRapide not use .NET's inbuilt attribute routing?
It is on our todo-list. We are concerned though about how to implement client-friendly error messages for invalid parameter values.

# How much does AgoRapide automate for me?
AgoRapide utilizes enums to a great extent for describing valid values for different properties. 
TODO: EXPAND ON THIS SECTION.
