// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class drumSignalGenerator : signalGenerator
{

    bool newSignal = false;
    bool signalOn = false;

    int hitdur = 100;
    int counter = 0;

    [DllImport("SoundStageNative", EntryPoint = "DrumSignalGenerator")]
    public static extern int DrumSignalGenerator(float[] buffer, int length, int channels, bool signalOn, int counter);

    void Start()
    {
        hitdur = AudioSettings.outputSampleRate / 10;
    }

    public void setKeyActive(bool on, int ID)
    {
        if (on)
        {
            newSignal = true;
            signalOn = true;
            counter = hitdur;
        }
    }

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        counter = DrumSignalGenerator(buffer, buffer.Length, channels, signalOn, counter);
        if (counter < 0)
            signalOn = false;
        if (newSignal)
        {
            buffer[0] = buffer[1] = -1;
            newSignal = false;
        }
    }
}