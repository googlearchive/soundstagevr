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

public class signalGenerator : MonoBehaviour
{
    public int index = 0;

    protected double _sampleRate;
    protected double _sampleDuration;
    public double _phase;

    public bool useNativeIfAvailable = true;

    float levelVal = 1;

    protected const int MAX_BUFFER_LENGTH = 2048; // Very important to enforce this 

    public virtual void Awake()
    {
        _phase = 0;
        _sampleRate = AudioSettings.outputSampleRate;
        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    }

    public virtual menuItem.deviceType queryDeviceType()
    {
        return menuItem.deviceType.Max;
    }

    public virtual void updateTape(string s)
    {
       
    }
  
    public virtual void trigger(int c)
    {
        
    }

    public virtual void trigger(int c, float f)
    {

    }

    public virtual void modBuffer(double dspTime, int channels, float[] buffer)
    {

    }

    public virtual void processBuffer(float[] buffer, double dspTime, int channels)
    {

    }

    public virtual float[] getBuffer(double dspTime, int channels, int bufferLength, bool modFreq = false, float requestedFreq = 440f, float detuneAmount = 0)
    {
        float[] buffer = new float[bufferLength];

        for (int i = 0; i < buffer.Length; i += channels)
        {
            double sample = Mathf.Sin((float)_phase * 2 * Mathf.PI);

            float frequency = 440;
            float amplitude = 0.5f;

            _phase += frequency * _sampleDuration;

            if (_phase > 1.0) _phase -= 1.0;

            buffer[i] = (float)sample * amplitude;
            buffer[i + 1] = (float)sample * amplitude;

            dspTime += _sampleDuration;
        }

        return buffer;
    }
}