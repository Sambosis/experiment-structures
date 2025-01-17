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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using Rotorz.Games.Reflection;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEditor;
#endif

namespace ExperimentStructures
{
    /// <summary>
    /// A trial consists of multiple phases, added as child GameObjects with the
    /// Phase component
    /// </summary>
    [DefaultExecutionOrder(-801)]
    public abstract class Trial : Structure
    {
        [SerializeField] protected int repetitions = 1;
        [SerializeField] [HideInInspector] private int currentRepetition;
        [SerializeField] [HideInInspector] private GameObject currentPhaseGameObject;

        private List<Phase> _phases;
        private int _state;
        private bool _trialComplete;

        [HideInInspector] public Block block;

        public Phase NextPhase
        {
            get
            {
                if (_state == _phases.Count) return null;

                return _phases[_state + 1];
            }
        }

        public Phase CurrentPhase => (_state >= _phases.Count) ? _phases[_phases.Count - 1] : _phases[_state];

        public int NumPhases => _phases.Count;

        public int Repetitions
        {
            get => repetitions;
            set => repetitions = value > 0 ? value : 1;
        }

        private bool _endless;

        public bool Endless
        {
            get => _endless;
            set
            {
                _endless = value;
                if (value)
                {
                    repetitions = 0;
                }
            }
        }

        public List<Phase> Phases => _phases;

        public int CurrentRepetition => currentRepetition;

        private void Awake()
        {
            _phases = new List<Phase>(GetComponentsInChildren<Phase>());
            _phases.RemoveAll(CheckPhaseDisabled);

            if (_phases.Count == 0) gameObject.SetActive(false);

            foreach (var phase in _phases)
            {
                phase.trial = this;
                phase.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (block == null)
            {
                Debug.LogWarning(
                    $"[Experiment Structures] Orphaned Trial {name}. Did you forget to put me under a Block?");
                gameObject.SetActive(false);
            }
        }

        internal void StartTrial()
        {
            _trialComplete = false;
            _state = 0;
            currentRepetition = 0;

            OnTrialBegin();
            OnNextRepetition();
        }

        private void Update()
        {
            if (_trialComplete) return;
            
            if (_state >= _phases.Count)
            {
                if (currentRepetition >= repetitions - 1 && !Endless)
                {
                    EndTrial();
                    return;
                }
                
                currentRepetition++;
                OnNextRepetition();
                _state = 0;
                
                if (_trialComplete) return;
            }

            if (!_phases[_state].Alive)
            {
                _phases[_state]._Enter();
                currentPhaseGameObject = _phases[_state].gameObject;
            }

            _phases[_state]._Loop();
        }

        public void EndTrial()
        {
            _trialComplete = true;
            currentPhaseGameObject = null;
            OnTrialComplete();
            block.TrialComplete(this);
        }

        public void PhaseComplete(Phase phase)
        {
            if (phase == _phases[_state])
            {
                _state++;
                block.PhaseComplete(phase);
            }
            else
            {
                Debug.LogError("[Experiment Structures] Invalid phase completion invocation from " + phase.name);
            }
        }

        public void GotoPhase(Phase phase)
        {
            _state = _phases.IndexOf(phase);
            block.PhaseComplete(phase);
        }

        /// <summary>
        /// Do this when this trial starts
        /// </summary>
        protected virtual void OnTrialBegin()
        {
        }

        /// <summary>
        /// Do this when the trial is complete. Useful for clean up or resets.
        /// </summary>
        protected virtual void OnTrialComplete()
        {
        }

        /// <summary>
        /// Implement something that happens before the next repetition
        /// like setting a new tempo or change the color of the next phase.
        /// </summary>
        protected virtual void OnNextRepetition()
        {
        }

        public void ForceNextPhase()
        {
            ExperimentManager.Instance.RaiseNextPhase();
        }

        private static bool CheckPhaseDisabled(Phase phase)
        {
            return !phase.gameObject.activeSelf;
        }

        private void OnValidate()
        {
            if (repetitions < 1)
            {
                Debug.LogError(
                    "[ExperimentStructures] Repetitions must be at least once. Deactivate GameObject to disable this trial.");
                repetitions = 1;
            }
        }

#if UNITY_EDITOR
#pragma warning disable CS0649
        // Disable null warnings for Unity < 2020
        [ClassExtends(typeof(Phase))] [SerializeField] [HideInInspector]
        internal List<ClassTypeReference> phasesToBuild;

        [ClassExtends(typeof(Trial))] [SerializeField] [HideInInspector]
        internal ClassTypeReference replaceTrialWith;
        
        [ClassExtends(typeof(Trial))] [SerializeField] [HideInInspector]
        internal ClassTypeReference addTrial;
#pragma warning restore CS0649
        [SerializeField] [HideInInspector] public bool showPhaseBuilder;
        [SerializeField] [HideInInspector] private float defaultDuration = Mathf.NegativeInfinity;

        public void MakePhases(bool append)
        {
            if (phasesToBuild.Count == 0) return;

            foreach (var reference in phasesToBuild)
                if (reference.Type == null)
                {
                    Debug.LogError("Either specify all Phases or remove unspecified ones.");
                    return;
                }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add Phases");
            var undoGroupIndex = Undo.GetCurrentGroup();

            if (!append)
                foreach (var phase in GetComponentsInChildren<Phase>())
                    Undo.DestroyObjectImmediate(phase.gameObject);

            foreach (var reference in phasesToBuild)
            {
                var go = new GameObject(reference.Type.Name);
                go.AddComponent(reference.Type);
                var phase = go.GetComponent<Phase>();
                phase.SetDuration(defaultDuration);
                go.transform.SetParent(transform);

                Undo.RegisterCreatedObjectUndo(go, "");
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }
        
#endif
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(Trial), true)] 
    public class TrialEditor : Editor
    {
        private static bool _showUtilities;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Monitor", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentRepetition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentPhaseGameObject"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Separator();
            DrawDefaultInspector();

