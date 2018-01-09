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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timelineGridRender : MonoBehaviour {
  public Transform gridUIPlane;
  private Mesh mesh;

  public void Init() {
    mesh = new Mesh();
    GetComponent<MeshFilter>().mesh = mesh;

    Material[] mats = GetComponent<Renderer>().materials;
    mats[0].SetColor("_TintColor", new Color32(0x00, 0x00, 0x01, 0x32));
    mats[1].SetColor("_TintColor", new Color32(0x08, 0x09, 0x15, 0x93));
    mats[2].SetColor("_TintColor", new Color32(0x0B, 0x0B, 0x11, 0xFF));
    mats[3].SetColor("_TintColor", new Color32(66, 66, 66, 255));
    mats[4].SetColor("_TintColor", Color.white);
  }

  void updateUI(Vector2 bounds) {
    Vector3 pos = new Vector3(bounds.x, bounds.y, 1);
    gridUIPlane.localScale = pos;
    pos.x *= .5f;
    pos.y *= .5f;
    pos.z = 0;
    gridUIPlane.localPosition = pos;
  }

  public bool rowTone(int i) {
    i = i - 1;
    return i % 12 == 1 || i % 12 == 3 || i % 12 == 6 || i % 12 == 8 || i % 12 == 10;
  }

  public void updateGrid(gridParams _gridParams) {
    mesh.Clear();
    mesh.subMeshCount = 5;

    List<Vector3> points = new List<Vector3>();
    List<int> rowsA = new List<int>();
    List<int> rowsB = new List<int>();

    List<int> lines = new List<int>();
    List<int> linesB = new List<int>();
    List<int> linesC = new List<int>();

    Vector2 bounds = new Vector2(_gridParams.getGridWidth(), _gridParams.getGridHeight());
    updateUI(bounds);

    points.Add(new Vector3(0, 0));
    points.Add(new Vector3(bounds.x, 0));

    int counter = 0;
    for (int i = 0; i < _gridParams.tracks; i++) {
      float h = _gridParams.UnittoY(i);
      points.Add(new Vector3(0, h));
      points.Add(new Vector3(bounds.x, h));
      counter++;
    }

    points.Add(new Vector3(0, bounds.y));
    points.Add(new Vector3(bounds.x, bounds.y));
    counter++;

    for (int i = 0; i < counter; i++) {
      int s = i * 2;
      if (rowTone(i)) {
        rowsA.AddRange(new int[] {
                    s, s+1,s+3,s+2
                });
      } else {
        rowsB.AddRange(new int[] {
                    s, s+1,s+3,s+2
                });
      }

      if ((i - 1) % 12 == 5 || (i - 1) % 12 == 0) {
        lines.Add(s);
        lines.Add(s + 1);
      }
    }

    int tempCounter = points.Count;
    counter = 0;

    for (int i = Mathf.FloorToInt(_gridParams.range.x * _gridParams.snapFraction); i < Mathf.CeilToInt(_gridParams.range.y * _gridParams.snapFraction); i++) {

      float x = -_gridParams.UnittoX(i / _gridParams.snapFraction);
      if (x != 0 && x != 1) {
        points.Add(new Vector3(x, 0));
        points.Add(new Vector3(x, bounds.y));

        int s = tempCounter + counter * 2;

        if (i % (4 * _gridParams.snapFraction) == 0) {
          linesC.Add(s);
          linesC.Add(s + 1);
        } else if (i % _gridParams.snapFraction == 0) {
          linesB.Add(s);
          linesB.Add(s + 1);
        } else {
          lines.Add(s);
          lines.Add(s + 1);
        }
        counter++;
      }
    }

    mesh.vertices = points.ToArray();

    mesh.SetIndices(rowsA.ToArray(), MeshTopology.Quads, 0);
    mesh.SetIndices(rowsB.ToArray(), MeshTopology.Quads, 1);
    mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 2);
    mesh.SetIndices(linesB.ToArray(), MeshTopology.Lines, 3);
    mesh.SetIndices(linesC.ToArray(), MeshTopology.Lines, 4);
  }
}
