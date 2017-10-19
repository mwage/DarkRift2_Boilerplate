# DarkRift2_Boilerplate

Boilerplate for new Darkrift projects. Free to use, copy and change to suit your needs.

I'm using MongoDB for this project, but adapting things to SQL shouldn't be all that difficult. For Authentication I'm using RSA and BCrypt, but for actual live projects outside of testing, you might want to use f.e. OAuth2 to not have to deal with storing user data yourself. I take no responsibility for any security issues. Type /join (name) or /leave (name) to join/leave a chatgroup and use tab/enter/esc to navigate in the chat window.

Example: https://www.youtube.com/watch?v=IvHqSiPhJiM

### Instructions:

1) Clone/Download the project.
2) Open the Unity folder with Unity.
3) Import the Darkrift 2 package.
4) Extract the "DarkRift Server.zip" that got added to the Assets/DarkRift folder to a location outside the Unity project.
5) Inside the "DarkRift Server" folder, create a new folder called "Plugins".
6) Open and run the Plugins/GenerateRsaKeys solution. This creates PrivateKey.xml and PublicKey.xml.
7) Copy PrivateKey.xml into DarkRift Server/Plugins and PublicKey.xml into the Assets/Resources folder in the Unity project (might have to create the folder).
8) Open the Plugins/Plugins.sln solution.
9) Add the DarkRift and DarkRift.Server references from DarkRiftServer/Lib to all 4 projects.
10) Install the MongoDB driver package with the NuGet package manager into Login and DBConnector projects.
11) You might have to install the BCrypt-Official Nuget package for the Login Project.
12) Build the solution.
13) Copy DbConnector.dll, Login.dll, Chat.dll and Rooms.dll into DarkRiftServer/Plugins and the 3 Mongo, the System.Runtime and the BCrypt dll into DarkRiftServer/lib. You can find all of them in the Debug/bin folder of the Chat project.

You should be able to run the server without errors now. 
Ignore the warning, it just reminds you that the connection path is set to default ("mongodb://localhost:27017").

With that the setup should complete. Install MongoDB if you don't have it and run mongod.exe.
Run the DarkRift.Server.Console.exe and run the Launcher in your Unity scene.