            var trial = (Trial)target;

            EditorGUILayout.Separator();
            _showUtilities = EditorGUILayout.Foldout(_showUtilities, "Utilities", EditorStyles.foldoutHeader);
            if (_showUtilities)
            {
                EditorGUILayout.Separator();
#if UNITY_2020_1_OR_NEWER
                EditorGUILayout.LabelField("Trial Builder", EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("phasesToBuild"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultDuration"));
                if (GUILayout.Button("Append Phases")) trial.MakePhases(true);

                if (GUILayout.Button("Replace Phases")) trial.MakePhases(false);

                EditorGUI.indentLevel = 0;
                EditorGUILayout.Separator();
#endif
                EditorGUILayout.LabelField("Replace Trial", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("replaceTrialWith"));

                serializedObject.ApplyModifiedProperties();

                if (GUILayout.Button("Replace", GUILayout.Width(80f)))
                {
                    var newTrialReference = trial.replaceTrialWith;

                    if (newTrialReference.Type != null)
                    {
                        Undo.IncrementCurrentGroup();
                        Undo.SetCurrentGroupName("Replace Phase");
                        var undoGroupIndex = Undo.GetCurrentGroup();

                        var go = trial.gameObject;

                        Undo.DestroyObjectImmediate(trial.gameObject.GetComponent<Trial>());

                        var newTrial = (Trial)Undo.AddComponent(go, newTrialReference.Type);
                        newTrial.name = newTrialReference.Type.Name;
                        Undo.CollapseUndoOperations(undoGroupIndex);

                        return;
                    }
                }
                GUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Add Trial to End of Block", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("addTrial"));

                serializedObject.ApplyModifiedProperties();

                if (GUILayout.Button("Add", GUILayout.Width(80f)))
                {
                    var newTrialReferenceType = trial.addTrial;

                    if (newTrialReferenceType.Type != null)
                    {
                        Undo.IncrementCurrentGroup();
                        Undo.SetCurrentGroupName("Add Trial");
                        var undoGroupIndex = Undo.GetCurrentGroup();

                        var go = new GameObject(newTrialReferenceType.Type.Name);
                        go.AddComponent(newTrialReferenceType.Type);
                        go.transform.SetParent(trial.transform.parent);

                        Undo.RegisterCreatedObjectUndo(go, "");

                        Undo.CollapseUndoOperations(undoGroupIndex);
                    }
                }
                GUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
                if (GUILayout.Button("Force Next Phase")) trial.ForceNextPhase();
            }
        }

        #region Update Icon

        private static MethodInfo CopyMonoScriptIconToImporters =
            typeof(MonoImporter).GetMethod("CopyMonoScriptIconToImporters",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo SetIconForObject =
            typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo GetIconForObject =
            typeof(EditorGUIUtility).GetMethod("GetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);

        private void Reset()
        {
#if UNITY_2021_2_OR_NEWER
            if (EditorGUIUtility.GetIconForObject(target) != null) return;
#else
            if (GetIconForObject.Invoke(null, new[] { target }) != null) return;
#endif

            Debug.LogWarning("[Experiment Structures] Assigning icon to new Trial. Reimporting Asset. Please wait.");

            var icon =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    AssetDatabase.GUIDToAssetPath("9e234cd8b18ef4d8a846af332590950f"));
#if UNITY_2021_2_OR_NEWER
            EditorGUIUtility.SetIconForObject(target, icon);
#else
            SetIconForObject.Invoke(null, new[] { target, icon });
#endif
            var behaviour = target as MonoBehaviour;
            var script = MonoScript.FromMonoBehaviour(behaviour);
            CopyMonoScriptIconToImporters.Invoke(null, new[] { script });
        }

        #endregion
    }
#endif
}