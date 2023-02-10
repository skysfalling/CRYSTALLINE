using UnityEngine;

public class XR_PhysicsHand : MonoBehaviour
{
    //https://www.youtube.com/watch?v=5D2bN7xL5us

    [Header("PID")]
    [SerializeField] float frequency = 50f;
    [SerializeField] float damping = 1f;
    [SerializeField] float rotFrequency = 100f;
    [SerializeField] float rotDamping = 0.9f;
    [SerializeField] Rigidbody playerRB;
    [SerializeField] Transform target;

    [Space(10)]
    [Header("Springs")]
    [SerializeField] float climbForce = 1000f;
    [SerializeField] float climbDrag = 500f;

    [Header("HookesLaw")]
    public bool hookesLawEnabled;

    Vector3 _previousPosition;
    Rigidbody _rigidbody;
    bool _isColliding;



    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.maxAngularVelocity = float.PositiveInfinity;
        _previousPosition = transform.position;

        SetPositionToPlayer();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PIDMovement();
        PIDRotation();

        if (hookesLawEnabled)
            HookesLaw(); // if enabled, allow movement
    }

    void PIDMovement()
    {
        float kp = (6f * frequency) * (6f * frequency) * 0.25f;
        float kd = 4.5f * frequency * damping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;

        // how much force to give the physics hand for it to match the transform of our controller
        Vector3 force = (target.position - transform.position) * ksg + (playerRB.velocity - _rigidbody.velocity) * kdg;

        _rigidbody.AddForce(force, ForceMode.Acceleration);
    }

    void PIDRotation()
    {
        float kp = (6f * rotFrequency) * (6f * rotFrequency) * 0.25f;
        float kd = 4.5f * rotFrequency * rotDamping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;

        Quaternion q = target.rotation * Quaternion.Inverse(transform.rotation);
        if (q.w < 0)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
        }
        q.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();
        axis *= Mathf.Deg2Rad;
        Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;

        _rigidbody.AddTorque(torque, ForceMode.Acceleration);
    }

    void HookesLaw()
    {
        // distance between your controller and the physics hand and use that as a "spring force" to propel

        Vector3 displacementFromResting = transform.position - target.position;
        Vector3 force = displacementFromResting * climbForce;

        float drag = GetDrag();

        playerRB.AddForce(force, ForceMode.Acceleration);
        playerRB.AddForce(drag * -playerRB.velocity * climbDrag, ForceMode.Acceleration);

    }

    // more drag when moving slower
    // less drag when moving faster
    float GetDrag()
    {
        Vector3 handVelocity = (target.localPosition - _previousPosition) / Time.fixedDeltaTime;

        float drag = 1 / handVelocity.magnitude + 0.01f;

        // clamp
        drag = drag > 1 ? 1 : drag; // if drag > 1 then = 1 else drag
        drag = drag < 0.02f ? 0.02f : drag;

        _previousPosition = transform.position;
        return drag;
    }

    public void SetPositionToPlayer()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        _isColliding = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        _isColliding = false;
    }
}
