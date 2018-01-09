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

//assumes its all local
public class primitiveLine : MonoBehaviour {
  public Transform p1, p2;

  Vector3 lastPos1, lastPos2;

  void Awake() {
    lastPos1 = Vector3.zero;
    lastPos2 = Vector3.zero;
  }

  void Update() {
    if (lastPos1 != p1.localPosition || lastPos2 != p2.localPosition) {
      lastPos1 = p1.localPosition;
      lastPos2 = p2.localPosition;
      UpdateLine();
    }
  }

  void UpdateLine() {
    transform.position = Vector3.Lerp(p1.position, p2.position, .5f);
    float dist = Vector3.Distance(p1.localPosition, p2.localPosition);
    transform.localScale = new Vector3(.0025f, dist, 1);
    float rot = Mathf.Atan2(p1.transform.localPosition.x - p2.transform.localPosition.x, p2.transform.localPosition.y - p1.transform.localPosition.y) * Mathf.Rad2Deg;
    transform.localRotation = Quaternion.Euler(0, 0, rot);
  }
}
