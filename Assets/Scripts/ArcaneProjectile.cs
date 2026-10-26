using UnityEngine;

namespace ArcaneWorkshop
{
    // A small moving instance that travels to a target and is consumed when it collides with it.
    // The receiving object's OnTriggerEnter applies the Stat change and destroys the projectile.
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcaneProjectile : MonoBehaviour
    {
        private const float MaxLifetime = 14f;

        private ProjectileKind kind;
        private Component target;
        private Vector3 targetOffset;
        private Vector3[] path;
        private float pathProgress;
        private float speed;
        private float phase;
        private float life;
        private float fallSpeed;
        private bool consumed;

        public ProjectileKind Kind { get { return kind; } }
        public Component Target { get { return target; } }
        public int Payload { get; private set; }

        public static ArcaneProjectile Spawn(ProjectileKind kind, Vector3 start, Component target, Vector3 targetOffset, GameObject visual, float scale)
        {
            GameObject projectileObject = visual != null ? Instantiate(visual) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = kind + " Projectile";
            foreach (Collider existing in projectileObject.GetComponentsInChildren<Collider>())
            {
                Destroy(existing);
            }

            projectileObject.transform.position = start;
            projectileObject.transform.localScale = Vector3.one * scale;
            SphereCollider sphere = projectileObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = visual != null ? 0.25f : 0.5f;
            ArcaneWeights.Apply(projectileObject, ArcaneWeights.RealMassKg(kind));

            ArcaneProjectile projectile = projectileObject.AddComponent<ArcaneProjectile>();
            projectile.kind = kind;
            projectile.target = target;
            projectile.targetOffset = targetOffset;
            projectile.phase = Random.Range(0f, 10f);
            projectile.speed = kind == ProjectileKind.FallingStar ? 7f : kind == ProjectileKind.Potion ? 2.4f : 2.4f;
            return projectile;
        }

        public ArcaneProjectile WithPayload(int payload)
        {
            Payload = payload;
            return this;
        }

        public ArcaneProjectile AlongPath(Vector3[] waypoints)
        {
            path = waypoints;
            pathProgress = 0f;
            return this;
        }

        private Vector3 Destination
        {
            get { return target.transform.position + targetOffset; }
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (consumed || target == null || life > MaxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            switch (kind)
            {
                case ProjectileKind.Ingredient:
                {
                    // Spiral inward: home on the cauldron while orbiting sideways.
                    Vector3 destination = Destination;
                    Vector3 tangent = Vector3.Cross(Vector3.up, destination - transform.position).normalized;
                    transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * speed);
                    transform.position += tangent * Mathf.Sin(Time.time * 8f + phase) * Time.deltaTime * 0.35f;
                    break;
                }

                case ProjectileKind.WaterDrop:
                {
                    // Falls under gravity straight onto the planter below it.
                    fallSpeed += 9.8f * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, Destination, fallSpeed * Time.deltaTime);
                    transform.localScale = new Vector3(0.08f, 0.08f + Mathf.Min(fallSpeed * 0.01f, 0.06f), 0.08f);
                    break;
                }

                case ProjectileKind.CrystalSpark:
                {
                    Vector3 destination = Destination;
                    transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * speed * 1.4f);
                    transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.World);
                    break;
                }

                case ProjectileKind.FallingStar:
                    transform.position = Vector3.MoveTowards(transform.position, Destination, Time.deltaTime * speed);
                    break;

                case ProjectileKind.Potion:
                    FollowPath();
                    break;
            }
        }

        // Designer-authored Catmull-Rom path, walked at roughly constant speed.
        private void FollowPath()
        {
            if (path == null || path.Length < 2)
            {
                transform.position = Vector3.MoveTowards(transform.position, Destination, Time.deltaTime * speed);
                return;
            }

            int segment = Mathf.Min(Mathf.FloorToInt(pathProgress), path.Length - 2);
            float segmentLength = Mathf.Max(0.01f, Vector3.Distance(path[segment], path[segment + 1]));
            pathProgress = Mathf.Min(path.Length - 1, pathProgress + speed * Time.deltaTime / segmentLength);
            segment = Mathf.Min(Mathf.FloorToInt(pathProgress), path.Length - 2);
            float t = pathProgress - segment;

            Vector3 p0 = path[Mathf.Max(segment - 1, 0)];
            Vector3 p1 = path[segment];
            Vector3 p2 = path[segment + 1];
            Vector3 p3 = path[Mathf.Min(segment + 2, path.Length - 1)];
            Vector3 next = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);

            Vector3 heading = next - transform.position;
            if (heading.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading.normalized) * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 6f) * 12f), 10f * Time.deltaTime);
            }
            transform.position = next;
        }

        public void Consume()
        {
            if (consumed)
            {
                return;
            }

            consumed = true;
            Destroy(gameObject);
        }
    }
}
