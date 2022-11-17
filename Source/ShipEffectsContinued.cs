﻿using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using KSP;

namespace ShipEffectsContinued
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SEMaster : MonoBehaviour
    {
        //soundnames
        const string rattles_small = "ShipEffectsContinued/Sounds/rattles_small";
        const string rattles_big = "ShipEffectsContinued/Sounds/rattles_big";
        const string rumble = "ShipEffectsContinued/Sounds/rumble";
        const string vibrations = "ShipEffectsContinued/Sounds/vibrations";
        const string stress_big = "ShipEffectsContinued/Sounds/stress_big";
        const string atmosphere = "ShipEffectsContinued/Sounds/atmosphere";

        const string thump_low = "ShipEffectsContinued/Sounds/thump_low";
        const string thump_heavy = "ShipEffectsContinued/Sounds/thump_heavy";

        const string docking = "ShipEffectsContinued/Sounds/docking";

        //groups
        FXGroup smallRattlesGroup = new FXGroup("smallRattlesFXGroup");
        FXGroup bigRattlesGroup = new FXGroup("bigRattlesFXGroup");
        FXGroup rumbleGroup = new FXGroup("rumbleFXGroup");
        FXGroup vibrationsGroup = new FXGroup("vibrationsFXGroup");
        FXGroup atmosphereGroup = new FXGroup("atmosphereFXGroup");
        FXGroup stressBigGroup = new FXGroup("stressBigFXGroup");

        FXGroup thumpLowGroup = new FXGroup("ThumpLowFXGroup");
        FXGroup thumpHeavyGroup = new FXGroup("ThumpHeavyFXGroup");

        FXGroup dockedGroup = new FXGroup("DockFXGroup");
        FXGroup undockedGroup = new FXGroup("DockFXGroup");

        //sets
        bool rumbleSet;
        bool smallRattlesSet;
        bool bigRattlesSet;
        bool vibrationsSet;
        bool atmosphereSet;
        bool stressBigSet;

        bool thumpLowSet;
        bool thumpHeavySet;

        bool dockedSet;
        bool undockedSet;

        bool gamePaused = false;

        Vessel vessel;
        AerodynamicsFX aeroFx;

        float vesselMass;
        float vesselAcceleration;
        float vesselRot;
        float surfSpeed;
        float atmDensity;
        float engineThrust;
        float vResist;
        float engineAccel;
        float engineMicro;
        float burnDownTime = 0;
        float[] atmBurnDowntimes = new float[] { 1.0f };

        float counter = 0;

        bool doEngineThrust;

        void Start()
        {
            vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || !HighLogic.LoadedSceneIsFlight)
                return;

            LoadGroups();

            GameEvents.onPartCouple.Add(this.onVesselDock);
            GameEvents.onPartUndock.Add(this.onVesselUndock);

            GameEvents.onGamePause.Add(this.onGamePaused);
            GameEvents.onGameUnpause.Add(this.onGameUnpaused);
        }

        void LoadGroups()
        {
            smallRattlesSet = createGroup(smallRattlesGroup, vessel, rattles_small, true, true);
            bigRattlesSet = createGroup(bigRattlesGroup, vessel, rattles_big, true, true);
            rumbleSet = createGroup(rumbleGroup, vessel, rumble, true, true);
            vibrationsSet = createGroup(vibrationsGroup, vessel, vibrations, true, true);
            stressBigSet = createGroup(stressBigGroup, vessel, stress_big, true, true);
            atmosphereSet = createGroup(atmosphereGroup, vessel, atmosphere, true, true);

            thumpLowSet = createGroup(thumpLowGroup, vessel, thump_low, false, false);
            thumpHeavySet = createGroup(thumpHeavyGroup, vessel, thump_heavy, false, false);

            dockedSet = createGroup(dockedGroup, vessel, docking, false, true);
            undockedSet = createGroup(undockedGroup, vessel, docking, false, true);
        }

        public bool createGroup(FXGroup group, Vessel vessel, string clip, bool loop, bool fxBypass)
        {
            GameObject audioGroup = new GameObject("ShipEffects");
            audioGroup.transform.parent = vessel.gameObject.transform;

            group.audio = audioGroup.AddComponent<AudioSource>();

            group.audio.clip = GameDatabase.Instance.GetAudioClip(clip);
            group.audio.Stop();
            group.audio.loop = loop;
            group.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            group.audio.playOnAwake = false;
            group.audio.maxDistance = 100f;
            group.audio.spatialBlend = 1f;
            group.audio.dopplerLevel = 0f;

            group.audio.bypassEffects = fxBypass;

            Debug.Log("ShipEffectsContinued: " + group.name + " is Created");

            return true;
        }

        void SoundFX(FXGroup fx, float volume, float volCtrl, float spread, bool play)
        {
            if (fx.audio != null)
            {
                if (!gamePaused && !fx.audio.isPlaying && play == true)
                {
                    fx.audio.Play();
                    fx.audio.volume = (Mathf.Clamp(volume, 0, 1f) * volCtrl) * HighLogic.CurrentGame.Parameters.CustomParams<SE>().masterVolume;
                }
                else
                {
                    fx.audio.volume = Mathf.Clamp(volume, 0, 1f);
                }
                if (play == false)
                {
                    fx.audio.Stop();
                }
                fx.audio.spread = spread;
            }
            else
            {
                LoadGroups();
            }
        }

        void SoundFX(FXGroup fx, bool play)
        {
            SoundFX(fx, 0, 0, 0, play);
        }

        float dockTime = 0;
        float[] dockTimes = new float[] { 0.25f };
        bool ifUndocking = false;
        public void onVesselDock(GameEvents.FromToAction<Part, Part> action)
        {

            dockTime = dockTimes[0];
            ifUndocking = false;
        }

        public void onVesselUndock(Part action)
        {
            dockTime = dockTimes[0];
            ifUndocking = true;

        }


        void Update()
        {
            vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || vessel.isEVA || !HighLogic.LoadedSceneIsFlight)
                return;

            //grab the data we need
            surfSpeed = (float)vessel.srfSpeed;
            atmDensity = (float)vessel.atmDensity;
            vesselMass = vessel.GetTotalMass();
            vesselRot = (float)Math.Sqrt(Math.Pow(vessel.angularVelocity.x, 2) + Math.Pow(vessel.angularVelocity.y, 2) + Math.Pow(vessel.angularVelocity.z, 2));
            vesselAcceleration = (float)Math.Sqrt(Math.Pow(vessel.acceleration.x, 2) + Math.Pow(vessel.acceleration.y, 2) + Math.Pow(vessel.acceleration.z, 2)) * 0.1f;
            vResist = (atmDensity * surfSpeed * 0.01f) * vesselAcceleration;



            if (aeroFx == null)
            {
                GameObject fx = GameObject.Find("FXLogic");
                if (fx != null)
                {
                    aeroFx = fx.GetComponent<AerodynamicsFX>();
                }
            }

            if ((aeroFx != null) && (aeroFx.FxScalar > 0.01))
            {
                if (aeroFx.fxLight.color.b < 0.12f)
                {

                    burnDownTime = atmBurnDowntimes[0];

                }

                if (aeroFx.fxLight.color.b > 0.20f)
                {

                    if (aeroFx.FxScalar > 0.1)
                        vResist += (aeroFx.FxScalar * 2);
                }
            }

            if (burnDownTime > 0)
            {
                vResist += (aeroFx.FxScalar * burnDownTime * HighLogic.CurrentGame.Parameters.CustomParams<SE>().reEntryMultiplier);

                burnDownTime -= Time.deltaTime;
            }

            //thickeratmosphericsound in thicker atmospheres
            if (atmDensity > 0)
            {
                float airPressure = 1f / atmDensity;
                atmosphereGroup.audio.pitch = Mathf.Clamp(airPressure, 0, 1.5f);
            }

            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module.moduleName != null)
                    {
                        if (module.moduleName.Contains("ModuleEnginesFX"))
                        {

                            ModuleEnginesFX e = module as ModuleEnginesFX;
                            if (e != null && e.isOperational)
                            {

                                engineThrust += (e.finalThrust);
                            }
                            if (engineThrust > 0)
                                doEngineThrust = true;
                        }
                        else if (module.moduleName.Contains("ModuleEngine"))
                        {

                            ModuleEngines e = module as ModuleEngines;
                            if (e != null && e.isOperational)
                            {

                                engineThrust += (e.finalThrust);
                            }

                            if (engineThrust > 0)
                                doEngineThrust = true;
                        }
                    }
                }
            }

            if (engineThrust > 0 && doEngineThrust == true)
            {
                engineMicro = engineThrust / 1000f;
                engineAccel = Mathf.Abs(((engineThrust / vesselMass) * 0.1f) - vesselAcceleration) + engineMicro;

            }
            else if (engineThrust <= 0)
            {
                doEngineThrust = false;
            }

            vResist += engineAccel;

            vResist *= HighLogic.CurrentGame.Parameters.CustomParams<SE>().resistMultiplier;

            bool isCrewed = false;

            //don't search if not necessary
            if (HighLogic.CurrentGame.Parameters.CustomParams<SE>().OnlyIfCrewed)
            {
                foreach (Part part in vessel.parts)
                {
                    if (part.protoModuleCrew.Count >= 1)
                    {
                        isCrewed = true;
                    }
                }
            }

            if (!gamePaused)
            {

                if ((!HighLogic.CurrentGame.Parameters.CustomParams<SE>().OnlyIfCrewed || isCrewed) && !MapView.MapIsEnabled && (HighLogic.CurrentGame.Parameters.CustomParams<SE>().OnlyInIVA == false || InternalCamera.Instance.isActive))
                {
                    //wind and pressure?
                    if (surfSpeed > 10 || vesselRot > 1.5f)
                    {
                        SoundFX(atmosphereGroup, ((atmDensity * surfSpeed - 10f) / 80f) + ((vesselRot - 1.5f) / 7.0f * atmDensity), HighLogic.CurrentGame.Parameters.CustomParams<SE>().atmosphereVolume / 100f, 90f, true);
                    }
                    else
                    {
                        SoundFX(atmosphereGroup, false);
                    }

                    //dynamics
                    if (vResist > 0.5)
                    {
                        SoundFX(smallRattlesGroup, (vResist - 0.5f) / 4f, HighLogic.CurrentGame.Parameters.CustomParams<SE>().rattleVolume / 100f, 90f, true);
                    }
                    else
                    {
                        SoundFX(smallRattlesGroup, false);
                    }

                    if (vResist > 0.8 || vesselRot > 1.5f)
                    {
                        SoundFX(vibrationsGroup, ((vResist - 0.8f) / 2f) + ((vesselRot - 1.5f) / 6f), HighLogic.CurrentGame.Parameters.CustomParams<SE>().vibrationVolume / 100f, 35f, true);
                    }
                    else
                    {
                        SoundFX(vibrationsGroup, false);
                    }

                    if (vResist > 1 || vesselRot > 2.0f)
                    {
                        SoundFX(rumbleGroup, ((vResist - 1f) / 2f) + ((vesselRot - 2.0f) / 6f), HighLogic.CurrentGame.Parameters.CustomParams<SE>().rumbleVolume / 100f, 180f, true);
                    }
                    else
                    {
                        SoundFX(rumbleGroup, false);
                    }

                    if (vResist > 4.0)
                    {
                        SoundFX(bigRattlesGroup, (vResist - 5f) / 4f, HighLogic.CurrentGame.Parameters.CustomParams<SE>().rattleVolume / 100f, 90f, true);
                    }
                    else
                    {
                        SoundFX(bigRattlesGroup, false);
                    }

                    if (vResist > 8.0)
                    {
                        SoundFX(stressBigGroup, (vResist - 6f) / 6f, HighLogic.CurrentGame.Parameters.CustomParams<SE>().stressVolume / 100f, 90f, true);
                    }
                    else
                    {
                        SoundFX(stressBigGroup, false);
                    }

                    if (dockTime > 0)
                    {
                        if (ifUndocking)
                            SoundFX(undockedGroup, 1f, 1f, 45f, true);
                        if (!ifUndocking)
                            SoundFX(dockedGroup, 1f, 1f, 45f, true);

                        dockTime -= Time.deltaTime;
                    }

                    if (vResist > 1.8)
                    {
                        counter += Time.deltaTime;
                        if (counter > 0.26f)
                            counter = 1;
                        if (counter < 0.25f)
                        {
                            if (vResist > 2.0f && vResist < 5.0)
                            {
                                SoundFX(thumpLowGroup, (vResist - 2.0f) / 3f, HighLogic.CurrentGame.Parameters.CustomParams<SE>().thumpVolume / 100f, 180f, true);
                            }
                            if (vResist > 4.0f && vResist < 8.0)
                            {
                                SoundFX(thumpHeavyGroup, (vResist - 4.0f) / 4f, HighLogic.CurrentGame.Parameters.CustomParams<SE>().thumpVolume / 100f, 180f, true);
                            }
                        }
                    }
                    else
                    {
                        counter = 0;
                    }

                }
                else
                {
                    SoundFX(atmosphereGroup, false);
                    SoundFX(smallRattlesGroup, false);
                    SoundFX(vibrationsGroup, false);
                    SoundFX(bigRattlesGroup, false);
                    SoundFX(rumbleGroup, false);
                    SoundFX(stressBigGroup, false);
                }
            }
            else
            {

                SoundFX(atmosphereGroup, false);
                SoundFX(smallRattlesGroup, false);
                SoundFX(vibrationsGroup, false);
                SoundFX(bigRattlesGroup, false);
                SoundFX(rumbleGroup, false);
                SoundFX(stressBigGroup, false);
            }

            engineMicro = 0;
            engineThrust = 0;
            engineAccel = 0;
        }

        public void onGamePaused()
        {
            gamePaused = true;
        }

        public void onGameUnpaused()
        {
            gamePaused = false;
        }

        public void OnDestroy()
        {
            GameEvents.onGamePause.Remove(new EventVoid.OnEvent(onGamePaused));
            GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(onGameUnpaused));
            GameEvents.onPartCouple.Remove(this.onVesselDock);
            GameEvents.onPartUndock.Remove(this.onVesselUndock);
        }
    }
}

