Copy contents from AgoRapide/API/Scripts into this folder.

As of Mar 2017 the relevant files are:
  AgoRapide-0.1.js
  jquery-3.1.1.min.js

You may also download these files from GitHub as follows:

and from jquery.com as
  https://code.jquery.com/jquery-3.1.1.min.js

AgoRapide should work with all JQuery versions >= 1.8.2. The default version used is 3.1.1.

If you want to use a different JQuery version than 3.1.1 then remember to change the configuration value for ScriptRelativePaths. This is done when initializing AgoRapide.Core.Configuration in Startup.cs:

    AgoRapide.Core.Util.Configuration = new AgoRapide.Core.Configuration(
        logPath: logPath,
        rootUrl: rootUrl
    ) {
        // Change to different version of JQuery by adding this line:
        // ScriptRelativePaths = new List<string> { "Scripts/AgoRapide-0.1.js", "Scripts/jquery-3.1.1.min.js" },

ScriptRelativePaths decides the <script src="https://...</script> links in HTML result pages.
