using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace Spider_Man.Bosses.GreenGoblin
{
    public class GliderController : MonoBehaviour
    {
        public LayerMask obstacleMask;
        public float flightSpeed = 30f;
        public float avoidanceRadius = 5f;
        public float avoidanceStrength = 30f;

        private Rigidbody rigidbody;
        private PID.PID pid = new PID.PID();

        private float proportionalGain = 1f;
        private float integralGain = 0f;
        private float derivativeGain = 1f;

        private GameObject targetObject;
        private NavMeshAgent agent;
        private Vector3 targetPosition;
        private float relativeDistance;
        Vector3 relativeDirection;
        
        //avoidance
        public float probeRadius = 0.5f;      // ★ size of your “bumper”
        public float probeDistance = 4f;      // ★ look-ahead distance
        public float sideStepStrength = 2f;   // ★ how strongly to steer sideways


        private void Start()
        {
            pid.proportionalGain = proportionalGain;
            pid.integralGain = integralGain;
            pid.derivativeGain = derivativeGain;
            pid.derivativeMeasurement = PID.PID.DerivativeMeasurement.ErrorRateOfChange;

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;                 // ★ smoother
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;  // ★ reduce tunneling
            rigidbody.maxDepenetrationVelocity = 10f;    
            targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetupNavMeshAgent();
            StartCoroutine(RandomPositionChange());
        }

        IEnumerator RandomPositionChange()
        {
            while (true)
            {
                targetPosition = FindRandomPositionWithinUnitSphere();
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
            }
        }
        
        private void SetupNavMeshAgent(float speedMultiplier = 5f, float stoppingDistance = 1f, float baseOffset = 0f)
        {
            targetObject.transform.position = transform.position;
            targetObject.transform.localScale = Vector3.one * 0.25f;
            agent = targetObject.gameObject.AddComponent<NavMeshAgent>();
            agent.stoppingDistance = stoppingDistance;
            agent.speed *= speedMultiplier;
            agent.baseOffset = baseOffset;
            agent.autoBraking = true;

            // ★ Use the agent only as a planner (don’t let it move the sphere).
            agent.updatePosition = false;   // ★
            agent.updateRotation = false;   // ★

            // (Optional) hide the helper sphere
            var rend = targetObject.GetComponent<Renderer>(); if (rend) rend.enabled = false; // ★
            var col  = targetObject.GetComponent<Collider>(); if (col) Destroy(col);          // ★
        }
        
        Vector3 FindRandomPositionWithinUnitSphere()
        {
            var unitPosition = UnityEngine.Random.onUnitSphere * 15f;
            var actual = new Vector3(unitPosition.x, Mathf.Abs(unitPosition.y), unitPosition.z);
            var position = Player.local.creature.ragdoll.targetPart.transform.position + actual;
            relativeDistance = Vector3.Distance(position, Player.local.creature.ragdoll.transform.position);
            relativeDirection = (position - Player.local.creature.ragdoll.transform.position).normalized;
            return Player.local.creature.ragdoll.targetPart.transform.position + (UnityEngine.Random.insideUnitSphere * 4f);
        }

        private void FixedUpdate()
        {
            if (Player.local == null || Player.local.creature == null) return;

            // Keep the target in a ring around the player (your original intent)

            // ★ 1) Project our current position to the NavMesh and sync the planner agent there (XZ).
            if (NavMesh.SamplePosition(new Vector3(transform.position.x, 0f, transform.position.z),
                                       out var startHit, 2f, NavMesh.AllAreas))
            {
                agent.nextPosition = startHit.position; // ★ keep planner at the glider’s XZ
            }

            // ★ 2) Project the goal XZ to the NavMesh and set as destination.
            Vector3 goalXZ = new Vector3(targetPosition.x, 0f, targetPosition.z); // ★
            Vector3 fallbackGoal = goalXZ; // ★
            if (NavMesh.SamplePosition(goalXZ, out var goalHit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(goalHit.position);  // ★ compute path on mesh
                fallbackGoal = goalHit.position;         // ★
            }
            else
            {
                agent.ResetPath(); // ★
            }

            // ★ 3) Use the agent’s steering target (next corner) as our planner output.
            Vector3 steer = (agent.hasPath && !agent.pathPending) ? agent.steeringTarget : fallbackGoal; // ★
            Vector3 navigatedXZ = new Vector3(steer.x, targetPosition.y, steer.z); // ★ keep your chosen altitude

            Vector3 toSteer = (navigatedXZ - transform.position);
            Vector3 dir = new Vector3(toSteer.x, 0f, toSteer.z).normalized; // stay horizontal for lateral steer
            Vector3 origin = transform.position + Vector3.up * 0.2f;

            if (Physics.SphereCast(origin, probeRadius, dir, out var hit, probeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Push away from obstacle: combine surface normal + a lateral slip
                Vector3 away = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

                // Pick freer side by probing a bit to the left/right
                float rightClear = Physics.SphereCast(origin, probeRadius * 0.9f, right,  out _, probeRadius * 1.5f, obstacleMask) ? 0f : 1f;
                float leftClear  = Physics.SphereCast(origin, probeRadius * 0.9f, -right, out _, probeRadius * 1.5f, obstacleMask) ? 0f : 1f;
                Vector3 lateral = (rightClear >= leftClear ? right : -right);

                Vector3 deflect = (away + lateral * sideStepStrength).normalized;

                // Shift the target a bit in the deflection direction, keep your chosen altitude
                navigatedXZ += deflect * Mathf.Min(hit.distance, probeDistance) * 0.5f; // ★ soften
                navigatedXZ.y = targetPosition.y;
            }
            
            // (Removed lines that made the agent path to itself and moved baseOffset every frame)
            // agent.destination = this.targetObject.transform.position;          // ✖
            // agent.baseOffset = targetPosition.y;                               // ✖
            // targetObject.transform.position = targetPosition;                   // ✖
            // Vector3 navigatedXZ = new Vector3(agent.nextPosition.x, targetPosition.y, agent.nextPosition.z); // ✖

            Vector3 input = pid.Update(Time.fixedDeltaTime, transform.position, navigatedXZ);
            rigidbody.AddForce(input * 30f);

            transform.rotation = Quaternion.LookRotation(
                Player.local.creature.ragdoll.headPart.transform.position - this.transform.position);
        }
    }
}
