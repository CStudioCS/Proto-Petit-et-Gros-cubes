using System.Collections.Generic;
using UnityEngine;

public class BrasJoints : MonoBehaviour
{
    class InfoBras
    {
        public Transform t;
        public ConfigurableJoint joint;
        public Vector3 posLocale;
        public Quaternion rotLocale;
        public Vector3 anchor;
        public Vector3 connectedAnchor;
    }

    readonly List<InfoBras> bras = new List<InfoBras>();

    void Start()
    {
        foreach (var joint in GetComponentsInChildren<ConfigurableJoint>())
        {
            bras.Add(new InfoBras
            {
                t = joint.transform,
                joint = joint,
                posLocale = joint.transform.localPosition,
                rotLocale = joint.transform.localRotation,
                anchor = joint.anchor,
                connectedAnchor = joint.connectedAnchor
            });
        }
    }

    // À appeler APRÈS avoir modifié localScale
    public void Reappliquer()
    {
        foreach (var b in bras)
        {
            var go = b.t.gameObject;

            go.SetActive(false);

            b.t.localPosition = b.posLocale;
            b.t.localRotation = b.rotLocale;

            b.joint.autoConfigureConnectedAnchor = false;
            b.joint.anchor = b.anchor;
            b.joint.connectedAnchor = b.connectedAnchor;

            go.SetActive(true);
        }

        Physics.SyncTransforms();
    }
}