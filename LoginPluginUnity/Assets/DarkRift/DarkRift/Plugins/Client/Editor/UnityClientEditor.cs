using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Net;

namespace DarkRift.Client.Unity
{
    [CustomEditor(typeof(UnityClient))]
    [CanEditMultipleObjects]
    public class UnityClientEditor : Editor
    {
        UnityClient client;
        string address;
        SerializedProperty port;
        SerializedProperty ipVersion;
        SerializedProperty autoConnect;
        SerializedProperty invokeFromDispatcher;
        SerializedProperty sniffData;

        void OnEnable()
        {
            client = ((UnityClient)serializedObject.targetObject);

            address     = client.Address.ToString();
            port        = serializedObject.FindProperty("port");
            ipVersion   = serializedObject.FindProperty("ipVersion");
            autoConnect = serializedObject.FindProperty("autoConnect");
            invokeFromDispatcher
                        = serializedObject.FindProperty("invokeFromDispatcher");
            sniffData   = serializedObject.FindProperty("sniffData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Display IP address
            address = EditorGUILayout.TextField(new GUIContent("Address", "The address the client will connect to."), address);

            try
            {
                client.Address = IPAddress.Parse(address);
            }
            catch (FormatException)
            {
                EditorGUILayout.HelpBox("Invalid IP address.", MessageType.Error);
            }

            EditorGUILayout.PropertyField(port);
            
            //Draw IP versions manually else it gets formatted as "Ip Version" and "I Pv4" -_-
            ipVersion.enumValueIndex = EditorGUILayout.Popup(new GUIContent("IP Version", "The IP protocol version to connect using."), ipVersion.enumValueIndex, Array.ConvertAll(ipVersion.enumNames, i => new GUIContent(i)));
            
            EditorGUILayout.PropertyField(autoConnect);

            //Alert to changes when this is unticked!
            bool old = invokeFromDispatcher.boolValue;
            EditorGUILayout.PropertyField(invokeFromDispatcher);

            if (invokeFromDispatcher.boolValue != old && !invokeFromDispatcher.boolValue)
            {
                invokeFromDispatcher.boolValue = !EditorUtility.DisplayDialog(
                    "Danger!",
                    "Unchecking " + invokeFromDispatcher.displayName + " will cause DarkRift to fire events from the .NET thread pool, unless you are confident using multithreading with Unity you should not disable this. Are you 100% sure you want to proceed?",
                    "Yes",
                    "No (Save me!)"
                );
            }

            EditorGUILayout.PropertyField(sniffData);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
