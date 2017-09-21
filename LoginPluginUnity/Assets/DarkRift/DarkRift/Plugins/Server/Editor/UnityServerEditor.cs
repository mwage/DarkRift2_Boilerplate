using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Net;

namespace DarkRift.Server.Unity
{
    [CustomEditor(typeof(UnityServer))]
    [CanEditMultipleObjects]
    public class UnityClientEditor : Editor
    {
        UnityServer server;

        string address;
        SerializedProperty port;
        SerializedProperty ipVersion;
        SerializedProperty createOnEnable;

        SerializedProperty dataDirectory;

        SerializedProperty useRecommended;
        SerializedProperty logFileString;
        SerializedProperty exceptionsTo;

        bool showLogging, showPlugins, showDatabases;

        void OnEnable()
        {
            server = (UnityServer)serializedObject.targetObject;

            address         = server.Address.ToString();
            port            = serializedObject.FindProperty("port");
            ipVersion       = serializedObject.FindProperty("ipVersion");
            createOnEnable  = serializedObject.FindProperty("createOnEnable");

            dataDirectory   = serializedObject.FindProperty("dataDirectory");

            useRecommended  = serializedObject.FindProperty("useRecommended");
            logFileString
                            = serializedObject.FindProperty("logFileString");

            //TODO log writer matrix
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Display IP address
            address = EditorGUILayout.TextField(new GUIContent("Address", "The address the client will connect to."), address);

            try
            {
                server.Address = IPAddress.Parse(address);
            }
            catch (FormatException)
            {
                EditorGUILayout.HelpBox("Invalid IP address.", MessageType.Error);
            }


            EditorGUILayout.PropertyField(port);

            //Draw IP versions manually else it gets formatted as "Ip Version" and "I Pv4" -_-
            ipVersion.enumValueIndex = EditorGUILayout.Popup(new GUIContent("IP Version", "The IP protocol version the server will listen on."), ipVersion.enumValueIndex, Array.ConvertAll(ipVersion.enumNames, i => new GUIContent(i)));

            EditorGUILayout.PropertyField(createOnEnable);

            EditorGUILayout.PropertyField(dataDirectory);

            if (showLogging = EditorGUILayout.Foldout(showLogging, "Logging Setttings"))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(useRecommended);

                EditorGUI.indentLevel++;
                if (useRecommended.boolValue)
                    EditorGUILayout.PropertyField(logFileString);
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
            }

            if (showPlugins = EditorGUILayout.Foldout(showPlugins, "Plugin Setttings"))
            {
                EditorGUI.indentLevel++;

                // Manually search the assembly and create a selection list of plugin types
                EditorGUILayout.HelpBox("These are the plugins that we found in your project, those you tick will be loaded into the DarkRift server when the component is loaded.", MessageType.Info);


                List<Type> pluginTypes = new List<Type>();

                Assembly searchAssembly = Assembly.GetAssembly(typeof(UnityServer));
                foreach (Type type in searchAssembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Plugin)))
                    {
                        if (EditorGUILayout.Toggle(type.Name, server.PluginTypes != null && server.PluginTypes.Contains(type)))
                            pluginTypes.Add(type);
                    }
                }

                server.PluginTypes = pluginTypes.ToArray();
                
                EditorGUI.indentLevel--;
            }

            //Draw databases manually
            if (showDatabases = EditorGUILayout.Foldout(showDatabases, "Databases"))
            {
                List<ServerSpawnData.DatabaseSettings.DatabaseConnectionData> databases = new List<ServerSpawnData.DatabaseSettings.DatabaseConnectionData>(server.Databases);

                EditorGUI.indentLevel++;
                for (int i = 0; i < databases.Count; i++)
                {
                    ServerSpawnData.DatabaseSettings.DatabaseConnectionData database = databases[i];

                    database.Name = EditorGUILayout.TextField("Name", database.Name);

                    database.ConnectionString = EditorGUILayout.TextField("Connection String", database.ConnectionString);

                    Rect removeRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());        //So indent level affects the button
                    if (GUI.Button(removeRect, "Remove"))
                    {
                        databases.Remove(database);
                        i--;
                    }

                    EditorGUILayout.Space();
                }

                Rect addRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true));
                if (GUI.Button(addRect, "Add Database"))
                    databases.Add(new ServerSpawnData.DatabaseSettings.DatabaseConnectionData("NewDatabase", "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"));

                EditorGUI.indentLevel--;

                server.Databases = databases.ToArray();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
