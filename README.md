# DarkRift2_Boilerplate

This project is meant to give people an idea of how DarkRift 2 works. Feel free for use and change to suit your needs.

I'm using MongoDB for this project, but adapting things to a different database shouldn't be all that difficult, just write a Db Connector for it and modify the queries. For Authentication I'm using RSA and BCrypt. For an actual live project, unless you really know what you do, you might want to use token based authorization like f.e. OAuth/OpenID or 3rd party platforms like PlayFab, so you don't have to deal with sensible user data yourself. I take no responsibility for any security issues, this project isn't meant as copy/paste solution, but rather to provide an idea of how you can work with DarkRift 2. 

Type /join (name) or /leave (name) to join/leave a chatgroup and use tab/enter/esc to navigate in the chat window.

Example: https://www.youtube.com/watch?v=IvHqSiPhJiM

### Update: Now updated to the official release version of DarkRift 2

### Instructions:

1) Clone/Download the project.
2) Open the Unity folder with Unity.
3) Download Darkrift 2 from the Asset Store.
4) Extract the "DarkRift Server.zip" that got added with the Assets/DarkRift folder to a location outside of the Unity project.
5) Inside the "DarkRift Server" folder, create a new folder called "Plugins".
6) Open and run the Plugins/GenerateRsaKeys solution. This creates PrivateKey.xml and PublicKey.xml.
7) Copy PrivateKey.xml into DarkRift Server/Plugins and PublicKey.xml into the Assets/Resources folder in the Unity project (might have to create the folder).
8) Open the Plugins/Plugins.sln solution.
9) Add the DarkRift and DarkRift.Server references from DarkRiftServer/Lib to all 4 projects.
10) Try building the solution. Usually the Nuget Manager should be able to install the Nuget packages by itself. If everything builds fine, jump to 13), otherwise continue with 11).
11) Install the MongoDB driver package with the NuGet package manager into Login and DBConnector projects.
12) You might have to install the BCrypt-Official Nuget package for the Login Project and build the solution.
13) Copy DbConnector.dll, Login.dll, Chat.dll and Rooms.dll into DarkRiftServer/Plugins and the 3 Mongo, the System.Runtime and the BCrypt dll into DarkRiftServer/lib. You can find all of them in the Debug/bin folder of the Chat project.

You should be able to run the server without errors now. 
Ignore the warning, it just reminds you that the connection path is set to default ("mongodb://localhost:27017").

With that the setup should complete. Install MongoDB if you don't have it and run mongod.exe.
Run the DarkRift.Server.Console.exe and run the Launcher in your Unity scene.

Hope this project will be of help to some. If you have any problems or find any bugs, let me know.
