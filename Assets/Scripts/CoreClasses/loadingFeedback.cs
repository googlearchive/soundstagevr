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

using UnityEngine;
using System.Collections;

public class loadingFeedback : MonoBehaviour {
  TextMesh txt;
  string[] txtFrames;
  // Use this for initialization
  void Awake() {
    txt = GetComponent<TextMesh>();
    txtFrames = new string[]
    {
            "  ",".","..","..."
    };
  }

  float timer = 0;
  int frame = 0;
  void Update() {
    timer = Mathf.Clamp01(timer + Time.deltaTime * 10);
    if (timer == 1) {
      timer = 0;
      frame = (frame + 1) % txtFrames.Length;
      txt.text = txtFrames[frame];
    }
  }
}
