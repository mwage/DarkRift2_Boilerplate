# DarkRift2_Bolierplate

Currently adding Friend-, Chat- and Roomsystem.









Login Plugin + MongoDB Connector for DarkRift 2

Free to use, copy and change to suit your needs.

This LoginPlugin uses MongoDB. If you want to use SQL, you'll have to change the DbConnector to something similar the old 
DR1 MySQL Connector and expose the Database as a variable to access from other plugins instead of the collections.

For security it uses and RSA and BCrypt. When a user connects, the LoginPlugin creates a new public/private key pair, 
stores the private key (for decryption) in a Dictionary and sends the public key (for encryption) to the user. 

The user encrypts his password with the public key, sends it to the server, where the LoginPlugin can decrypt it with the private key 
and either salt-hashes it with BCrypt before storing it in the database or just verifys it (BCrypt.Verify()).

This should make it a lot more secure than the old DR1 one, but if you want to actually use it outside of basic testing, 
you might want to revise the security and make sure everything is really as secure as the needed to meet your demands. For example you'll need to address the lack of identification in it's current state (I might include something on this later on).

I take no responsibility for any complications/problems caused by use of this plugin.


### Instructions:

1) Clone/Download the project.
2) Open LoginPluginUnity with Unity
3) Import the Darkrift 2 package.
4) Open the Launcher Scene and make sure the UnityClient Script is attached to the GameManager Object
5) Extract the "DarkRift Server.zip" that got added to the Assets/DarkRift folder to a location outside the Unity project.
6) Inside the "DarkRift Server" folder, create a new folder called "Plugins"
7) Open the Plugins/Plugins.sln solution
8) Add the DarkRift and DarkRift.Server references from DarkRiftServer/Lib to both projects
9) Install MongoDB package with the NuGet package manager into both projects (just use the first one with 1million+ downlaods)
10) You might have to install the BCrypt Nuget package for the Login Project (I used BCrypt-Official)
11) Add the DbConnector reference to the Login Project
12) Build the solution
13) From the Login/bin/Debug folder: Copy DbConnector.dll and Login.dll into DarkRiftServer/Plugins and the 3 Mongo, the System.Runtime and the BCrypt dll into DarkRiftServer/lib

You should be able to run the server without errors now. 
Ignore the warning, it just reminds you that the connection path is set to default ("mongodb://localhost:27017").

With that the setup should complete. Install MongoDB if you don't have it and run mongod.exe.
Run the DarkRift.Server.Console.exe and run the Launcher in your Unity scene and you should be able to Register/Login/Logout.
