﻿/*
// MIT License
//
// Copyright (c) 2020 Erik Eriksson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
*/

using System;
using UnityEditor;
using UnityEngine;

namespace ExperimentStructures
{
    /// <summary>
    /// Draws the generic dictionary a bit nicer than Unity would natively (not as many expand-arrows
    /// and better spacing between KeyValue pairs). Also renders a warning-box if there are duplicate
    /// keys in the dictionary.
    /// </summary>
    [CustomPropertyDrawer(typeof(GenericDictionary<,>))]
    public class GenericDictionaryPropertyDrawer : PropertyDrawer
    {
        static float lineHeight = EditorGUIUtility.singleLineHeight;
        static float vertSpace = EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            // Draw list.
            var list = property.FindPropertyRelative("list");
            string fieldName = ObjectNames.NicifyVariableName(fieldInfo.Name);
            var currentPos = new Rect(lineHeight, pos.y, pos.width, lineHeight);
            EditorGUI.PropertyField(currentPos, list, new GUIContent(fieldName), true);

            // Draw key collision warning.
            var keyCollision = property.FindPropertyRelative("keyCollision").boolValue;
            if (keyCollision)
            {
                currentPos.y += EditorGUI.GetPropertyHeight(list, true) + vertSpace;
                var entryPos = new Rect(lineHeight, currentPos.y, pos.width, lineHeight * 2f);
                EditorGUI.HelpBox(entryPos, "Duplicate keys will not be serialized.", MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totHeight = 0f;

            // Height of KeyValue list.
            var listProp = property.FindPropertyRelative("list");
            totHeight += EditorGUI.GetPropertyHeight(listProp, true);

            // Height of key collision warning.
            bool keyCollision = property.FindPropertyRelative("keyCollision").boolValue;
            if (keyCollision)
            {
                totHeight += lineHeight * 2f + vertSpace;
            }

            return totHeight;
        }
    }
}