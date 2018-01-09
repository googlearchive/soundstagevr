// This is a placeholder script so that SoundStage runs without Sonic Ether Natural Bloom & Dirty Lens from the Unity Asset Store
// You can purchase the asset here: http://u3d.as/7v5

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SENaturalBloomAndDirtyLens : MonoBehaviour
{
  public static bool debugMsgShown = false;

	[Range(0.0f, 0.4f)]
	public float bloomIntensity = 0.05f;

  private void Start() {
    if(!debugMsgShown) {
      debugMsgShown = true;
      Debug.Log("Glow effect missing (not included in source control)\n" +
  "You can purchase the Sonic Ether Natural Bloom & Dirty Lens asset in the Unity Asset Store: http://u3d.as/7v5");
    }

  }
}