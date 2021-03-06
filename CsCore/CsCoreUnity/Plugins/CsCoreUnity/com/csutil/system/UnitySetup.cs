﻿using com.csutil.async;
using com.csutil.http;
using com.csutil.io;
using com.csutil.logging;
using System;
using UnityEngine;

namespace com.csutil {

    public class UnitySetup {

        public const string UNITY_SETUP_DONE = "Unity setup now done";

        static UnitySetup() { // This method is only executed only once at the very beginning 
            // Debug.Log("com.csutil.UnitySetup static constructor called..");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeSceneLoad() {
            Debug.Log("BeforeSceneLoad");
            SystemConsoleToUnityLogRedirector.Setup();
            try { DestroyExistingMainThreadIfNeeded(); } catch (Exception e) { Debug.Log(e); }
        }

        private static void DestroyExistingMainThreadIfNeeded() {
            var mt = IoC.inject.Get<MainThread>(null, false);
            if (mt != null) { mt.gameObject.Destroy(); }
            InjectorExtensionsForUnity.GetOrAddGameObject(InjectorExtensionsForUnity.DEFAULT_SINGLETON_NAME).Destroy();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad() {
            Debug.Log("AfterSceneLoad");
            SetupDefaultSingletonsIfNeeded();
            EventBus.instance.Publish(UNITY_SETUP_DONE);
        }

        public static void SetupDefaultSingletonsIfNeeded() {
            MainThread.instance.enabled = true; // Called to init main thread if not yet done by other logic
            var caller = new object();
            Log.instance = IoC.inject.GetOrAddSingleton<ILog>(caller, () => new LogToUnityDebugLog());
            IoC.inject.GetOrAddSingleton<EnvironmentV2>(caller, () => new EnvironmentV2Unity());
            IoC.inject.GetOrAddSingleton<RestFactory>(caller, () => new UnityRestFactory());
            if (EnvironmentV2.isWebGL) {
                IoC.inject.GetOrAddSingleton<TaskV2>(caller, () => new TaskV2WebGL());
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoaded() { Log.d("Now the scene finished loading"); }

        /// <summary> 
        /// Ensures that the callback is invoked either directly if the UnitySetup already ran or 
        /// after the UnitySetup is fully initialized 
        /// </summary>
        public static void InvokeAfterUnitySetupDone(Action callback) {
            EventBus.instance.SubscribeForOnePublishOrInstantInvokeIfInHistory(UNITY_SETUP_DONE, callback);
        }

    }

}
