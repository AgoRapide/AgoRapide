The AgoRapide\bin folder contains the binary files for the libraries referred to in AgoRapide, other than "standard" .NET libraries. 

By using "static" binaries we ensure a stable enviroment where future changes to any library will not affect your application. 

This approach is somewhat unconvential. You may want to delete these files from your project and instead use the more convential NuGet method for fetching packages instead.

As of Dec 2016 the files are:
Npgsql.dll v 3.1.9
