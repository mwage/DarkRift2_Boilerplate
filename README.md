# DarkRift2_Boilerplate

This project is meant to give people an idea of how DarkRift 2 works. Feel free for use and change to suit your needs.
For Authentication I'm using RSA and BCrypt. For an actual live project, unless you really know what you do, you might want to use token based authorization like f.e. OAuth/OpenID or 3rd party platforms like PlayFab, so you don't have to deal with sensible user data yourself.
You can use any Database you like, just write a DbConnector (or use an existing one) and write your own queries into the DataLayer (see MongoDb example).

Type /join (name) or /leave (name) to join/leave a chatgroup and use tab/enter/esc to navigate in the chat window.

Example: https://www.youtube.com/watch?v=IvHqSiPhJiM

### Update: Abstracted the database to make adding different ones easier

### Instructions:

1) Clone/Download the project.
2) Open the Unity folder with Unity.
3) Download Darkrift 2 from the Asset Store.
4) Extract the "DarkRift Server.zip" that got added with the Assets/DarkRift folder to a location outside of the Unity project.
5) Inside the "DarkRift Server" folder, create a new folder called "Plugins".
6) Open and run the Plugins/GenerateRsaKeys solution. This creates PrivateKey.xml and PublicKey.xml.
7) Copy PrivateKey.xml into DarkRift Server/Plugins and PublicKey.xml into the Assets/Resources folder in the Unity project (might have to create the folder).
8) In unity, open the "Launcher" scene, select the GameManager object and if one of the scripts is corrupted, replace it with "[YourDarkRiftFolder]\DarkRift\Plugins\Client\UnityClient.cs". You'll have to do this if your DarkRift version is more recent than the one used here.
9) Open the Plugins/Plugins.sln solution.
10) Add the DarkRift and DarkRift.Server references from DarkRiftServer/Lib to all projects
11) Try building the solution. Usually the Nuget Manager should be able to install the Nuget packages by itself. If everything builds fine, jump to 14), otherwise continue with 12).
12) For MongoDB, install the MongoDB driver package with the NuGet package manager for the MongoDbConnector.
13) You might have to install the BCrypt-Official Nuget package for the Login Project.
14) Build the solution
15) Copy DbConnector.dll, Login.dll, Chat.dll and Rooms.dll into DarkRiftServer/Plugins and the 3 Mongo, the System.Runtime and the BCrypt dll into DarkRiftServer/lib. You can find all of them in the Debug/bin folder of the Chat project.

You should be able to run the server without errors now. 

With that the setup should be complete. Start your Db, run the DarkRift.Server.Console.exe and either start the Launcher in your Unity scene or a build of it.

Hope this project is helpful. If you have any problems or find any bugs, let me know.
