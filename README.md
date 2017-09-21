# DarkRift2_Login-Plugin
Login Plugin + MongoDB Connector for DarkRift 2

Free to use, copy and change to suit your needs.

This LoginPlugin uses MongoDB. If you want to use SQL, you'll have to change the DbConnector to something similar the old 
DR1 MySQL Connector and expose the Database as a variable to access from other plugins instead of the collections.

For security it uses and RSA and BCrypt. When a user connects, the LoginPlugin creates a new public/private key pair, 
stores the private key (for decryption) in a Dictionary and sends the public key (for encryption) to the user. 

The user encrypts his password with the public key, sends it to the server, where the LoginPlugin can decrypt it with the private key 
and either salt-hashes it with BCrypt before storing it in the database or just verifys it (BCrypt.Verify()).

This should make it a lot more secure than the old DR1 one, but if you want to actually use it outside of basic testing, 
you might want to revise the security and make sure everything is really as secure as the needed to meet your demands.

I take no responsibility for any complications/problems caused by use of this plugin.


### Instructions:

1) Clone/Download the project.
2) Open LoginPluginUnity with Unity
3) Import the Darkrift 2 package.
4) Open the Launcher Scene and make sure the UnityClient Script is attached to the GameManager Object
5) Extract the "DarkRift Server.zip" that got added to the Assets/DarkRift folder to a location outside the Unity project.
6) Inside the DarkRift Server folder, create a new one called "Plugins"
7) Open the DbConnector solution
8) Add the DarkRift and DarkRift.Server references from DarkRiftServer/Lib
9) Install MongoDB package with the NuGet package manager (i just used the first one with 1million+ downlaods
10) Build the solution
11) Move the DbConnector.dll to DarkRiftServer/Plugins and the 3 Mongo .dlls and the System.Runtime one to DarkRiftServer/lib

You should be able to run the server without errors now. 
Ignore the warning, it just reminds you that the connection path is set to default ("mongodb://localhost:27017").

12) open the LoginPlugin solution
13) Import the missing references (you can just import them from DarkRiftServer/Lib
14) You might have to install the BCrypt Nuget package (I used BCrypt-Official)
15) Build the solution
16) Move Login.dll to DarkRiftServer/Plugins and BCrypt.Net.dll to DarkRiftServer/Lib

With that the setup should complete. Install MongoDB if you haven't and run mongod.exe with its default settings.
Run the DarkRift.Server.Console.exe and run the Launcher in your Unity scene and you should be able to Register/Login/Logout.
