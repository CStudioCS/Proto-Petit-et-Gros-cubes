using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    [Header("Rope")]
    public int segmentCount = 20;
    public float segmentLength = 0.25f;
    public float radius = 0.05f;

    [Header("Endpoints")]
    public Rigidbody playerA;
    public Rigidbody playerB;

    [Header("Visual")]
    public LineRenderer line;

    private Rigidbody[] segments;

    void Start()
    {
        CreateRope();
    }

    void CreateRope()
    {
        segments = new Rigidbody[segmentCount];

        Vector3 start = playerA.position;
        Vector3 end = playerB.position;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (i + 0.5f) / segmentCount;
            Vector3 position = Vector3.Lerp(start, end, t);

            GameObject obj = new GameObject("RopeSegment_" + i);
            obj.transform.position = position;

            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;

            rb.collisionDetectionMode =
                CollisionDetectionMode.Continuous;

            CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = segmentLength;
            capsule.direction = 2; // Z

            segments[i] = rb;

            if (i == 0)
            {
                CreateJoint(rb, playerA);
            }
            else
            {
                CreateJoint(rb, segments[i - 1]);
            }
        }

        CreateJoint(playerB, segments[segmentCount - 1]);
    }

    void CreateJoint(Rigidbody rb, Rigidbody connected)
    {
        ConfigurableJoint joint = rb.gameObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = connected;

        joint.autoConfigureConnectedAnchor = true;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        SoftJointLimit limit = joint.linearLimit;
        limit.limit = 0.01f;
        joint.linearLimit = limit;

        joint.enableCollision = false;
    }

    void LateUpdate()
    {
        if (segments == null || line == null)
            return;

        line.positionCount = segmentCount + 2;

        line.SetPosition(0, playerA.position);

        for (int i = 0; i < segmentCount; i++)
        {
            line.SetPosition(i + 1, segments[i].position);
        }

        line.SetPosition(segmentCount + 1, playerB.position);
    }
}