// -------------------------------------------------------------------------------------------------
// warnings.cs 0.0.1
//
// Voice warnings using Windows text to speech api.
// Copyright (C) 2014 Iván Atienza
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.
//
// Email: mecagoenbush at gmail dot com
// Freenode: hashashin
//
// -------------------------------------------------------------------------------------------------

using SpeechLib;
using System;
using System.Linq;
using UnityEngine;

namespace warnings
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Warnings : MonoBehaviour
    {
        private const string _btextureOff = "warnings/Textures/icon_off";
        private const string _btextureOn = "warnings/Textures/icon_on";
        private const float _interval = 15.0f;
        private const float _interval2 = 5.0f;
        private const string _tooltipoff = "Active warnings";
        private const string _tooltipon = "Disable warnings";
        private bool _active;
        private IButton _button;
        private string _lastlog;
        private string _lastsituation;
        private float _lastUpdate;
        private float _lastUpdate2;
        //private const float _safedist = 10f;
        private SpVoice _objSpeech = new SpVoice();
        public void Speech(string args)
        {
            _objSpeech.Volume = 100;
            _objSpeech.SynchronousSpeakTimeout = 30;
            _objSpeech.Rate = -1;
            _objSpeech.Speak(args, SpeechVoiceSpeakFlags.SVSFlagsAsync);
        }

        private void Awake()
        {
            GameEvents.onOverheat.Add(OnOverHeat);
            GameEvents.onLaunch.Add(OnLaunch);
            GameEvents.onCrewKilled.Add(OnCrewKilled);
        }
        private void OnCrewKilled(EventReport data)
        {
            if (_active)
            {
                Speech("Dramatic mode on: NOOOOOOOOOO!");
            }
        }

        private void OnDestroy()
        {
            _objSpeech = null;
            if (_button != null)
            {
                _button.Destroy();
            }
            GameEvents.onOverheat.Remove(OnOverHeat);
            GameEvents.onLaunch.Remove(OnLaunch);
            GameEvents.onCrewKilled.Remove(OnCrewKilled);
        }

        private void OnLaunch(EventReport data)
        {
            if (_active)
            {
                Speech("And " + FlightGlobals.ActiveVessel.GetName() + "starts his mission! Good luck brave Kerbals!");
            }
        }

        private void OnOverHeat(EventReport data)
        {
            if (_active)
            {
                Speech("Warning Overheat detected!");
            }
        }
        private void Start()
        {
            if (!ToolbarManager.ToolbarAvailable) return;
            _button = ToolbarManager.Instance.add("warnings", "toggle");
            _button.TexturePath = _btextureOff;
            _button.ToolTip = _tooltipoff;
            _button.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);

            _button.OnClick += e => Toggle();
        }

        private void Toggle()
        {
            if (_active)
            {
                Speech("Systems offline. Bye.");
                _active = false;
                _button.TexturePath = _btextureOff;
                _button.ToolTip = _tooltipoff;
            }
            else
            {
                Speech("Systems online.");
                _active = true;
                _button.TexturePath = _btextureOn;
                _button.ToolTip = _tooltipon;
            }
        }

        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel.isEVA || !_active) return;

            if ((Time.time - _lastUpdate2) > _interval2)
                if (FlightLogger.eventLog.Count > 0 && _lastlog != FlightLogger.eventLog.Last())
                {
                    Speech(FlightLogger.eventLog.Last());
                    _lastlog = FlightLogger.eventLog.Last();
                    _lastUpdate2 = Time.time;
                }

            if (!((Time.time - _lastUpdate) > _interval)) return;

            _lastUpdate = Time.time;

            if (FlightGlobals.ship_altitude < 1000 && FlightGlobals.ship_altitude > 200)
            {
                Speech("<EMPH>Warning</EMPH>!, low altitude: " + Math.Floor(FlightGlobals.ship_altitude) +
                       " meters.");
            }
            else if (FlightGlobals.ship_altitude < 200 && FlightGlobals.ship_altitude > 1 &&
                     FlightGlobals.ActiveVessel.situation.ToString() != "LANDED" &&
                     FlightGlobals.ActiveVessel.situation.ToString() != "PRELAUNCH" &&
                     FlightGlobals.ActiveVessel.situation.ToString() != "SPLASHED")
            {
                Speech("<EMPH>Warning</EMPH>!, about to crash!");
            }
            else if (FlightGlobals.ActiveVessel.rootPart.temperature >=
                     FlightGlobals.ActiveVessel.rootPart.maxTemp - 200)
            {
                Speech("<EMPH>Warning</EMPH>!, high temperature in cabin hull");
            }
            else if (FlightGlobals.ship_geeForce > 6)
            {
                Speech("<EMPH>Warning</EMPH>!, Gee force too high. About: " +
                       Math.Floor(FlightGlobals.ship_geeForce) + " gees.");
            }
            else if (FlightGlobals.ActiveVessel.packed)
            {
                Speech(FlightGlobals.ActiveVessel.vesselName + " is warping");
            }
            else if (FlightGlobals.vacuumTemperature > 3100)
            {
                Speech("<EMPH>Warning</EMPH>!, exterior temperature too high!, about: " +
                       FlightGlobals.vacuumTemperature + " celsious degrees");
            }
            else if (FlightGlobals.getExternalTemperature(FlightGlobals.ActiveVessel.GetWorldPos3D()) >
                     3100)
            {
                Speech("<EMPH>Warning</EMPH>!, exterior temperature too high!, about: " +
                       FlightGlobals.vacuumTemperature + " celsious degrees");
            }
            else if (FlightGlobals.ActiveVessel.Landed && FlightGlobals.ActiveVessel.situation.ToString() != "PRELAUNCH")
            {
                Speech("Landed at: " + FlightGlobals.ActiveVessel.mainBody.theName);
            }
            else
            {
                if (_lastsituation != Vessel.GetSituationString(FlightGlobals.ActiveVessel))
                {
                    Speech(Vessel.GetSituationString(FlightGlobals.ActiveVessel));
                }
                _lastsituation = Vessel.GetSituationString(FlightGlobals.ActiveVessel);
            }
        }
    }
}