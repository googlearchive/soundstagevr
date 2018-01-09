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

public class wireConnect : MonoBehaviour {

  public omniJack setoutput, setinput;

  void Start() {
    ConnectJacks(setoutput, setinput);
  }

  public void ConnectJacks(omniJack output, omniJack input) {
    StartCoroutine(ConnectJacksRoutine(output, input));
  }

  IEnumerator ConnectJacksRoutine(omniJack output, omniJack input) {
    omniPlug o1 = (Instantiate(output.plugPrefab, output.transform.position, output.transform.rotation) as GameObject).GetComponent<omniPlug>();
    o1.outputPlug = false;

    omniPlug o2 = (Instantiate(output.plugPrefab, output.transform.position, input.transform.rotation) as GameObject).GetComponent<omniPlug>();
    o2.outputPlug = true;

    Vector3[] tempPath = new Vector3[] {
            output.transform.position,
            input.transform.position
        };

    Color tempColor = Color.HSVToRGB(0, .8f, .5f);

    o1.Activate(o2, output, new Vector3[] { }, tempColor);
    o2.Activate(o1, input, new Vector3[] { output.transform.position, output.transform.position }, tempColor);

    Vector3 targPos = o2.transform.position;
    Quaternion targRot = o2.transform.rotation;

    Quaternion preRot = o1.transform.rotation * Quaternion.Euler(180, 0, 0);

    float timer = 0;

    o2.transform.position = output.transform.position;
    o2.transform.rotation = preRot;
    o2.connected = o1.connected = null;

    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 4);
      o2.transform.position = Vector3.Lerp(output.transform.position, targPos, timer);
      o2.transform.rotation = Quaternion.Lerp(preRot, targRot, timer);

      yield return null;
    }
    o1.connected = output;
    o2.connected = input;
    yield return null;
  }
}
