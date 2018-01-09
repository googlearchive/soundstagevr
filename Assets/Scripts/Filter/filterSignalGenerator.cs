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
using System.Runtime.InteropServices;

public class filterSignalGenerator : signalGenerator {

    public signalGenerator incoming,controlIncoming;

    public float controlFloat = 0;

    MonoFilter[] filters;

    public float resonance = .5f;//0 to 1 - tie to a dial?
    public float[] frequency = new float[] { .3f,.6f};// cutoff frequency for LP and BP

    float[] bufferCopy;
    float[] controlBuffer;

    // Changing this number requires changing native code.
    const int NUM_FILTERS = 4;

    // Changing this enum requires changing the mirrored native enum.
    public enum filterType
    {
        none,
        LP,
        HP,
        LP_long,
        HP_long,
        BP,
        Notch,
        pass
    };

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);
    [DllImport("SoundStageNative")]
    public static extern void CopyArray(float[] a, float[] b, int length);
    [DllImport("SoundStageNative")]
    public static extern void AddArrays(float[] a, float[] b, int length);
    [DllImport("SoundStageNative")]
    public static extern void processStereoFilter(float[] buffer, int length, ref mfValues mfA, ref mfValues mfB);

    public filterType curType = filterType.none;

    public override void Awake()
    {
        base.Awake();
        filters = new MonoFilter[NUM_FILTERS];
        bufferCopy = new float[MAX_BUFFER_LENGTH];
        controlBuffer = new float[MAX_BUFFER_LENGTH];

        //primary stereo filter
        filters[0] = new MonoFilter(frequency[0], resonance);
        filters[1] = new MonoFilter(frequency[0], resonance);

        //secondary stereo filter (for BP/notch)
        filters[2] = new MonoFilter(frequency[1], resonance);
        filters[3] = new MonoFilter(frequency[1], resonance);
    }


    public void updateFilterType(filterType f)
    {
        curType = f;

        if(f == filterType.LP)
        {
            filters[0].mf.LP = true;
            filters[1].mf.LP = true;

            filters[0].SetFrequency(frequency[0]);
            filters[1].SetFrequency(frequency[0]);
        }
        else if(f == filterType.LP_long)
        {
            filters[0].mf.LP = true;
            filters[1].mf.LP = true;

            filters[0].SetFrequency(frequency[1]);
            filters[1].SetFrequency(frequency[1]);
        }
        else if (f == filterType.HP)
        {
            filters[0].mf.LP = false;
            filters[1].mf.LP = false;

            filters[0].SetFrequency(frequency[1]);
            filters[1].SetFrequency(frequency[1]);
        }
        else if (f == filterType.HP_long)
        {
            filters[0].mf.LP = false;
            filters[1].mf.LP = false;

            filters[0].SetFrequency(frequency[0]);
            filters[1].SetFrequency(frequency[0]);
        }
        else if (f == filterType.Notch)
        {
            filters[0].mf.LP = true;
            filters[1].mf.LP = true;

            filters[2].mf.LP = false;
            filters[3].mf.LP = false;

            filters[0].SetFrequency(frequency[0]);
            filters[1].SetFrequency(frequency[0]);

            filters[2].SetFrequency(frequency[1]);
            filters[3].SetFrequency(frequency[1]);
        }
        else if (f == filterType.BP)
        {
            filters[0].mf.LP = true;
            filters[1].mf.LP = true;

            filters[2].mf.LP = false;
            filters[3].mf.LP = false;

            filters[0].SetFrequency(frequency[1]);
            filters[1].SetFrequency(frequency[1]);

            filters[2].SetFrequency(frequency[0]);
            filters[3].SetFrequency(frequency[0]);
        }
    }

    void Update()
    {
        if (curType == filterType.LP || curType == filterType.HP_long)
        {
            filters[0].SetFrequency(frequency[0]);
            filters[1].SetFrequency(frequency[0]);
        }
        else if (curType == filterType.LP_long || curType == filterType.HP)
        {
            filters[0].SetFrequency(frequency[1]);
            filters[1].SetFrequency(frequency[1]);
        }
        else if (curType == filterType.Notch)
        {

            filters[0].SetFrequency(frequency[0]);
            filters[1].SetFrequency(frequency[0]);

            filters[2].SetFrequency(frequency[1]);
            filters[3].SetFrequency(frequency[1]);
        }
        else if (curType == filterType.BP)
        {

            filters[0].SetFrequency(frequency[1]);
            filters[1].SetFrequency(frequency[1]);

            filters[2].SetFrequency(frequency[0]);
            filters[3].SetFrequency(frequency[0]);
        }
    }

    private void OnAudioFilterRead(float[] buffer, int channels)
    {        
        if (incoming == null || bufferCopy == null) return;
        CopyArray(bufferCopy,buffer, buffer.Length);
    }
    
    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (bufferCopy.Length != buffer.Length)
            System.Array.Resize(ref bufferCopy, buffer.Length);

        if (controlBuffer.Length != buffer.Length)
            System.Array.Resize(ref controlBuffer, buffer.Length);

        if (controlIncoming != null)
        {
            controlIncoming.processBuffer(controlBuffer, dspTime, channels);
            controlFloat = controlBuffer[controlBuffer.Length - 1];
        }

        // if silent, 0 out and return
        if (!incoming || curType == filterType.none)
        {
            SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
            SetArrayToSingleValue(bufferCopy, bufferCopy.Length, 0.0f);
            return;            
        }

        incoming.processBuffer(buffer, dspTime, channels);

        // if pass through, just end
        if (curType == filterType.pass)
        {
            CopyArray(buffer, bufferCopy,buffer.Length);
            return;
        }

        if (curType != filterType.Notch && curType != filterType.BP)
        {
            processStereoFilter(buffer, buffer.Length, ref filters[0].mf, ref filters[1].mf);
        }
        else if (curType == filterType.Notch)
        {
            CopyArray(buffer, bufferCopy, buffer.Length);

            processStereoFilter(buffer, buffer.Length, ref filters[0].mf, ref filters[1].mf);
            processStereoFilter(bufferCopy, bufferCopy.Length, ref filters[2].mf, ref filters[3].mf);

            AddArrays(buffer, bufferCopy, buffer.Length);
        }

        else if (curType == filterType.BP)
        {
            processStereoFilter(buffer, buffer.Length, ref filters[0].mf, ref filters[1].mf);
            processStereoFilter(buffer, buffer.Length, ref filters[2].mf, ref filters[3].mf);
        }
        
        CopyArray(buffer, bufferCopy, buffer.Length);
    }
}

public struct mfValues
{
    public float f, p, q; //filter coefficients
    public float b0, b1, b2, b3, b4; //filter buffers (beware denormals!)
    public bool LP;
};

class MonoFilter
{
    public float frequency = .5f;
    public float resonance = .5f;
    
    public mfValues mf = new mfValues();
    float t1, t2; 
    public MonoFilter(float fre, float r)
    {
        mf.LP = true;
        frequency = fre;
        resonance = r;        
        Update();
    }

    public void SetFrequency(float fre)
    {
        frequency = fre;
        Update();
    }

    public void SetResonance(float r)
    {
        resonance = r;
        Update();
    }

    public void Update()
    {
        mf.q = 1.0f - frequency;
        mf.p = frequency + 0.8f * frequency * mf.q;
        mf.f = mf.p + mf.p - 1.0f;
        mf.q = resonance * (1.0f + 0.5f * mf.q * (1.0f - mf.q + 5.6f * mf.q * mf.q));
    }
}

