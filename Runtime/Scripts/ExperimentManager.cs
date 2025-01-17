﻿// MIT License

// Copyright (c) 2021 Jom Preechayasomboon

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ExperimentStructures
{
    [DisallowMultipleComponent]
    public class ExperimentManager : MonoBehaviour
    {
        private static ExperimentManager _instance;

        public static ExperimentManager Instance
        {
            get
            {
                if (!FindObjectOfType<ExperimentManager>())
                {
                    var go = new GameObject("ExperimentManager");
                    _instance = go.AddComponent<ExperimentManager>();
                    return _instance;
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(gameObject);
            else
                _instance = this;
        }

        public delegate void NextPhase();

        public event NextPhase nextPhase;

        public void RaiseNextPhase()
        {
            nextPhase?.Invoke();
        }

        public delegate void StartPhase();

        public event StartPhase startPhase;

        public void RaiseStartPhase()
        {
            startPhase?.Invoke();
        }

        public void ForceNextPhase()
        {
            RaiseNextPhase();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExperimentManager), true)]
    public class ExperimentManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Force Next Phase")) ((ExperimentManager)target).ForceNextPhase();
        }
    }
#endif
}