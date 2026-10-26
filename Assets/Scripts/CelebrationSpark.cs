using UnityEngine;

namespace ArcaneWorkshop
{
    // A short-lived glowing mote that bursts upward and fades: used for goal and ending celebrations.
    public sealed class CelebrationSpark : MonoBehaviour
    {
        private Vector3 velocity;
        private float life;
        private float lifetime;

        public static void Burst(Vector3 origin, int count, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spark.name = "Celebration Spark";
                Destroy(spark.GetComponent<Collider>());
                spark.GetComponent<Renderer>().sharedMaterial = material;
                spark.transform.position = origin;
                spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
                CelebrationSpark mover = spark.AddComponent<CelebrationSpark>();
                mover.velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(4f, 7f), Random.Range(-2.5f, 2.5f));
                mover.lifetime = Random.Range(1.4f, 2.4f);
            }
        }

        private void Update()
        {
            life += Time.deltaTime;
            velocity += Vector3.down * 6f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.localScale *= 1f - Time.deltaTime * 0.6f;
            if (life >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
