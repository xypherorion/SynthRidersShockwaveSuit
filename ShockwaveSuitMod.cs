using System;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using HarmonyLib;
using Util.Audio;
using System.IO;
using MiKu.NET.Charting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace ShockwaveSuit {
    public class ShockwaveSuitMod : MelonMod {
        public static ShockwaveSuitMod cs_instance;
        public static ShockwaveManager suit = ShockwaveManager.Instance;

        public enum EventSelect {
            hit,
            miss,
            rail,
            special,
            specialpass,
            specialfail,
            maxcombo,
            wall,
            buttonclick,
            buttonhover,
            gameover,
            resultbgm,
            ambient,
            applause
        }

        /*
        [HarmonyPatch(typeof(Util_HitSFXSource), "Awake")]
        public static class OverwriteHitSFX
        {
            private static void Postfix()
            {
                MelonLogger.Msg("Setting Hit SFX");
                var cs_instance = new ShockwaveSuitMod();
                Type classType = typeof(Util_HitSFXSource);
                Util_HitSFXSource instance = Util_HitSFXSource.s_instance;
                //FieldInfo pathField = null;

                MelonCoroutines.Start(cs_instance.GetAudioClip(hitFilePath, EventSelect.hit));
                MelonCoroutines.Start(cs_instance.GetAudioClip(missFilePath, EventSelect.miss));
                //MelonCoroutines.Start(instance.GetAudioClip(railFilePath, SfxSelect.rail));
                MelonCoroutines.Start(cs_instance.GetAudioClip(specialFilePath, EventSelect.special));
                MelonCoroutines.Start(cs_instance.GetAudioClip(specialpassFilePath, EventSelect.specialpass));
                MelonCoroutines.Start(cs_instance.GetAudioClip(specialfailFilePath, EventSelect.specialfail));
                MelonCoroutines.Start(cs_instance.GetAudioClip(maxmultilierFilePath, EventSelect.maxcombo));
                MelonCoroutines.Start(cs_instance.GetAudioClip(wallFilePath, EventSelect.wall));
            }
        }

        [HarmonyPatch(typeof(ExtraSFXAudioController), "Awake")]
        public static class OverwriteXSFX
        {
            private static void Postfix()
            {
                MelonLogger.Msg("Setting Menu SFX");
                var cs_instance = new ShockwaveSuitMod();
                Type classType = typeof(ExtraSFXAudioController);
                MelonCoroutines.Start(cs_instance.GetAudioClip(buttonclickFilePath, classType, "buttonClickClip"));
                MelonCoroutines.Start(cs_instance.GetAudioClip(buttonhoverFilePath, classType, "buttonHoverClip"));
            }
        }
        */


        public static List<ShockwaveManager.HapticGroup> LeftArmEffect = new List<ShockwaveManager.HapticGroup>() { 
            ShockwaveManager.HapticGroup.LEFT_FOREARM,
            ShockwaveManager.HapticGroup.LEFT_ARM,
            ShockwaveManager.HapticGroup.LEFT_BICEP,
            ShockwaveManager.HapticGroup.LEFT_SHOULDER,
            ShockwaveManager.HapticGroup.LEFT_CHEST,
            ShockwaveManager.HapticGroup.LEFT_TORSO,
            ShockwaveManager.HapticGroup.LEFT_WAIST,
            ShockwaveManager.HapticGroup.LEFT_THIGH,
            ShockwaveManager.HapticGroup.LEFT_LOWER_LEG,
            ShockwaveManager.HapticGroup.LEFT_CALF
        };

        public static List<ShockwaveManager.HapticGroup> RightArmEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.RIGHT_FOREARM,
            ShockwaveManager.HapticGroup.RIGHT_ARM,
            ShockwaveManager.HapticGroup.RIGHT_BICEP,
            ShockwaveManager.HapticGroup.RIGHT_SHOULDER,
            ShockwaveManager.HapticGroup.RIGHT_CHEST,
            ShockwaveManager.HapticGroup.RIGHT_TORSO,
            ShockwaveManager.HapticGroup.RIGHT_WAIST,
            ShockwaveManager.HapticGroup.RIGHT_THIGH,
            ShockwaveManager.HapticGroup.RIGHT_LOWER_LEG,
            ShockwaveManager.HapticGroup.RIGHT_CALF
        };

        public static List<ShockwaveManager.HapticGroup> TwoHandSpecialEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.SHOULDERS_FRONT,
            ShockwaveManager.HapticGroup.SHOULDERS,
            ShockwaveManager.HapticGroup.SHOULDERS_BACK,
            ShockwaveManager.HapticGroup.CHEST_FRONT,
            ShockwaveManager.HapticGroup.CHEST,
            ShockwaveManager.HapticGroup.CHEST_BACK,
            ShockwaveManager.HapticGroup.TORSO_FRONT,
            ShockwaveManager.HapticGroup.TORSO,
            ShockwaveManager.HapticGroup.TORSO_BACK
        };


        public static List<ShockwaveManager.HapticGroup> OneHandSpecialEffect = new List<ShockwaveManager.HapticGroup>() {
            ShockwaveManager.HapticGroup.SHOULDERS_FRONT,
            ShockwaveManager.HapticGroup.CHEST_FRONT,
            ShockwaveManager.HapticGroup.TORSO_FRONT,
            ShockwaveManager.HapticGroup.TORSO,
            ShockwaveManager.HapticGroup.TORSO_BACK
        };

        public async static Task PlayPulse(List<ShockwaveManager.HapticGroup> pulseList) {
            while (pulseList.Count > 0) {
                ShockwaveManager.Instance?.SendHapticGroup(pulseList[0], 1.0f, SuitPreferences.pulseRate.Value);
                pulseList.RemoveAt(0);

                await Task.Delay(SuitPreferences.pulseRate.Value);
            }
        }

        //static Color pulseColor = new Color();
        static int[] ledIdx = new int[] { 0 };
        static float[] ledColor = new float[] { 0.0f, 0.0f, 0.0f };

        protected static void ColorToFloatArray(Color color) {
            ledColor[0] = color.r * color.a;
            ledColor[1] = color.g * color.a;
            ledColor[2] = color.b * color.a;
        }

        [HarmonyPatch(typeof(Game_ControllIndicator), "CheckNoteCollition")]
        public static class PostFixGame_Note_CheckNoteCollition {
            private static void Postfix(Game_ControllIndicator __instance, Game_Note ___gameNote) {
                //MelonLogger.Msg("Check Note Collition");
                
                switch((SuitPreferences.HapticsResponseMode)SuitPreferences.hapticMode.Value) {
                    case SuitPreferences.HapticsResponseMode.OnMiss:
                        if(___gameNote.NoteWasFailed) {
                            switch (___gameNote.NoteRef.Type) {
                                case Note.NoteType.LeftHanded:
                                    Task.Run(() => PlayPulse(LeftArmEffect));
                                    if (SuitPreferences.ledResponse.Value) {
                                        ledIdx[0] = 4;
                                        ColorToFloatArray(___gameNote.NoteColor);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                    break;
                                case Note.NoteType.RightHanded:
                                    Task.Run(() => PlayPulse(RightArmEffect));
                                    if (SuitPreferences.ledResponse.Value) {
                                        ledIdx[0] = 1;
                                        ColorToFloatArray(___gameNote.NoteColor);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                    break;
                                case Note.NoteType.OneHandSpecial:
                                    Task.Run(() => PlayPulse(OneHandSpecialEffect));
                                    if (SuitPreferences.ledResponse.Value) {
                                        ledIdx[0] = 0;
                                        ColorToFloatArray(___gameNote.NoteColor);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                    break;
                                case Note.NoteType.BothHandsSpecial:
                                    Task.Run(() => PlayPulse(TwoHandSpecialEffect));
                                    if (SuitPreferences.ledResponse.Value) {
                                        ledIdx[0] = 2;
                                        ColorToFloatArray(___gameNote.NoteColor);
                                        ShockwaveManager.Instance?.sendLEDUpdate(ledIdx, ledColor, 1);
                                    }
                                    break;
                                case Note.NoteType.SeparateHandSpecial:
                                    break;
                                default:
                                    break;
                            }
                        }
                        return;
                    case SuitPreferences.HapticsResponseMode.OnHit:
                        if(!___gameNote.NoteWasFailed) {
                            switch (___gameNote.NoteRef.Type) {
                                case Note.NoteType.LeftHanded:
                                    Task.Run(() => PlayPulse(LeftArmEffect));
                                    break;
                                case Note.NoteType.RightHanded:
                                    Task.Run(() => PlayPulse(RightArmEffect));
                                    break;
                                case Note.NoteType.OneHandSpecial:
                                    Task.Run(() => PlayPulse(OneHandSpecialEffect));
                                    break;
                                case Note.NoteType.BothHandsSpecial:
                                    Task.Run(() => PlayPulse(TwoHandSpecialEffect));
                                    break;
                                case Note.NoteType.SeparateHandSpecial:
                                    break;
                                default:
                                    break;
                            }
                        }
                        return;
                }
            }
        }

        CancellationTokenSource pauseTokenSource = new CancellationTokenSource();
        Task pauseTask;

        float pauseCycleDir = 1.0f;
        int[] pauseLeds = new int[] { 0, 1, 2, 3, 4, 5 };
        float[] pauseColors = new float[] {
                1.0f, 0.0f, 0.0f,
                1.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 1.0f
        };

        protected async Task OnPause() {
            System.DateTime FlipDirectionTime = System.DateTime.UtcNow.AddMilliseconds(SuitPreferences.pulseRate.Value);
            int wrapLed = 0, i = 0;
            while (true) {
                if (SuitPreferences.ledResponse.Value) {
                    ShockwaveManager.Instance?.sendLEDUpdate(pauseLeds, pauseColors, 6);
                    if (pauseCycleDir > 0.0f) {
                        wrapLed = pauseLeds[0];
                        for (i = 0; i < 5; i++)
                            pauseLeds[i] = pauseLeds[i + 1];
                        pauseLeds[5] = wrapLed;
                    } else {
                        wrapLed = pauseLeds[5];
                        for (i = 1; i < 6; i++)
                            pauseLeds[i] = pauseLeds[i - 1];
                        pauseLeds[0] = wrapLed;
                    }

                    if (System.DateTime.UtcNow > FlipDirectionTime) {
                        pauseCycleDir = -pauseCycleDir;
                        FlipDirectionTime = System.DateTime.UtcNow.AddMilliseconds(SuitPreferences.pulseRate.Value);
                    }
                }
                await Task.Delay(SuitPreferences.pulseRate.Value);
            }
        }

        public void SongPaused() {
            if (pauseTask == null)
                pauseTask = Task.Run(() => OnPause(), pauseTokenSource.Token);
        }

        public void SongUnPaused() {
            if (pauseTask != null) {
                pauseTokenSource.Cancel();

                // Just continue on this thread, or await with try-catch:
                try {
                    pauseTask.GetAwaiter().GetResult();
                } catch (OperationCanceledException e) {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                } finally {
                    pauseTokenSource.Dispose();
                }
            }
        }

        [HarmonyPatch(typeof(GameControlManager), "Awake")]
        public static class OverwriteGCM {
            private static void Postfix() {
                var cs_instance = new ShockwaveSuitMod();
                Type classType = typeof(GameControlManager);
                GameObject managerVRTK = GameObject.Find("VRTKManager");
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
            base.OnSceneWasInitialized(buildIndex, sceneName);
        }

        public override void OnApplicationLateStart() {
            base.OnApplicationLateStart();
            LoggerInstance.Msg("Init");
            Task.Run(() => WaitForSuit());
        }


        public static async Task WaitForSuit() {
            MelonLogger.Msg($"~~~SHOCKWAVE~~~ Waiting for Suit");
            suit = ShockwaveManager.Instance;
            suit.InitializeSuit(); //Wait for the suit forever

            suit.enableBodyTracking = false;
            while (!ShockwaveManager.Instance.Ready && ShockwaveManager.Instance.error == 0) {
                await Task.Delay(1000);
                if(SuitPreferences.verbose.Value)
                    MelonLogger.Msg($"~~~SHOCKWAVE~~~ Waiting...");
            }

            if (ShockwaveManager.Instance.error > 0) {
                MelonLogger.Msg($"~~~SHOCKWAVE~~~ Initialization Error {ShockwaveManager.Instance.error}");
            } else if (ShockwaveManager.Instance.Ready) {
                ShockwaveManager.Instance.InitSequence();
                MelonLogger.Msg($"~~~SHOCKWAVE~~~ Suit Connected { (ShockwaveManager.Instance.isUsingSteamVR ? "SteamVR" : "Native") }");
            } else
                MelonLogger.Msg($"~~~SHOCKWAVE~~~ Unknown Init Error");
        }
    }
}
