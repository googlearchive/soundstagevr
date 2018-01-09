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

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public class bufferToWav : MonoBehaviour {
    public static bufferToWav instance;

    [DllImport("SoundStageNative")]
    public static extern void CompressClip(float[] clip, int length);

    void Awake()
    {
        instance = this;
    }

    public bool savingInProgress = false;
    public Coroutine Save(string filename, float[] clip, int channels, int length, TextMesh txt, signalGenerator sig)
    {
        savingInProgress = true;

        if (!filename.ToLower().EndsWith(".wav")) filename += ".wav";
        return StartCoroutine(SaveRoutine(filename, clip, length, txt, sig));
    }

    void WavHeader(BinaryWriter b, int length)
    {
        int _samplerate = 44100;
        int _channels = 2;
        int _samplelength = 2; //2 bytes
       
        b.Write(Encoding.ASCII.GetBytes("RIFF")); // chunkid
        b.Write(36 + length * _samplelength); // chunksize = (length * _samplelength) + 36???
        b.Write(Encoding.ASCII.GetBytes("WAVE")); // format
        b.Write(Encoding.ASCII.GetBytes("fmt "));  // subchunk1 ID (fmt)
        b.Write(16); // subchunk1 size -- constant size
        b.Write((short)1);  // udioformat
        b.Write((short)_channels); //   // channels
        b.Write(_samplerate);  // samplerate
        b.Write(_samplerate * _samplelength * _channels);    //byterate
        b.Write((short)(_samplelength * _channels)); // block align
        b.Write((short)(8 * _samplelength)); // bits per sample
        b.Write(Encoding.ASCII.GetBytes("data"));  // subchunk2 ID (data)    
        b.Write(length * _samplelength); // subchunk 2 size
    }

    IEnumerator SaveRoutine(string filename, float[] clip, int length, TextMesh txt, signalGenerator sig)
    {
        txt.gameObject.SetActive(true);
        txt.text = "Saving...";
        
        FileStream _filestream = new FileStream(filename, FileMode.Create);
        BinaryWriter _binarystream = new BinaryWriter(_filestream);
        WavHeader(_binarystream, length);

        CompressClip(clip, clip.Length);

        int counter = 0;
        for (int i = 0; i < length; i++)
        {
            Int16 sample = Convert.ToInt16( Mathf.Clamp(clip[i],-1f,1f) * 32760 );
            _binarystream.Write((short)sample);
            counter++;

            if (counter > 10000)
            {
                counter = 0;
                txt.text = "Saving... " + (int)(100 * (float)i / length) + "% Complete";
                yield return null;
            }
        }

        _binarystream.Close();
        _filestream.Close();
        txt.text = "Saved";
        
        sampleManager.instance.AddRecording(filename);
        txt.text = "Saved";
        savingInProgress = false;
        txt.gameObject.SetActive(false);
        sig.updateTape(filename);
        yield return new WaitForSeconds(1.5f);
    }    
}
